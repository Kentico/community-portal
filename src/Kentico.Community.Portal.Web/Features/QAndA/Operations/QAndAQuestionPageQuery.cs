using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<QAndAQuestionPage>(Page);
public class QAndAQuestionPageQueryHandler : WebPageQueryHandler<QAndAQuestionPageQuery, QAndAQuestionPage>
{
    public QAndAQuestionPageQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<QAndAQuestionPage> Handle(QAndAQuestionPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page.WebsiteChannelName, request.Page, queryParameters =>
        {
            _ = queryParameters.WithLinkedItems(1);
        });

        var pages = await Executor.GetWebPageResult(b, WebPageMapper.Map<QAndAQuestionPage>, DefaultQueryOptions, cancellationToken);

        return pages.First();
    }
}
