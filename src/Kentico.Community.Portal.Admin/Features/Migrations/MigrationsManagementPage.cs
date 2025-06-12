using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.Migrations;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    uiPageType: typeof(MigrationManagementPage),
    parentType: typeof(MigrationsApplicationPage),
    slug: "management",
    name: "Management",
    templateName: "@kentico-community/portal-web-admin/MigrationManagementLayout",
    order: 1,
    Icon = Icons.Cogwheels)]

namespace Kentico.Community.Portal.Admin.Features.Migrations;

public class MigrationManagementPage(IEnumerable<IDataMigrator> migrators) : Page<MigrationManagementPageClientProperties>
{
    public const string COMMAND_NAME = "Migrate";

    public override async Task<MigrationManagementPageClientProperties> ConfigureTemplateProperties(MigrationManagementPageClientProperties properties)
    {
        await Task.CompletedTask;

        List<MigrationState> migrations = [];
        foreach (var dataMigrator in migrators)
        {
            var items = await dataMigrator.MigrateableItems();
            migrations.Add(new(dataMigrator.Name, dataMigrator.DisplayName, items.Count));
        }
        properties.AvailableMigrations = migrations;

        properties.CommandName = COMMAND_NAME;
        return properties;
    }

    [PageCommand(CommandName = COMMAND_NAME, Permission = SystemPermissions.UPDATE)]
    public async Task<ICommandResponse> Migrate(MigrateCommandParams commandParams)
    {
        var (name, count) = commandParams;
        var migration = migrators.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));

        if (migration is null)
        {
            return ResponseFrom(new MigrationResult(name, "", [], [], 0))
                .AddErrorMessage($"Migration [{name}] is not registered");
        }

        var result = await migration.Migrate(count);
        var remaining = await migration.MigrateableItems();

        return ResponseFrom(result with { MigratableItemsCount = remaining.Count })
            .AddSuccessMessage("Migration complete");
    }
}

public class MigrationManagementPageClientProperties : TemplateClientProperties
{
    public string CommandName { get; set; } = "";
    public IEnumerable<MigrationState> AvailableMigrations { get; set; } = [];
}

public record MigrateCommandParams(string MigrationName, int MigrateItemsCount = 0);

public interface IDataMigrator
{
    public string Name { get; }
    public string DisplayName { get; }
    public Task<Dictionary<string, string>> MigrateableItems(CancellationToken token = default);
    public Task<MigrationResult> Migrate(int count, CancellationToken token = default);
}

public record MigrationState(string Name, string DisplayName, int MigratableItemsCount);
public record MigrationResult(
    string Name,
    string DisplayName,
    IEnumerable<string> Successes,
    IEnumerable<string> Failures,
    int MigratableItemsCount);
