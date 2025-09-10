/**
 * This application is used to clean and fix data from a PROD environment that is being restored locally before running CI Restore.
 * Processing this data is required because the CI process does not track Member data, but other objects it tracks are dependent on it
 */

using System.Text.Json;
using System.Xml;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

// Load member data from JSON file
string jsonFilePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "default-members.json");
string jsonContent = File.ReadAllText(jsonFilePath);
var members = JsonSerializer.Deserialize<Member[]>(jsonContent)
    ?? throw new Exception("Failed to deserialize default-members.json");

string solutionDirectory = FindSolutionFileDirectory(AppDomain.CurrentDomain.BaseDirectory)
    ?? throw new Exception("This application requires a parent .sln file");

// Clean up the database first
await CleanupMemberTable(members);

MigrateQuestions(solutionDirectory, members);
MigrateAnswers(solutionDirectory, members);
MigrateLinkContent(solutionDirectory, members);
MigrateIntegrationContent(solutionDirectory, members);
MigrateMemberProfileContent(solutionDirectory, members);
MigrateAuthorContent(solutionDirectory, members);

void MigrateQuestions(string rootPath, Member[] members)
{
    string directoryPath = Path.Join(rootPath, @"src\Kentico.Community.Portal.Web\App_Data\CIRepository\devnet\contentitemdata.kenticocommunity.qandaquestionpage");

    if (!Directory.Exists(directoryPath))
    {
        Console.WriteLine($"Directory not found: {directoryPath}. Skipping Questions migration.");
        return;
    }

    string[] xmlFiles = Directory.GetFiles(directoryPath, "*.xml", SearchOption.AllDirectories);

    foreach (string file in xmlFiles)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            // Find the <QAndAQuestionPageAuthorMemberID> element
            var authorMemberIdNode = xmlDoc.SelectSingleNode("//QAndAQuestionPageAuthorMemberID")!;

            // Don't replace the member for questions authored by the system
            if (authorMemberIdNode != null && int.Parse(authorMemberIdNode.InnerText) != 0)
            {
                var randomMember = members[new Random().Next(members.Length)];
                authorMemberIdNode.InnerText = randomMember.MemberID.ToString();
                xmlDoc.Save(file);

                Console.WriteLine($"Updated QAndAQuestionPageAuthorMemberID to {randomMember.MemberID} in file: {file}");
            }
            else
            {
                Console.WriteLine($"No <QAndAQuestionPageAuthorMemberID> node found in file: {file}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file {file}: {ex.Message}");
        }
    }

    Console.WriteLine("Processing completed.");
}

void MigrateAnswers(string rootPath, Member[] members)
{
    string directoryPath = Path.Join(rootPath, @"src\Kentico.Community.Portal.Web\App_Data\CIRepository\@global\kenticocommunity.qandaanswerdata");

    if (!Directory.Exists(directoryPath))
    {
        Console.WriteLine($"Directory not found: {directoryPath}. Skipping Answers migration.");
        return;
    }

    string[] xmlFiles = Directory.GetFiles(directoryPath, "*.xml");

    foreach (string file in xmlFiles)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            // Find the <GUID> element under <QAndAAnswerDataAuthorMemberID>
            var authorMemberIdNode = xmlDoc.SelectSingleNode("//QAndAAnswerDataAuthorMemberID/GUID")!;

            if (authorMemberIdNode != null)
            {
                var randomMember = members[new Random().Next(members.Length)];
                authorMemberIdNode.InnerText = randomMember.MemberGuid.ToString();
                xmlDoc.Save(file);

                Console.WriteLine($"Updated GUID to {randomMember.MemberGuid} (MemberID {randomMember.MemberID}) in file: {file}");
            }
            else
            {
                Console.WriteLine($"No <GUID> node found in file: {file}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file {file}: {ex.Message}");
        }
    }

    Console.WriteLine("Processing completed.");
}

void MigrateLinkContent(string rootPath, Member[] members)
{
    string directoryPath = Path.Join(rootPath, @"src\Kentico.Community.Portal.Web\App_Data\CIRepository\@global\contentitemdata.kenticocommunity.linkcontent");

    if (!Directory.Exists(directoryPath))
    {
        Console.WriteLine($"Directory not found: {directoryPath}. Skipping LinkContent migration.");
        return;
    }

    string[] xmlFiles = Directory.GetFiles(directoryPath, "*.xml", SearchOption.AllDirectories);

    foreach (string file in xmlFiles)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            // Find the <LinkContentMemberID> element
            var memberIdNode = xmlDoc.SelectSingleNode("//LinkContentMemberID")!;

            // Don't replace the member for content authored by the system
            if (memberIdNode != null && int.Parse(memberIdNode.InnerText) != 0)
            {
                var randomMember = members[new Random().Next(members.Length)];
                memberIdNode.InnerText = randomMember.MemberID.ToString();
                xmlDoc.Save(file);

                Console.WriteLine($"Updated LinkContentMemberID to {randomMember.MemberID} in file: {file}");
            }
            else
            {
                Console.WriteLine($"No <LinkContentMemberID> node found or value is 0 in file: {file}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file {file}: {ex.Message}");
        }
    }

    Console.WriteLine("LinkContent processing completed.");
}

void MigrateIntegrationContent(string rootPath, Member[] members)
{
    string directoryPath = Path.Join(rootPath, @"src\Kentico.Community.Portal.Web\App_Data\CIRepository\@global\contentitemdata.ke..integrationcontent@1dabf6434c");

    if (!Directory.Exists(directoryPath))
    {
        Console.WriteLine($"Directory not found: {directoryPath}. Skipping IntegrationContent migration.");
        return;
    }

    string[] xmlFiles = Directory.GetFiles(directoryPath, "*.xml", SearchOption.AllDirectories);

    foreach (string file in xmlFiles)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            // Find the <IntegrationContentAuthorMemberID> element
            var memberIdNode = xmlDoc.SelectSingleNode("//IntegrationContentAuthorMemberID")!;

            // Don't replace the member for content authored by the system
            if (memberIdNode != null && int.Parse(memberIdNode.InnerText) != 0)
            {
                var randomMember = members[new Random().Next(members.Length)];
                memberIdNode.InnerText = randomMember.MemberID.ToString();
                xmlDoc.Save(file);

                Console.WriteLine($"Updated IntegrationContentAuthorMemberID to {randomMember.MemberID} in file: {file}");
            }
            else
            {
                Console.WriteLine($"No <IntegrationContentAuthorMemberID> node found or value is 0 in file: {file}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file {file}: {ex.Message}");
        }
    }

    Console.WriteLine("IntegrationContent processing completed.");
}

void MigrateMemberProfileContent(string rootPath, Member[] members)
{
    string directoryPath = Path.Join(rootPath, @"src\Kentico.Community.Portal.Web\App_Data\CIRepository\@global\contentitemdata.ke..mberprofilecontent@8653edbc10");

    if (!Directory.Exists(directoryPath))
    {
        Console.WriteLine($"Directory not found: {directoryPath}. Skipping MemberProfileContent migration.");
        return;
    }

    string[] xmlFiles = Directory.GetFiles(directoryPath, "*.xml", SearchOption.AllDirectories);

    foreach (string file in xmlFiles)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            // Find the <MemberProfileContentMemberID> element
            var memberIdNode = xmlDoc.SelectSingleNode("//MemberProfileContentMemberID")!;

            // Don't replace the member for content authored by the system
            if (memberIdNode != null && int.Parse(memberIdNode.InnerText) != 0)
            {
                var randomMember = members[new Random().Next(members.Length)];
                memberIdNode.InnerText = randomMember.MemberID.ToString();
                xmlDoc.Save(file);

                Console.WriteLine($"Updated MemberProfileContentMemberID to {randomMember.MemberID} in file: {file}");
            }
            else
            {
                Console.WriteLine($"No <MemberProfileContentMemberID> node found or value is 0 in file: {file}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file {file}: {ex.Message}");
        }
    }

    Console.WriteLine("MemberProfileContent processing completed.");
}

void MigrateAuthorContent(string rootPath, Member[] members)
{
    string directoryPath = Path.Join(rootPath, @"src\Kentico.Community.Portal.Web\App_Data\CIRepository\@global\contentitemdata.kenticocommunity.authorcontent");

    if (!Directory.Exists(directoryPath))
    {
        Console.WriteLine($"Directory not found: {directoryPath}. Skipping AuthorContent migration.");
        return;
    }

    string[] xmlFiles = Directory.GetFiles(directoryPath, "*.xml", SearchOption.AllDirectories);

    foreach (string file in xmlFiles)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            // Find the <AuthorContentMemberID> element
            var memberIdNode = xmlDoc.SelectSingleNode("//AuthorContentMemberID")!;

            // Don't replace the member for content authored by the system (only update if field exists and is not 0)
            if (memberIdNode != null && int.Parse(memberIdNode.InnerText) != 0)
            {
                var randomMember = members[new Random().Next(members.Length)];
                memberIdNode.InnerText = randomMember.MemberID.ToString();
                xmlDoc.Save(file);

                Console.WriteLine($"Updated AuthorContentMemberID to {randomMember.MemberID} in file: {file}");
            }
            else
            {
                Console.WriteLine($"No <AuthorContentMemberID> node found or value is 0 in file: {file}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file {file}: {ex.Message}");
        }
    }

    Console.WriteLine("AuthorContent processing completed.");
}

async Task CleanupMemberTable(Member[] members)
{
    Console.WriteLine("Starting database cleanup...");

    string connectionString = GetConnectionString();

    // Safety confirmation
    Console.WriteLine("WARNING: This will delete all records from the CMS_Member table and replace them with test data.");
    Console.WriteLine("Press 'y' to continue or any other key to skip database cleanup:");

    var key = Console.ReadKey();
    Console.WriteLine(); // New line after key press

    if (key.KeyChar is not 'y' and not 'Y')
    {
        Console.WriteLine("Database cleanup skipped.");
        return;
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
        foreach (var member in members)
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

        Console.WriteLine($"Successfully inserted {members.Length} members into the database.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during database cleanup: {ex.Message}");
        throw;
    }
}

string GetConnectionString()
{
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .Build();

    string? connectionString = configuration["ConnectionStrings:CMSConnectionString"];

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new Exception("Connection string 'CMSConnectionString' not found in user secrets. Please ensure you have configured the user secrets for this project.");
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
    string currentDir = startPath;

    while (!string.IsNullOrEmpty(currentDir))
    {
        string[] solutionFiles = Directory.GetFiles(currentDir, "*.sln");
        if (solutionFiles.Length > 0)
        {
            return currentDir;
        }
        currentDir = Directory.GetParent(currentDir)?.FullName ?? "";
    }

    return null;
}

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
