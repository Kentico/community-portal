using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Community.Portal.Web.Features.QAndA.Events;

/// <summary>
/// Ensures changes to <see cref="QAndAAnswerDataInfo" /> records
/// trigger an index update of their associated questions since the Lucene
/// integration doesn't yet track object graphs
/// </summary>
public class QAndAAnswerSearchIndexTaskHandler(
    IChannelDataProvider channelProvider,
    IContentQueryExecutor executor,
    ILogger<QAndAAnswerSearchIndexTaskHandler> logger,
    ILuceneTaskLogger taskLogger) :
        IInfoObjectEventHandler<InfoObjectBeforeInsertEvent<QAndAAnswerDataInfo>>,
        IInfoObjectEventHandler<InfoObjectBeforeUpdateEvent<QAndAAnswerDataInfo>>,
        IInfoObjectEventHandler<InfoObjectBeforeDeleteEvent<QAndAAnswerDataInfo>>
{
    private readonly IChannelDataProvider channelProvider = channelProvider;
    private readonly IContentQueryExecutor executor = executor;
    private readonly ILogger<QAndAAnswerSearchIndexTaskHandler> logger = logger;
    private readonly ILuceneTaskLogger taskLogger = taskLogger;

    public void Handle(InfoObjectBeforeInsertEvent<QAndAAnswerDataInfo> infoObjectEvent) =>
        Handle(infoObjectEvent.InfoObject).GetAwaiter().GetResult();
    public void Handle(InfoObjectBeforeUpdateEvent<QAndAAnswerDataInfo> infoObjectEvent) =>
        Handle(infoObjectEvent.InfoObject).GetAwaiter().GetResult();
    public void Handle(InfoObjectBeforeDeleteEvent<QAndAAnswerDataInfo> infoObjectEvent) =>
        Handle(infoObjectEvent.InfoObject).GetAwaiter().GetResult();
    public async Task HandleAsync(InfoObjectBeforeInsertEvent<QAndAAnswerDataInfo> infoObjectEvent, CancellationToken cancellationToken) =>
        await Handle(infoObjectEvent.InfoObject, cancellationToken);
    public async Task HandleAsync(InfoObjectBeforeUpdateEvent<QAndAAnswerDataInfo> infoObjectEvent, CancellationToken cancellationToken) =>
        await Handle(infoObjectEvent.InfoObject, cancellationToken);
    public async Task HandleAsync(InfoObjectBeforeDeleteEvent<QAndAAnswerDataInfo> infoObjectEvent, CancellationToken cancellationToken) =>
        await Handle(infoObjectEvent.InfoObject, cancellationToken);

    public async Task Handle(QAndAAnswerDataInfo answer, CancellationToken cancellationToken = default)
    {
        int questionWebPageID = answer.QAndAAnswerDataQuestionWebPageItemID;

        var b = new ContentItemQueryBuilder()
            .ForContentTypes(q => q
                .OfContentType(QAndAQuestionPage.CONTENT_TYPE_NAME)
                .ForWebsite([questionWebPageID]));

        var page = (await executor.GetMappedWebPageResult<QAndAQuestionPage>(b, cancellationToken: cancellationToken)).FirstOrDefault();
        if (page is null)
        {
            logger.LogWarning(new EventId(0, "MISSING_QUESTION"), "Missing question page {QuestionWebPageID} for answer {AnswerGuid}. Skipping search index update.", questionWebPageID, answer.QAndAAnswerDataGUID);

            return;
        }

        string? channelName = await channelProvider.GetChannelNameByWebsiteChannelID(page.SystemFields.WebPageItemWebsiteChannelId, cancellationToken);
        if (channelName is null)
        {
            logger.LogWarning(new EventId(0, "INVALID_CHANNEL"), "Could not retrieve channel name for website channel {WebsiteChannelID} for answer {AnswerGuid}. Skipping search index update.", page.SystemFields.WebPageItemWebsiteChannelId, answer.QAndAAnswerDataGUID);

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
            channelName,
            page.SystemFields.WebPageItemTreePath,
            page.SystemFields.WebPageItemParentID,
            page.SystemFields.WebPageItemOrder);

        await taskLogger.HandleEvent(model, WebPageEvents.Publish.Name);
    }
}
