using System.IO.Compression;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kentico.Community.Portal.AppHost;

/// <summary>
/// AppHost extensions for the Kentico Community Portal SQL Server resource, including a
/// one-time database restore exposed as a custom dashboard command.
/// </summary>
internal static class SqlServerResourceBuilderExtensions
{
    /// <summary>
    /// Bind-mounts the repository's <c>database</c> folder into the SQL Server container and registers a
    /// "Restore database" dashboard command that extracts the latest <c>*.zip</c> backup and restores it
    /// as <paramref name="databaseName"/>. Intended as a one-time onboarding step; the database otherwise
    /// persists on the resource's data volume.
    /// </summary>
    public static IResourceBuilder<SqlServerServerResource> WithDatabaseRestoreCommand(
        this IResourceBuilder<SqlServerServerResource> builder,
        string databaseFolderHostPath,
        string backupContainerFolder,
        string databaseName)
    {
        _ = builder.WithBindMount(databaseFolderHostPath, backupContainerFolder);

        _ = builder.WithCommand(
            name: "restore-database",
            displayName: "Restore database",
            executeCommand: context => OnRestoreDatabaseAsync(builder, context, databaseFolderHostPath, backupContainerFolder, databaseName),
            commandOptions: new CommandOptions
            {
                Description = $"Extracts the latest backup in the 'database' folder and restores it as '{databaseName}'.",
                ConfirmationMessage = $"This drops and restores the '{databaseName}' database from the latest backup in the 'database' folder. Continue?",
                IconName = "DatabaseArrowDown",
                IconVariant = IconVariant.Filled,
                UpdateState = static context => context.ResourceSnapshot.HealthStatus is Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
                    ? ResourceCommandState.Enabled
                    : ResourceCommandState.Disabled,
            });

        return builder;
    }

    private static async Task<ExecuteCommandResult> OnRestoreDatabaseAsync(
        IResourceBuilder<SqlServerServerResource> builder,
        ExecuteCommandContext context,
        string databaseFolderHostPath,
        string backupContainerFolder,
        string databaseName)
    {
        var loggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        var logger = loggerService.GetLogger(context.ResourceName);

        try
        {
            var backupZip = new DirectoryInfo(databaseFolderHostPath)
                .GetFiles("*.zip")
                .OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (backupZip is null)
            {
                logger.LogError("No backup .zip found in {Folder}.", databaseFolderHostPath);

                return CommandResults.Failure($"No backup .zip found in '{databaseFolderHostPath}'.");
            }

            // The .zip BaseName is the .bak file name (e.g. Kentico.Community-31.5.4-2026-06-16.bak).
            string backupFileName = Path.GetFileNameWithoutExtension(backupZip.Name);
            string extractedBakHostPath = Path.Combine(databaseFolderHostPath, backupFileName);

            logger.LogInformation("Extracting {Zip} ...", backupZip.Name);

            using (var archive = ZipFile.OpenRead(backupZip.FullName))
            {
                var bakEntry = archive.Entries
                    .FirstOrDefault(e => e.Name.Equals(backupFileName, StringComparison.OrdinalIgnoreCase))
                    ?? archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase));

                if (bakEntry is null)
                {
                    logger.LogError("No .bak entry found inside {Zip}.", backupZip.Name);

                    return CommandResults.Failure($"No .bak entry found inside '{backupZip.Name}'.");
                }

                backupFileName = bakEntry.Name;
                extractedBakHostPath = Path.Combine(databaseFolderHostPath, backupFileName);
                bakEntry.ExtractToFile(extractedBakHostPath, overwrite: true);
            }

            // Path as seen by the SQL Server container via the bind mount.
            string backupContainerPath = $"{backupContainerFolder.TrimEnd('/')}/{backupFileName}";

            string? serverConnectionString = await builder.Resource.GetConnectionStringAsync(context.CancellationToken);

            if (string.IsNullOrEmpty(serverConnectionString))
            {
                return CommandResults.Failure($"Unable to resolve the '{context.ResourceName}' connection string.");
            }

            // Always target the master database to run the RESTORE.
            var masterConnectionString = new SqlConnectionStringBuilder(serverConnectionString)
            {
                InitialCatalog = "master",
            }.ConnectionString;

            string restoreSql = $"""
                IF EXISTS (SELECT name FROM sys.databases WHERE name = @dbName)
                BEGIN
                    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{databaseName}];
                END

                RESTORE DATABASE [{databaseName}]
                FROM DISK = @backupPath
                WITH REPLACE;
                """;

            logger.LogInformation("Restoring '{Database}' from {Backup} ...", databaseName, backupContainerPath);

            await using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync(context.CancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = restoreSql;
            command.CommandTimeout = 0;
            _ = command.Parameters.AddWithValue("@dbName", databaseName);
            _ = command.Parameters.AddWithValue("@backupPath", backupContainerPath);

            _ = await command.ExecuteNonQueryAsync(context.CancellationToken);

            logger.LogInformation("Database '{Database}' restored successfully.", databaseName);

            return CommandResults.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database restore failed.");

            return CommandResults.Failure(ex);
        }
    }
}
