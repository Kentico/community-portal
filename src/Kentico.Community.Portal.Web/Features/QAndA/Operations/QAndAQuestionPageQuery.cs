using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionPageQuery(IRoutedWebPage Page) : WebPageRoutedQuery<QAndAQuestionPageQueryResponse>(Page);
public record QAndAQuestionPageQueryResponse(QAndAQuestionPage Page);
public class QAndAQuestionPageQueryHandler : WebPageQueryHandler<QAndAQuestionPageQuery, QAndAQuestionPageQueryResponse>
{
    public QAndAQuestionPageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<QAndAQuestionPageQueryResponse> Handle(QAndAQuestionPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(WebsiteChannelContext, QAndAQuestionPage.CONTENT_TYPE_NAME, request.Page, queryParameters =>
        {
            _ = queryParameters.WithLinkedItems(1);
        });

        var pages = await Executor.GetWebPageResult(b, Mapper.Map<QAndAQuestionPage>, DefaultQueryOptions, cancellationToken);

        return new(pages.First());
    }
}
