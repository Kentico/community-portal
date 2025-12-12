using CMS.ContentEngine;
using CMS.Core;
using CMS.Core.Internal;
using CMS.Tests;
using CMS.Websites;
using Kentico.Community.Portal.Core.Content;

namespace Kentico.Community.Portal.Web.Tests.Features.Blog;

public class PostPageIntegrationTests : IntegrationTests
{
    /// <summary>
    /// Resolves bug "Message: No service for type 'CMS.Core.Internal.IDateTimeNowService' has been registered."
    /// </summary>
    protected override void RegisterTestServices()
    {
        Service.Use<IDateTimeNowService, MockDateTimeNowService>();

        base.RegisterTestServices();
    }

    [Test]
    public async Task Database_Should_Have_Multiple_PostPages()
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.OfContentType(BlogPostPage.CONTENT_TYPE_NAME));

        var executor = Service.Resolve<IContentQueryExecutor>();

        var posts = await executor.GetMappedWebPageResult<BlogPostPage>(b);

        Assert.That(posts.Count(), Is.GreaterThanOrEqualTo(3));
    }
}

public class MockDateTimeNowService : IDateTimeNowService
{
    public DateTime GetDateTimeNow() => new(2025, 12, 01);
}
