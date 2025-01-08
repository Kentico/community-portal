using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndALandingPageQuery(string ChannelName) : IQuery<Maybe<QAndALandingPage>>, IChannelContentQuery;
public class QAndALandingPageQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<QAndALandingPageQuery, Maybe<QAndALandingPage>>(tools)
{
    public override async Task<Maybe<QAndALandingPage>> Handle(QAndALandingPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(
                q => q
                    .OfContentType(QAndALandingPage.CONTENT_TYPE_NAME)
                    .WithContentTypeFields()
                    .ForWebsite(request.ChannelName))
            .Parameters(q => q.TopN(1));

        var pages = await Executor.GetMappedWebPageResult<QAndALandingPage>(b, DefaultQueryOptions, cancellationToken);

        return pages.TryFirst();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndALandingPageQuery query, Maybe<QAndALandingPage> result, ICacheDependencyKeysBuilder builder) =>
        builder.AllContentItems(QAndALandingPage.CONTENT_TYPE_NAME);
}
