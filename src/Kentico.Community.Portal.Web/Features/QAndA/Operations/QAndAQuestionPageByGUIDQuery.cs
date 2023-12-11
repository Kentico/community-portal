using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionPageByGUIDQuery(Guid WebPageGUID, string ChannelName) : WebPageByGUIDQuery<QAndAQuestionPage>(WebPageGUID), IChannelContentQuery;
public class QAndAQuestionPageByGUIDQueryHandler : WebPageQueryHandler<QAndAQuestionPageByGUIDQuery, QAndAQuestionPage>
{
    public QAndAQuestionPageByGUIDQueryHandler(WebPageQueryTools tools) : base(tools) { }

    public override async Task<QAndAQuestionPage> Handle(QAndAQuestionPageByGUIDQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForWebPage(request.ChannelName, QAndAQuestionPage.CONTENT_TYPE_NAME, request.WebPageGUID, queryParameters =>
        {
            _ = queryParameters.WithLinkedItems(1);
        });

        var pages = await Executor.GetWebPageResult(b, WebPageMapper.Map<QAndAQuestionPage>, DefaultQueryOptions, cancellationToken);

        return pages.First();
    }
}
