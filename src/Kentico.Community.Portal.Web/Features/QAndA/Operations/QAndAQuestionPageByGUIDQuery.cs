using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionPageByGUIDQuery(Guid WebPageGUID) : WebPageByGUIDQuery<Maybe<QAndAQuestionPage>>(WebPageGUID);
public class QAndAQuestionPageByGUIDQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<QAndAQuestionPageByGUIDQuery, Maybe<QAndAQuestionPage>>(tools)
{
    public override async Task<Maybe<QAndAQuestionPage>> Handle(QAndAQuestionPageByGUIDQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(QAndAQuestionPage.CONTENT_TYPE_NAME, request.WebPageGUID, p => p.WithLinkedItems(1));

        var pages = await Executor.GetMappedWebPageResult<QAndAQuestionPage>(b, DefaultQueryOptions, cancellationToken);

        return pages.TryFirst();
    }
}
