using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Tests;
using CMS.Websites;
using Kentico.Community.Portal.Core.Content;

namespace Kentico.Community.Portal.Web.Tests;

public class PostPageIntegrationTests : IntegrationTests
{
    [Test]
    public async Task Database_Should_Have_Multiple_PostPages()
    {
        var provider = Service.Resolve<IInfoProvider<ChannelInfo>>();

        var portalChannel = provider.Get()
            .WhereEquals(nameof(ChannelInfo.ChannelType), "Website")
            .GetEnumerableTypedResult()
            .FirstOrDefault()!;

        var b = new ContentItemQueryBuilder()
            .ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParameters =>
            {
                queryParameters.ForWebsite(portalChannel.ChannelName);
            });

        var executor = Service.Resolve<IContentQueryExecutor>();
        var mapper = Service.Resolve<IWebPageQueryResultMapper>();

        var posts = await executor.GetWebPageResult(b, mapper.Map<BlogPostPage>);

        Assert.That(posts.ToList(), Has.Count.GreaterThanOrEqualTo(3));
    }
}
