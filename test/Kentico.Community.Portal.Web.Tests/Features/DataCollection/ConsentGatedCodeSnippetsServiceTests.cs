using Kentico.Community.Portal.Web.Features.DataCollection;
using Kentico.Xperience.TagManager.Rendering;

namespace Kentico.Community.Portal.Web.Tests.Features.DataCollection;

/// <summary>
/// The allowlist filter used before Analytics is allowed. E2E specs cover the gate itself.
/// </summary>
public class ConsentGatedCodeSnippetsServiceTests
{
    private static readonly CodeSnippetDto headTop = new() { ID = 1, Code = "<script></script>", Location = CodeSnippetLocations.HeadTop };
    private static readonly CodeSnippetDto bodyTop = new() { ID = 1, Code = "<noscript></noscript>", Location = CodeSnippetLocations.BodyTop };
    private static readonly CodeSnippetDto other = new() { ID = 2, Code = "<script></script>", Location = CodeSnippetLocations.HeadBottom };

    [Test]
    public void Keeps_Every_Location_Of_Allowed_Snippets_Only()
    {
        var snippets = new[] { headTop, bodyTop, other }.ToLookup(s => s.Location);

        var result = ConsentGatedCodeSnippetsService.OnlyAllowed(snippets, new HashSet<int> { 1 });

        Assert.Multiple(() =>
        {
            Assert.That(result[CodeSnippetLocations.HeadTop], Is.EqualTo(new[] { headTop }));
            Assert.That(result[CodeSnippetLocations.BodyTop], Is.EqualTo(new[] { bodyTop }));
            Assert.That(result[CodeSnippetLocations.HeadBottom], Is.Empty);
        });
    }

    [Test]
    public void No_Allowed_Ids_Keeps_Nothing()
    {
        var snippets = new[] { headTop, other }.ToLookup(s => s.Location);

        var result = ConsentGatedCodeSnippetsService.OnlyAllowed(snippets, new HashSet<int>());

        Assert.That(result.SelectMany(g => g), Is.Empty);
    }
}
