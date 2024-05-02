using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionsRootPageQuery(string ChannelName) : IQuery<QAndAQuestionsRootPage>, IChannelContentQuery;
public class QAndAQuestionsRootPageQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<QAndAQuestionsRootPageQuery, QAndAQuestionsRootPage>(tools)
{
    public override async Task<QAndAQuestionsRootPage> Handle(QAndAQuestionsRootPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(QAndAQuestionsRootPage.CONTENT_TYPE_NAME, queryParameters =>
        {
            _ = queryParameters
                .ForWebsite(request.ChannelName)
                .TopN(1);
        });

        var pages = await Executor.GetMappedWebPageResult<QAndAQuestionsRootPage>(b, DefaultQueryOptions, cancellationToken);

        return pages.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndAQuestionsRootPageQuery query, QAndAQuestionsRootPage result, ICacheDependencyKeysBuilder builder) =>
        builder.AllContentItems(QAndAQuestionsRootPage.CONTENT_TYPE_NAME);
}
