using Kentico.Community.Portal.AppHost;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

const string DatabaseName = "Kentico.Community";

// Web project's UserSecretsId — keep in sync with Kentico.Community.Portal.Web.csproj.
const string WebUserSecretsId = "3df470e6-54c4-41d8-b8f0-36955d9433d2";

// Database wiring is intentionally conditional:
//
//   * Bring-your-own-server: if a CMSConnectionString is resolvable from the AppHost's own
//     configuration OR the Web project's user secrets, Aspire references THAT existing SQL
//     Server directly and provisions no container. An existing developer who already keeps
//     CMSConnectionString in their Web app user secrets (the manual workflow) therefore keeps
//     using their own local SQL Server automatically — no AppHost setup required.
//
//   * Aspire-managed (CI/CD and fresh onboarding, where no connection string exists yet):
//     Aspire provisions a managed SQL Server container with a persistent data volume and a
//     one-time "Restore database" command — "Aspire manages everything".
string? existingDatabaseConnection = builder.Configuration.GetConnectionString("CMSConnectionString");

if (string.IsNullOrWhiteSpace(existingDatabaseConnection))
{
    // Fall back to the Web project's user secrets (the connection string used by the manual workflow).
    existingDatabaseConnection = new ConfigurationBuilder()
        .AddUserSecrets(WebUserSecretsId)
        .Build()
        .GetConnectionString("CMSConnectionString");
}

bool useManagedSqlServer = string.IsNullOrWhiteSpace(existingDatabaseConnection);

IResourceBuilder<IResourceWithConnectionString> database;
if (useManagedSqlServer)
{
    // Absolute path to the repository's "database" folder (contains the backup .zip used by the restore command).
    string databaseFolderHostPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "database"));
    const string BackupContainerFolder = "/var/opt/mssql/backups";

    // SQL Server: persistent container + data volume so the restored database survives restarts.
    // The database is restored once via the "Restore database" dashboard command.
    // The host port is Aspire-managed (dynamic) so this never collides with a legacy standalone
    // SQL container; the web app receives the connection string through WithReference below.
    var sql = builder.AddSqlServer("sql")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("kentico-community-sql-data")
        .WithDatabaseRestoreCommand(databaseFolderHostPath, BackupContainerFolder, DatabaseName);

    database = sql.AddDatabase("kentico-community", DatabaseName);
}
else
{
    // Reference the developer's existing SQL Server. Make the resolved value available to
    // AddConnectionString regardless of which store it came from (AppHost or Web project secrets).
    builder.Configuration["ConnectionStrings:CMSConnectionString"] = existingDatabaseConnection;
    database = builder.AddConnectionString("CMSConnectionString");
}

// Azure Storage emulator (Azurite), pinned to the standard emulator ports so the existing
// CMSAzure* development settings keep working unchanged. Used by Support Request Processing when enabled.
_ = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume("kentico-community-azurite-data")
        .WithBlobPort(10000)
        .WithQueuePort(10001)
        .WithTablePort(10002));

// Vite dev server for the website channel client assets (proxied by the web app via Vite.AspNetCore).
// Self-configures HTTPS + port 5174 from appsettings; the endpoint is declared for dashboard visibility only.
// pnpm packages are installed once at the repo root (shared pnpm workspace), so per-resource install is skipped.
_ = builder.AddViteApp("web-client", "../Kentico.Community.Portal.Web", "dev")
    .WithPnpm(install: false)
    .WithHttpsEndpoint(port: 5174, isProxied: false);

// Webpack dev server for the Xperience admin customizations (consumed by the admin UI in Proxy mode).
// Self-configures HTTPS + port 3099 from webpack.config.js.
_ = builder.AddJavaScriptApp("admin-client", "../Kentico.Community.Portal.Admin/Client", "start")
    .WithPnpm(install: false)
    .WithHttpsEndpoint(port: 3099, isProxied: false);

// The Xperience by Kentico web application. Uses the existing launch profile so the fixed
// Kestrel ports (45039 site, 45040 admin, 45041 http) that Xperience config depends on are preserved.
var web = builder.AddProject<Projects.Kentico_Community_Portal_Web>("web", launchProfileName: "Portal.Web")
    .WithReference(database, connectionName: "CMSConnectionString");

// Only gate startup on the database when Aspire owns its lifecycle; an external server is assumed ready.
if (useManagedSqlServer)
{
    web.WaitFor(database);
}

// === Developer tooling surfaced as on-demand dashboard actions ===
// Each tool is explicit-start (never auto-runs), receives the resolved database connection string via
// WithReference (ConnectionStrings__CMSConnectionString), and is grouped under the web app. The pwsh
// scripts read that injected connection string automatically (see scripts/Utilities Get-ConnectionString);
// the DataCleaner project reads it via AddEnvironmentVariables. PowerShell 7 (pwsh) is required for the
// script-based actions.
string scriptsDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "scripts"));

IResourceBuilder<T> AsDevTool<T>(IResourceBuilder<T> tool)
    where T : IResource, IResourceWithEnvironment, IResourceWithWaitSupport
{
    tool = tool
        .WithExplicitStart()
        .WithReference(database, connectionName: "CMSConnectionString")
        .WithParentRelationship(web);

    // Wait for the managed database to be healthy before the tool runs; an external server is assumed ready.
    if (useManagedSqlServer)
    {
        tool = tool.WaitFor(database);
    }

    return tool;
}

// Regenerate Xperience content/object type code files (tool of the web app; reads schema from the database).
_ = AsDevTool(builder.AddExecutable("generate-code", "pwsh", scriptsDirectory,
    "-NoProfile", "-File", "Refresh-GeneratedCode.ps1"));

// Generate the Xperience SaaS deployment package (used to test CD repository.config changes).
_ = AsDevTool(builder.AddExecutable("deployment-package", "pwsh", scriptsDirectory,
    "-NoProfile", "-File", "Export-DeploymentPackage.ps1"));

// Back up the database to a zipped .bak in the 'database' folder (mirrors the manual PowerShell workflow).
_ = AsDevTool(builder.AddExecutable("db-backup", "pwsh", scriptsDirectory,
    "-NoProfile", "-File", "Backup-Database.ps1"));

// Scrub member data from a restored production backup before CI restore. Destructive: --skip-confirmation
// proceeds without the interactive prompt (the dashboard Start action is the confirmation).
_ = AsDevTool(builder.AddProject<Projects.Kentico_Community_DataCleaner_App>("data-cleaner")
    .WithArgs("--skip-confirmation"));

builder.Build().Run();
