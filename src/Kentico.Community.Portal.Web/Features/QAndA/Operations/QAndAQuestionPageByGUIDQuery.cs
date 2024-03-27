using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionPageByGUIDQuery(Guid WebPageGUID, string ChannelName) : WebPageByGUIDQuery<QAndAQuestionPage>(WebPageGUID), IChannelContentQuery;
public class QAndAQuestionPageByGUIDQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<QAndAQuestionPageByGUIDQuery, QAndAQuestionPage>(tools)
{
    public override async Task<QAndAQuestionPage> Handle(QAndAQuestionPageByGUIDQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(QAndAQuestionPage.CONTENT_TYPE_NAME, request.WebPageGUID, p => p.WithLinkedItems(1));

        var pages = await Executor.GetMappedWebPageResult<QAndAQuestionPage>(b, DefaultQueryOptions, cancellationToken);

        return pages.First();
    }
}
