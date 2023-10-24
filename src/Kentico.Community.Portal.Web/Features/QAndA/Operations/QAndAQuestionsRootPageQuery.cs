using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionsRootPageQuery : IQuery<QAndAQuestionsRootPage>;
public class QAndAQuestionsRootPageQueryHandler : ContentItemQueryHandler<QAndAQuestionsRootPageQuery, QAndAQuestionsRootPage>
{
    public QAndAQuestionsRootPageQueryHandler(ContentItemQueryTools tools) : base(tools) { }

    public override async Task<QAndAQuestionsRootPage> Handle(QAndAQuestionsRootPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(QAndAQuestionsRootPage.CONTENT_TYPE_NAME, queryParameters =>
        {
            _ = queryParameters
                .ForWebsite(WebsiteChannelContextContext.WebsiteChannelName)
                .TopN(1);
        });

        var pages = await Executor.GetWebPageResult(b, WebPageMapper.Map<QAndAQuestionsRootPage>, DefaultQueryOptions, cancellationToken);

        return pages.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndAQuestionsRootPageQuery query, QAndAQuestionsRootPage result, ICacheDependencyKeysBuilder builder) =>
        builder.AllContentItems(QAndAQuestionsRootPage.CONTENT_TYPE_NAME);
}
