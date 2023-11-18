using CMS.ContentEngine;
using CMS.Core;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;

namespace Kentico.Community.Portal.Web.Features.QAndA.Events;

/// <summary>
/// Ensures that new <see cref="QAndAAnswerDataInfo" /> records
/// trigger an index update of their associated questions since the Lucene
/// integration doesn't yet track object graphs
/// </summary>
public class QAndAAnswerCreateSearchIndexTaskHandler
{
    private readonly IHttpContextAccessor accessor;
    private readonly IWebsiteChannelContext channelContext;
    private readonly IContentQueryExecutor executor;
    private readonly IEventLogService log;
    private readonly ILuceneTaskLogger taskLogger;

    public QAndAAnswerCreateSearchIndexTaskHandler(
        IHttpContextAccessor accessor,
        IWebsiteChannelContext channelContext,
        IContentQueryExecutor executor,
        IEventLogService log,
        ILuceneTaskLogger taskLogger)
    {
        this.accessor = accessor;
        this.channelContext = channelContext;
        this.executor = executor;
        this.log = log;
        this.taskLogger = taskLogger;
    }

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
            .ForContentType(QAndAQuestionPage.CONTENT_TYPE_NAME, queryParameters =>
            {
                _ = queryParameters
                    .ForWebsite(channelContext.WebsiteChannelName)
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), questionWebPageID))
                    .Columns(new[] { nameof(WebPageFields.WebPageItemGUID), nameof(WebPageFields.WebPageItemTreePath) });
            });

        var page = (await executor.GetWebPageResult(b, c => new { c.WebPageItemGUID, c.WebPageItemTreePath })).FirstOrDefault();
        if (page is null)
        {
            log.LogWarning(
                source: nameof(QAndAAnswerCreateSearchIndexTaskHandler),
                eventCode: "MISSING_QUESTION",
                eventDescription: $"Could not find question web site page [{questionWebPageID}] for answer [{answer.QAndAAnswerDataGUID}].{Environment.NewLine}Skipping search indexing.");

            return;
        }

        var model = new IndexedItemModel
        {
            ChannelName = channelContext.WebsiteChannelName,
            LanguageName = "en-US",
            TypeName = QAndAQuestionPage.CONTENT_TYPE_NAME,
            WebPageItemGuid = page.WebPageItemGUID,
            WebPageItemTreePath = page.WebPageItemTreePath
        };

        await taskLogger.HandleEvent(model, WebPageEvents.Publish.Name);
    }
}
