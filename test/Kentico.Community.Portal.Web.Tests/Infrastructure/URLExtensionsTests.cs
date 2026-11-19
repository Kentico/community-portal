using Kentico.Content.Web.Mvc;
using Microsoft.AspNetCore.Http;

namespace Kentico.Community.Portal.Web.Tests.Infrastructure;

public class URLExtensionsTests
{
    [TestCase("", "/member/5", "?utm_source=x", "https://example.com/member/5")]
    [TestCase("", "/blog/", "", "https://example.com/blog")]
    [TestCase("", "/", "?page=2", "https://example.com/")]
    [TestCase("", "", "", "https://example.com/")]
    [TestCase("/base", "/Q-and-A", "", "https://example.com/base/Q-and-A")]
    public void CanonicalURL_Is_Absolute_Without_Query_Or_Trailing_Slash(string pathBase, string path, string query, string expected)
    {
        var request = new DefaultHttpContext().Request;
        request.Scheme = "https";
        request.Host = new HostString("example.com");
        request.PathBase = pathBase;
        request.Path = path;
        request.QueryString = new QueryString(query);

        Assert.That(request.CanonicalURL(), Is.EqualTo(expected));
    }
}
