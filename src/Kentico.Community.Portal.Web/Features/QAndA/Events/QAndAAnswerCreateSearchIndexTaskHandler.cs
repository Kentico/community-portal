using CMS.ContentEngine;
using CMS.Core;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Lucene.Indexing;

namespace Kentico.Community.Portal.Web.Features.QAndA.Events;

/// <summary>
/// Ensures that new <see cref="QAndAAnswerDataInfo" /> records
/// trigger an index update of their associated questions since the Lucene
/// integration doesn't yet track object graphs
/// </summary>
public class QAndAAnswerCreateSearchIndexTaskHandler
{
    private readonly IHttpContextAccessor accessor;
    private readonly IWebPageQueryResultMapper mapper;
    private readonly IWebsiteChannelContext channelContext;
    private readonly IContentQueryExecutor executor;
    private readonly IEventLogService log;
    private readonly ILuceneTaskLogger taskLogger;

    public QAndAAnswerCreateSearchIndexTaskHandler(
        IHttpContextAccessor accessor,
        IWebPageQueryResultMapper mapper,
        IWebsiteChannelContext channelContext,
        IContentQueryExecutor executor,
        IEventLogService log,
        ILuceneTaskLogger taskLogger)
    {
        this.accessor = accessor;
        this.mapper = mapper;
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
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), questionWebPageID));
            });

        var page = (await executor.GetWebPageResult(b, mapper.Map<QAndAQuestionPage>)).FirstOrDefault();
        if (page is null)
        {
            log.LogWarning(
                source: nameof(QAndAAnswerCreateSearchIndexTaskHandler),
                eventCode: "MISSING_QUESTION",
                eventDescription: $"Could not find question web site page [{questionWebPageID}] for answer [{answer.QAndAAnswerDataGUID}].{Environment.NewLine}Skipping search indexing.");

            return;
        }

        var model = new IndexEventWebPageItemModel(
            page.SystemFields.WebPageItemID,
            page.SystemFields.WebPageItemGUID,
            "en-US",
            QAndAQuestionPage.CONTENT_TYPE_NAME,
            page.SystemFields.WebPageItemName,
            page.SystemFields.ContentItemIsSecured,
            page.SystemFields.ContentItemContentTypeID,
            page.SystemFields.ContentItemCommonDataContentLanguageID,
            channelContext.WebsiteChannelName,
            page.SystemFields.WebPageItemTreePath,
            page.SystemFields.WebPageItemParentID,
            page.SystemFields.WebPageItemOrder)
        { };

        await taskLogger.HandleEvent(model, WebPageEvents.Publish.Name);
    }
}
