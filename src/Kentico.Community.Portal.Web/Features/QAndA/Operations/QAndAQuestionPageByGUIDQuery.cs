using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionPageByGUIDQuery(Guid WebPageGUID) : WebPageByGUIDQuery<QAndAQuestionPage>(WebPageGUID);
public class QAndAQuestionPageByGUIDQueryHandler : WebPageQueryHandler<QAndAQuestionPageByGUIDQuery, QAndAQuestionPage>
{
    public QAndAQuestionPageByGUIDQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<QAndAQuestionPage> Handle(QAndAQuestionPageByGUIDQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(WebsiteChannelContext, QAndAQuestionPage.CONTENT_TYPE_NAME, request.WebPageGUID, queryParameters =>
        {
            _ = queryParameters.WithLinkedItems(1);
        });

        var pages = await Executor.GetWebPageResult(b, Mapper.Map<QAndAQuestionPage>, DefaultQueryOptions, cancellationToken);

        return pages.First();
    }
}
