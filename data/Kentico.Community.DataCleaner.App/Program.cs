/**
 * This application is used to clean and fix data from a PROD environment that is being restored locally before running CI Restore.
 * Processing this data is required because the CI process does not track Member data, but other objects it tracks are dependent on it.
 */

using System.Text.Json;
using System.Xml;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

// `--skip-confirmation` (or `--yes`/`-y`) runs the destructive member cleanup without prompting. This is
// required when the tool is launched from the Aspire dashboard, which has no interactive console.
bool skipConfirmation = args.Any(a => a is "--skip-confirmation" or "--yes" or "-y");

// Load member data from JSON file
string jsonFilePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "default-members.json");
string jsonContent = File.ReadAllText(jsonFilePath);
var members = JsonSerializer.Deserialize<Member[]>(jsonContent)
    ?? throw new Exception("Failed to deserialize default-members.json");

string solutionDirectory = FindSolutionFileDirectory(AppDomain.CurrentDomain.BaseDirectory)
    ?? throw new Exception("This application requires a parent .slnx or .sln file");

// Clean up the database first
await CleanupMemberTable(members, skipConfirmation);

// Rewrite member references in the CI repository content so they point at the seeded test members.
var targets = new MemberReferenceTarget[]
{
    new(
        "Questions",
        ["src", "Kentico.Community.Portal.Web", "App_Data", "CIRepository", "devnet", "contentitemdata.kenticocommunity.qandaquestionpage"],
        "//QAndAQuestionPageAuthorMemberID",
        UseGuid: false, SkipWhenZero: true, Recurse: true),
    new(
        "Answers",
        ["src", "Kentico.Community.Portal.Web", "App_Data", "CIRepository", "@global", "kenticocommunity.qandaanswerdata"],
        "//QAndAAnswerDataAuthorMemberID/GUID",
        UseGuid: true, SkipWhenZero: false, Recurse: false),
    new(
        "LinkContent",
        ["src", "Kentico.Community.Portal.Web", "App_Data", "CIRepository", "@global", "contentitemdata.kenticocommunity.linkcontent"],
        "//LinkContentMemberID",
        UseGuid: false, SkipWhenZero: true, Recurse: true),
    new(
        "IntegrationContent",
        ["src", "Kentico.Community.Portal.Web", "App_Data", "CIRepository", "@global", "contentitemdata.ke..integrationcontent@1dabf6434c"],
        "//IntegrationContentAuthorMemberID",
        UseGuid: false, SkipWhenZero: true, Recurse: true),
    new(
        "MemberProfileContent",
        ["src", "Kentico.Community.Portal.Web", "App_Data", "CIRepository", "@global", "contentitemdata.ke..mberprofilecontent@8653edbc10"],
        "//MemberProfileContentMemberID",
        UseGuid: false, SkipWhenZero: true, Recurse: true),
    new(
        "AuthorContent",
        ["src", "Kentico.Community.Portal.Web", "App_Data", "CIRepository", "@global", "contentitemdata.kenticocommunity.authorcontent"],
        "//AuthorContentMemberID",
        UseGuid: false, SkipWhenZero: true, Recurse: true),
};

foreach (var target in targets)
{
    ReplaceMemberReferences(target, solutionDirectory, members);
}

Console.WriteLine("All processing completed.");

// Replaces member-reference values in CI repository XML content with a randomly selected seeded member.
void ReplaceMemberReferences(MemberReferenceTarget target, string rootPath, Member[] availableMembers)
{
    string[] pathSegments = [rootPath, .. target.RelativeDirSegments];
    string directoryPath = Path.Combine(pathSegments);

    if (!Directory.Exists(directoryPath))
    {
        Console.WriteLine($"Directory not found: {directoryPath}. Skipping {target.Name} migration.");
        return;
    }

    var searchOption = target.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
    string[] xmlFiles = Directory.GetFiles(directoryPath, "*.xml", searchOption);

    foreach (string file in xmlFiles)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            var memberNode = xmlDoc.SelectSingleNode(target.Xpath);
            if (memberNode is null)
            {
                Console.WriteLine($"No node matching '{target.Xpath}' found in file: {file}");
                continue;
            }

            // System-authored content uses member ID 0 and must be preserved.
            if (target.SkipWhenZero && int.TryParse(memberNode.InnerText, out int currentId) && currentId == 0)
            {
                Console.WriteLine($"Skipping system-authored content (value 0) in file: {file}");
                continue;
            }

            var randomMember = availableMembers[Random.Shared.Next(availableMembers.Length)];
            memberNode.InnerText = target.UseGuid
                ? randomMember.MemberGuid.ToString()
                : randomMember.MemberID.ToString();
            xmlDoc.Save(file);

            Console.WriteLine($"Updated {target.Name} reference to member {randomMember.MemberID} in file: {file}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file {file}: {ex.Message}");
        }
    }

    Console.WriteLine($"{target.Name} processing completed.");
}

async Task CleanupMemberTable(Member[] seedMembers, bool skipConfirmationPrompt)
{
    Console.WriteLine("Starting database cleanup...");

    string connectionString = GetConnectionString();

    if (!skipConfirmationPrompt)
    {
        if (Console.IsInputRedirected)
        {
            Console.WriteLine("Non-interactive session detected and --skip-confirmation was not provided. Skipping database cleanup.");
            return;
        }

        Console.WriteLine("WARNING: This will delete all records from the CMS_Member table and replace them with test data.");
        Console.WriteLine("Press 'y' to continue or any other key to skip database cleanup:");

        var key = Console.ReadKey();
        Console.WriteLine(); // New line after key press

        if (key.KeyChar is not 'y' and not 'Y')
        {
            Console.WriteLine("Database cleanup skipped.");
            return;
        }
    }

    try
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        Console.WriteLine("Connected to database. Cleaning up CMS_Member table...");

        // Delete all records from the table (TRUNCATE cannot be used due to foreign key constraints)
        await ExecuteNonQueryAsync(connection, "DELETE FROM CMS_Member");

        Console.WriteLine("Table cleared. Inserting new member data...");

        // Enable IDENTITY_INSERT to allow explicit insertion of identity column values
        await ExecuteNonQueryAsync(connection, "SET IDENTITY_INSERT CMS_Member ON");

        // Insert new members
        foreach (var member in seedMembers)
        {
            string insertSql = """
                INSERT INTO CMS_Member (
                    MemberID, MemberEmail, MemberEnabled, MemberCreated, MemberGuid, MemberName, 
                    MemberPassword, MemberIsExternal, MemberSecurityStamp, MemberFirstName, MemberLastName,
                    MemberLinkedInIdentifier, MemberAvatarFileExtension, MemberAdministratorNotes, 
                    MemberModerationStatus, MemberEmployerLink, MemberJobTitle, MemberTimeZone, 
                    MemberCountry, MemberBio
                ) VALUES (
                    @MemberID, @MemberEmail, @MemberEnabled, @MemberCreated, @MemberGuid, @MemberName,
                    @MemberPassword, @MemberIsExternal, @MemberSecurityStamp, @MemberFirstName, @MemberLastName,
                    @MemberLinkedInIdentifier, @MemberAvatarFileExtension, @MemberAdministratorNotes,
                    @MemberModerationStatus, @MemberEmployerLink, @MemberJobTitle, @MemberTimeZone,
                    @MemberCountry, @MemberBio
                )
                """;

            using var command = new SqlCommand(insertSql, connection);

            command.Parameters.AddWithValue("@MemberID", member.MemberID);
            command.Parameters.AddWithValue("@MemberEmail", member.MemberEmail);
            command.Parameters.AddWithValue("@MemberEnabled", member.MemberEnabled);
            command.Parameters.AddWithValue("@MemberCreated", member.MemberCreated);
            command.Parameters.AddWithValue("@MemberGuid", member.MemberGuid);
            command.Parameters.AddWithValue("@MemberName", member.MemberName);
            command.Parameters.AddWithValue("@MemberPassword", member.MemberPassword);
            command.Parameters.AddWithValue("@MemberIsExternal", member.MemberIsExternal);
            command.Parameters.AddWithValue("@MemberSecurityStamp", member.MemberSecurityStamp);
            command.Parameters.AddWithValue("@MemberFirstName", member.MemberFirstName);
            command.Parameters.AddWithValue("@MemberLastName", member.MemberLastName);
            command.Parameters.AddWithValue("@MemberLinkedInIdentifier", member.MemberLinkedInIdentifier);
            command.Parameters.AddWithValue("@MemberAvatarFileExtension", member.MemberAvatarFileExtension);
            command.Parameters.AddWithValue("@MemberAdministratorNotes", (object?)member.MemberAdministratorNotes ?? DBNull.Value);
            command.Parameters.AddWithValue("@MemberModerationStatus", (object?)member.MemberModerationStatus ?? DBNull.Value);
            command.Parameters.AddWithValue("@MemberEmployerLink", (object?)member.MemberEmployerLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@MemberJobTitle", (object?)member.MemberJobTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@MemberTimeZone", member.MemberTimeZone);
            command.Parameters.AddWithValue("@MemberCountry", member.MemberCountry);
            command.Parameters.AddWithValue("@MemberBio", (object?)member.MemberBio ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
            Console.WriteLine($"Inserted member: {member.MemberName} (ID: {member.MemberID})");
        }

        // Disable IDENTITY_INSERT after all insertions are complete
        await ExecuteNonQueryAsync(connection, "SET IDENTITY_INSERT CMS_Member OFF");

        Console.WriteLine($"Successfully inserted {seedMembers.Length} members into the database.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during database cleanup: {ex.Message}");
        throw;
    }
}

string GetConnectionString()
{
    // Prefer the Aspire-injected connection string (ConnectionStrings__CMSConnectionString), provided
    // automatically when this tool runs as a resource in the Aspire dashboard. Falls back to this
    // project's user secrets for the standalone `dotnet run` workflow.
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .Build();

    string? connectionString = configuration.GetConnectionString("CMSConnectionString");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new Exception(
            "Connection string 'CMSConnectionString' not found. Provide it via the " +
            "ConnectionStrings__CMSConnectionString environment variable (injected automatically when run " +
            "from the Aspire dashboard) or this project's user secrets.");
    }

    return connectionString;
}

async Task ExecuteNonQueryAsync(SqlConnection connection, string sql)
{
    using var command = new SqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
}

static string? FindSolutionFileDirectory(string startPath)
{
    string? currentDir = startPath;

    while (!string.IsNullOrEmpty(currentDir))
    {
        bool hasSolutionFile =
            Directory.EnumerateFiles(currentDir, "*.slnx").Any() ||
            Directory.EnumerateFiles(currentDir, "*.sln").Any();

        if (hasSolutionFile)
        {
            return currentDir;
        }

        currentDir = Directory.GetParent(currentDir)?.FullName;
    }

    return null;
}

// Describes a CI repository content location and the member-reference node to rewrite within it.
internal sealed record MemberReferenceTarget(
    string Name,
    string[] RelativeDirSegments,
    string Xpath,
    bool UseGuid,
    bool SkipWhenZero,
    bool Recurse);

public class Member
{
    public int MemberID { get; set; }
    public string MemberEmail { get; set; } = "";
    public string MemberEnabled { get; set; } = "";
    public DateTime MemberCreated { get; set; }
    public Guid MemberGuid { get; set; }
    public string MemberName { get; set; } = "";
    public string MemberPassword { get; set; } = "";
    public string MemberIsExternal { get; set; } = "";
    public string MemberSecurityStamp { get; set; } = "";
    public string MemberFirstName { get; set; } = "";
    public string MemberLastName { get; set; } = "";
    public string MemberLinkedInIdentifier { get; set; } = "";
    public string MemberAvatarFileExtension { get; set; } = "";
    public string? MemberAdministratorNotes { get; set; }
    public string? MemberModerationStatus { get; set; }
    public string? MemberEmployerLink { get; set; }
    public string? MemberJobTitle { get; set; }
    public string MemberTimeZone { get; set; } = "";
    public string MemberCountry { get; set; } = "";
    public string? MemberBio { get; set; }
}

// Program class for user secrets support
public partial class Program { }
