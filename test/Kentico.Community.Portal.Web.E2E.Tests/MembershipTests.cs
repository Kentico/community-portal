using System.Configuration;
using System.Data;
using AngleSharp;
using AngleSharp.Html.Dom;
using Kentico.Community.Portal.Core.Modules.Membership;
using Microsoft.Data.SqlClient;

namespace Kentico.Community.Portal.Web.E2E.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class MembershipTests : CommunityPageTests
{
    public string UserName { get; } = $"testuser-{Guid.NewGuid()}";
    public string Email { get; } = $"testuser-{Guid.NewGuid()}@kentico.local";
    public string Password { get; } = $"Pass@12345";
    public string NewPassword { get; } = $"Pass@123456";

    [Test]
    public async Task User_Can_Perform_Registration_And_Login()
    {
        TestContext.Out.WriteLine("Navigate from home to login");

        _ = await Page.GotoAsync("/");
        await Page.Locator("nav [test-id=signInLinkNav]").ClickAsync();
        await Page.WaitForURLAsync("**/login?returnURL=/");

        TestContext.Out.WriteLine("Navigate to register");

        await Page.Locator("[test-id=registerLink]").ClickAsync();
        await Page.WaitForURLAsync("**/register");

        TestContext.Out.WriteLine("Submit the registration form");

        await Page.Locator("[test-id=userName]").FillAsync(UserName);
        await Page.Locator("[test-id=email]").FillAsync(Email);
        await Page.Locator("[test-id=password]").FillAsync(Password);
        await Page.Locator("[test-id=passwordConfirmation]").FillAsync(Password);
        await Page.Locator("[test-id=consentAgreement]").CheckAsync();
        await Page.Locator("form[test-id=registerForm] button[type=submit]").ClickAsync();
        _ = await Page.WaitForResponseAsync("**/registrationwidget/register");
        await Expect(Page.Locator("[test-id=confirmationEmailMessage]")).ToBeVisibleAsync();

        TestContext.Out.WriteLine("Login attempt before confirmation");

        await Page.Locator("[test-id=registrationSignInLink]").ClickAsync();
        await Page.WaitForURLAsync("**/login");

        await Page.Locator("[test-id=userNameOrEmail]").FillAsync(UserName);
        await Page.Locator("[test-id=password]").FillAsync(Password);
        await Page.Locator("form[test-id=signInForm] button[type=submit]").ClickAsync();
        await Page.WaitForResponseAsync("**/loginwidget/login");
        await Expect(Page.Locator("form[test-id=emailNotConfirmedForm]")).ToBeVisibleAsync();

        TestContext.Out.WriteLine("Confirm email address");

        string confirmationEmailURL = await GetConfirmationEmailURL();
        _ = await Page.GotoAsync(confirmationEmailURL);

        await Expect(Page.Locator("[test-id=confirmationMessage]")).ToBeVisibleAsync();
        await Page.Locator("[test-id=confirmationSignInLink]").ClickAsync();
        await Page.WaitForURLAsync("**/login");

        TestContext.Out.WriteLine("Submit login form");

        await Page.Locator("[test-id=userNameOrEmail]").FillAsync(UserName);
        await Page.Locator("[test-id=password]").FillAsync(Password);
        await Page.Locator("form[test-id=signInForm] button[type=submit]").ClickAsync();
        await Page.WaitForResponseAsync("**/loginwidget/login");

        TestContext.Out.WriteLine("Validate session is authenticated");

        await Expect(Page.Locator("nav [test-id=signInLinkNav]")).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("nav [test-id=username]")).ToHaveTextAsync(UserName);

        TestContext.Out.WriteLine("Logout");

        await Page.Locator("[test-id=username]").HoverAsync();
        await Page.Locator("form[test-id=logoutForm] button[type=submit]").ClickAsync();
        await Page.WaitForURLAsync("**/");

        TestContext.Out.WriteLine("Validate session is not authenticated");

        await Expect(Page.Locator("nav [test-id=signInLinkNav]")).ToBeVisibleAsync();
        await Expect(Page.Locator("nav [test-id=username]")).Not.ToBeVisibleAsync();

        TestContext.Out.WriteLine("Login disabled for spam account");

        await SetModerationStatus(ModerationStatuses.Spam);
        await Page.Locator("nav [test-id=signInLinkNav]").ClickAsync();
        await Page.WaitForURLAsync("**/login?returnURL=/");

        await Page.Locator("[test-id=userNameOrEmail]").FillAsync(UserName);
        await Page.Locator("[test-id=password]").FillAsync(Password);
        await Page.Locator("form[test-id=signInForm] button[type=submit]").ClickAsync();
        _ = await Page.WaitForResponseAsync("**/loginwidget/login");

        await Expect(Page.Locator("[test-id=accountModerationMessage]")).ToBeVisibleAsync();

        await SetModerationStatus(ModerationStatuses.None);

        TestContext.Out.WriteLine("Account recovery with forgot password");

        await Page.Locator("[test-id=accountRecoveryLink]").ClickAsync();
        await Page.WaitForURLAsync("**/account-recovery");

        await Page.Locator("[test-id=email]").FillAsync(Email);
        await Page.Locator("form[test-id=requestRecoveryEmailForm] button[type=submit]").ClickAsync();
        _ = await Page.WaitForResponseAsync("**/accountrecoverywidget/requestrecoveryemail");

        string recoveryEmailURL = await GetRecoveryEmailURL();
        _ = await Page.GotoAsync(recoveryEmailURL);

        TestContext.Out.WriteLine("Reset password");

        await Page.Locator("[test-id=password]").FillAsync(NewPassword);
        await Page.Locator("[test-id=passwordConfirmation]").FillAsync(NewPassword);
        await Page.Locator("form[test-id=resetPasswordForm] button[type=submit]").ClickAsync();
        _ = await Page.WaitForResponseAsync("**/resetpasswordwidget/resetpassword");

        await Expect(Page.Locator("[test-id=resetPasswordConfirmation]")).ToBeVisibleAsync();
        await Page.Locator("[test-id=signInLinkResetPasswordConfirmation]").ClickAsync();
        await Page.WaitForURLAsync("**/login");

        TestContext.Out.WriteLine("Sign in");

        await Page.Locator("[test-id=userNameOrEmail]").FillAsync(UserName);
        await Page.Locator("[test-id=password]").FillAsync(NewPassword);
        await Page.Locator("form[test-id=signInForm] button[type=submit]").ClickAsync();
        _ = await Page.WaitForResponseAsync("**/loginwidget/login");

        TestContext.Out.WriteLine("Validate session is authenticated");

        await Expect(Page.Locator("nav [test-id=signInLinkNav]")).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("nav [test-id=username]")).ToHaveTextAsync(UserName);

        await ResetMemberEmails();
    }

    private async Task ResetMemberEmails()
    {
        string connectionString = GetConnectionString();
        string query = """
            DELETE
            FROM CMS_Email
            WHERE EmailTo = @EmailTo
        """;

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = new SqlCommand(query, connection);
        var parameter = new SqlParameter("@EmailTo", SqlDbType.NVarChar)
        {
            Value = Email
        };
        _ = command.Parameters.Add(parameter);
        using var reader = await command.ExecuteReaderAsync();
    }

    private async Task<string> GetConfirmationEmailURL()
    {
        string connectionString = GetConnectionString();
        string query = """
            SELECT EmailBody 
            FROM CMS_Email
            WHERE EmailTo = @EmailTo AND EmailSubject = 'Confirm your email for your new Kentico Community Portal account'
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
        using var reader = await command.ExecuteReaderAsync();

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

    public async Task SetModerationStatus(ModerationStatuses status)
    {
        string connectionString = GetConnectionString();
        string query = """
            UPDATE CMS_Member
            SET MemberModerationStatus = @ModerationStatus
            WHERE MemberName = @UserName
        """;

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand(query, connection)
        {
            CommandType = CommandType.Text
        };

        command.Parameters.AddWithValue("@ModerationStatus", status.ToString());
        command.Parameters.AddWithValue("@UserName", UserName);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<string> GetRecoveryEmailURL()
    {
        string connectionString = GetConnectionString();
        string query = """
            SELECT EmailBody 
            FROM CMS_Email
            WHERE EmailTo = @EmailTo AND EmailSubject = 'Kentico Community Portal - Reset Password Confirmation'
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
        using var reader = await command.ExecuteReaderAsync();

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
