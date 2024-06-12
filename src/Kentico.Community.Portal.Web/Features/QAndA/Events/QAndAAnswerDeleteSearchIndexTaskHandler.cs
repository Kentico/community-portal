using CMS.ContentEngine;
using CMS.Core;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Community.Portal.Web.Features.QAndA.Events;

/// <summary>
/// Ensures that deleted <see cref="QAndAAnswerDataInfo" /> records
/// trigger an index update of their associated questions since the Lucene
/// integration doesn't yet track object graphs
/// </summary>
public class QAndAAnswerDeleteSearchIndexTaskHandler(
    IHttpContextAccessor accessor,
    IWebsiteChannelContext channelContext,
    IContentQueryExecutor executor,
    IEventLogService log,
    ILuceneTaskLogger taskLogger)
{
    private readonly IHttpContextAccessor accessor = accessor;
    private readonly IWebsiteChannelContext channelContext = channelContext;
    private readonly IContentQueryExecutor executor = executor;
    private readonly IEventLogService log = log;
    private readonly ILuceneTaskLogger taskLogger = taskLogger;

    public async Task Handle(QAndAAnswerDataInfo answer)
    {
        /*
         * Only perform search indexing when a site is available (eg not during CI restore)
         */
        if (accessor.HttpContext is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(channelContext.WebsiteChannelName))
        {
            return;
        }

        int questionWebPageID = answer.QAndAAnswerDataQuestionWebPageItemID;

        var b = new ContentItemQueryBuilder()
            .ForContentTypes(q => q
                .OfContentType(QAndAQuestionPage.CONTENT_TYPE_NAME)
                .ForWebsite([questionWebPageID]));

        var page = (await executor.GetMappedWebPageResult<QAndAQuestionPage>(b)).FirstOrDefault();
        if (page is null)
        {
            log.LogWarning(
                source: nameof(QAndAAnswerCreateSearchIndexTaskHandler),
                eventCode: "MISSING_QUESTION",
                eventDescription: $"Could not find question web site page [{questionWebPageID}] for answer [{answer.QAndAAnswerDataGUID}].{Environment.NewLine}Skipping search index update.");

            return;
        }

        var model = new IndexEventWebPageItemModel(
            page.SystemFields.WebPageItemID,
            page.SystemFields.WebPageItemGUID,
            PortalWebSiteChannel.DEFAULT_LANGUAGE,
            QAndAQuestionPage.CONTENT_TYPE_NAME,
            page.SystemFields.WebPageItemName,
            page.SystemFields.ContentItemIsSecured,
            page.SystemFields.ContentItemContentTypeID,
            page.SystemFields.ContentItemCommonDataContentLanguageID,
            channelContext.WebsiteChannelName,
            page.SystemFields.WebPageItemTreePath,
            page.SystemFields.WebPageItemParentID,
            page.SystemFields.WebPageItemOrder);

        await taskLogger.HandleEvent(model, WebPageEvents.Publish.Name);
    }
}
