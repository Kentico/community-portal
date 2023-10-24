namespace Kentico.Community.Portal.Web.E2E.Tests;

public class CommunityPageTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions() =>
        new()
        {
            ColorScheme = ColorScheme.Light,
            ViewportSize = new()
            {
                Width = 1920,
                Height = 1080
            },
            BaseURL = "https://localhost:45039",
            IgnoreHTTPSErrors = true
        };
}
