namespace Kentico.Community.Portal.Web.E2E.Tests;

public class CommunityPageTests : PageTest
{
    protected Uri RootUri { get; private set; } = new("https://localhost:45039");

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions() ?? new();
        options.ColorScheme = ColorScheme.Light;
        options.ViewportSize = new()
        {
            Height = 1920,
            Width = 1080
        };
        options.BaseURL = RootUri.ToString();
        options.IgnoreHTTPSErrors = true;

        return options;
    }
}
