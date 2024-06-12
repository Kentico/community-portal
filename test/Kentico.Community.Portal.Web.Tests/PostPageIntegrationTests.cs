using CMS.ContentEngine;
using CMS.Core;
using CMS.Tests;
using CMS.Websites;
using Kentico.Community.Portal.Core.Content;

namespace Kentico.Community.Portal.Web.Tests;

public class PostPageIntegrationTests : IntegrationTests
{
    [Test]
    public async Task Database_Should_Have_Multiple_PostPages()
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.OfContentType(BlogPostPage.CONTENT_TYPE_NAME));

        var executor = Service.Resolve<IContentQueryExecutor>();

        var posts = await executor.GetMappedWebPageResult<BlogPostPage>(b);

        Assert.That(posts.ToList(), Has.Count.GreaterThanOrEqualTo(3));
    }
}
