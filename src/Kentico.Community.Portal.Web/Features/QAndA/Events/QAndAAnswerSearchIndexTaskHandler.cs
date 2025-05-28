using CMS.ContentEngine;
using CMS.Core;
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
    IEventLogService log,
    ILuceneTaskLogger taskLogger) :
        IInfoObjectEventHandler<InfoObjectBeforeInsertEvent<QAndAAnswerDataInfo>>,
        IInfoObjectEventHandler<InfoObjectBeforeUpdateEvent<QAndAAnswerDataInfo>>,
        IInfoObjectEventHandler<InfoObjectBeforeDeleteEvent<QAndAAnswerDataInfo>>
{
    private readonly IChannelDataProvider channelProvider = channelProvider;
    private readonly IContentQueryExecutor executor = executor;
    private readonly IEventLogService log = log;
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
            log.LogWarning(
                source: nameof(QAndAAnswerSearchIndexTaskHandler),
                eventCode: "MISSING_QUESTION",
                eventDescription: $"Could not find question web site page [{questionWebPageID}] for answer [{answer.QAndAAnswerDataGUID}].{Environment.NewLine}Skipping search index update.");

            return;
        }

        string? channelName = await channelProvider.GetChannelNameByWebsiteChannelID(page.SystemFields.WebPageItemWebsiteChannelId, cancellationToken);
        if (channelName is null)
        {
            log.LogWarning(
                source: nameof(QAndAAnswerSearchIndexTaskHandler),
                eventCode: "INVALID_CHANNEL",
                eventDescription: $"Could not retrieve a channel name for website channel [{page.SystemFields.WebPageItemWebsiteChannelId}] for answer [{answer.QAndAAnswerDataGUID}].{Environment.NewLine}Skipping search index update.");

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
