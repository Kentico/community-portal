using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionPageQuery(RoutedWebPage Page) : WebPageRoutedQuery<QAndAQuestionPage>(Page);
public class QAndAQuestionPageQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<QAndAQuestionPageQuery, QAndAQuestionPage>(tools)
{
    public override async Task<QAndAQuestionPage> Handle(QAndAQuestionPageQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.Page, p => p.WithLinkedItems(1));

        var pages = await Executor.GetMappedWebPageResult<QAndAQuestionPage>(b, DefaultQueryOptions, cancellationToken);

        return pages.First();
    }
}
