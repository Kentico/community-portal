using System.Configuration;
using System.Data;
using AngleSharp;
using AngleSharp.Html.Dom;
using Microsoft.Data.SqlClient;

namespace Kentico.Community.Portal.Web.E2E.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class MembershipTests : CommunityPageTests
{
    public string UserName { get; } = $"testuser-{Guid.NewGuid()}";
    public string Email { get; } = $"testuser-{Guid.NewGuid()}@kentico.com";
    public string Password { get; } = $"Pass@12345";

    [Test]
    public async Task User_Can_Perform_Registration_And_Login()
    {
        TestContext.WriteLine("Navigate from home to login");

        _ = await Page.GotoAsync("/");
        await Page.Locator("nav [test-id=signIn]").ClickAsync();
        await Page.WaitForURLAsync("**/authentication/login?returnURL=%2F");

        TestContext.WriteLine("Navigate to register");

        await Page.Locator("[test-id=register]").ClickAsync();
        await Page.WaitForURLAsync("**/registration/register");

        TestContext.WriteLine("Submit the registration form");

        await Page.Locator("[test-id=userName]").FillAsync(UserName);
        await Page.Locator("[test-id=email]").FillAsync(Email);
        await Page.Locator("[test-id=password]").FillAsync(Password);
        await Page.Locator("[test-id=passwordConfirmation]").FillAsync(Password);
        await Page.Locator("[test-id=consentAgreement]").CheckAsync();
        await Page.Locator("form[test-id=register] button[type=submit]").ClickAsync();
        _ = await Page.WaitForResponseAsync("**/registration/register");

        TestContext.WriteLine("Confirm email address");

        string confirmationEmailURL = await GetConfirmationEmailURL();

        _ = await Page.GotoAsync(confirmationEmailURL);

        await Expect(Page.Locator("[test-id=confirmationMessage]")).ToBeVisibleAsync();
        await Page.Locator("[test-id=confirmationSignIn]").ClickAsync();
        await Page.WaitForURLAsync("**/authentication/login");

        TestContext.WriteLine("Submit login form");

        await Page.Locator("[test-id=userNameOrEmail]").FillAsync(UserName);
        await Page.Locator("[test-id=password]").FillAsync(Password);
        await Page.Locator("form[test-id=signIn] button[type=submit]").ClickAsync();
        await Page.WaitForURLAsync("**/");

        TestContext.WriteLine("Validate session is authenticated");

        await Expect(Page.Locator("nav [test-id=signIn]")).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("nav [test-id=username]")).ToHaveTextAsync(UserName);

        TestContext.WriteLine("Logout");

        await Page.Locator("[test-id=username]").HoverAsync();
        await Page.Locator("[test-id=logout]").ClickAsync();
        await Page.WaitForURLAsync("**/");

        TestContext.WriteLine("Validate session is not authenticated");

        await Expect(Page.Locator("nav [test-id=signIn]")).ToBeVisibleAsync();
        await Expect(Page.Locator("nav [test-id=username]")).Not.ToBeVisibleAsync();
    }

    private async Task<string> GetConfirmationEmailURL()
    {
        string connectionString = GetConnectionString();
        string query = """
            SELECT EmailBody 
            FROM CMS_Email
            WHERE EmailTo = @EmailTo
            ORDER BY EmailID Desc
        """;

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = new SqlCommand(query, connection);
        var parameter = new SqlParameter("@EmailTo", SqlDbType.NVarChar)
        {
            Value = Email
        };
        _ = command.Parameters.Add(parameter);
        using var reader = command.ExecuteReader();

        string emailBody = "";

        while (await reader.ReadAsync())
        {
            emailBody = reader.GetString(0);
        }

        var context = BrowsingContext.New(AngleSharp.Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(emailBody));
        var el = document.QuerySelector("a[data-confirmation-url]");

        return el is IHtmlAnchorElement anchorEl
            ? anchorEl.Href
            : "";
    }

    private static string GetConnectionString()
    {
        var settings = GetConnectionStringSettings("Tests.Local.config");

        if (settings is not null)
        {
            return settings.ConnectionString;
        }

        settings = GetConnectionStringSettings("Tests.config");

        if (settings is not null)
        {
            return settings.ConnectionString;
        }

        throw new Exception("Could not find [CMSTestConnectionString] in any accessible configuration file");

        static ConnectionStringSettings? GetConnectionStringSettings(string filename)
        {
            var config = ConfigurationManager.OpenMappedExeConfiguration(new()
            {
                ExeConfigFilename = filename
            }, ConfigurationUserLevel.None);

            return config.ConnectionStrings.ConnectionStrings["CMSTestConnectionString"];
        }
    }
}
