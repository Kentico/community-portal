/**
 * This application is used to clean and fix data from a PROD environment that is being restored locally before running CI Restore.
 * Processing this data is required because the CI process does not track Member data, but other objects it tracks are dependent on it
 */

using System.Xml;

string solutionDirectory = FindSolutionFileDirectory(AppDomain.CurrentDomain.BaseDirectory)
    ?? throw new Exception("This application requires a parent .sln file");

MigrateQuestions(solutionDirectory);
MigrateAnswers(solutionDirectory);

static void MigrateQuestions(string rootPath)
{
    string directoryPath = Path.Join(rootPath, @"src\Kentico.Community.Portal.Web\App_Data\CIRepository\devnet\contentitemdata.kenticocommunity.qandaquestionpage");

    // CMS_Member.MemberID values
    int[] memberIDs = [1, 2, 3, 4];
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
                int randomID = memberIDs[new Random().Next(memberIDs.Length)];
                authorMemberIdNode.InnerText = randomID.ToString();
                xmlDoc.Save(file);

                Console.WriteLine($"Updated QAndAQuestionPageAuthorMemberID in file: {file}");
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

static void MigrateAnswers(string rootPath)
{
    string directoryPath = Path.Join(rootPath, @"src\Kentico.Community.Portal.Web\App_Data\CIRepository\@global\kenticocommunity.qandaanswerdata");

    // CMS_Member.MemberGUID values
    Guid[] guids =
    [
        Guid.Parse("b63e360d-4b76-4418-a8a7-affd51730462"),
        Guid.Parse("8fb0dad6-8c28-43e4-849b-684df65417a7"),
        Guid.Parse("7577a98f-036d-454e-8e19-5ff0881e6fc7"),
        Guid.Parse("f69d9d84-4ca9-4a3d-80f0-cff18438940a")
    ];
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
                var randomGuid = guids[new Random().Next(guids.Length)];
                authorMemberIdNode.InnerText = randomGuid.ToString();
                xmlDoc.Save(file);

                Console.WriteLine($"Updated GUID in file: {file}");
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
