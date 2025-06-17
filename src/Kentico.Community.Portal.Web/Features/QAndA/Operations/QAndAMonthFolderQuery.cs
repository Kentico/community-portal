using CMS.DataEngine;
using CMS.Membership;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAMonthFolderQuery(
    QAndAQuestionsRootPage ParentPage,
    int Year,
    int Month) : IQuery<WebPageFolder>, ICacheByValueQuery
{
    public string CacheValueKey => $"{Year}|{Month}|Channel|{ParentPage.SystemFields.WebPageItemWebsiteChannelId}";
}

public class QAndAMonthFolderQueryHandler(
    ContentItemQueryTools tools,
    IInfoProvider<UserInfo> users,
    IWebPageManagerFactory webPageManagerFactory,
    IWebPageFolderRetriever webPageFolderRetriever)
    : ContentItemQueryHandler<QAndAMonthFolderQuery, WebPageFolder>(tools)
{
    private readonly IInfoProvider<UserInfo> users = users;
    private readonly IWebPageManagerFactory webPageManagerFactory = webPageManagerFactory;
    private readonly IWebPageFolderRetriever webPageFolderRetriever = webPageFolderRetriever;

    public override async Task<WebPageFolder> Handle(QAndAMonthFolderQuery request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetPublicMemberContentAuthor();
        var webPageManager = webPageManagerFactory.Create(request.ParentPage.SystemFields.WebPageItemWebsiteChannelId, user.UserID);

        string yearName = request.Year.ToString();
        string monthName = request.Month.ToString("D2");

        var yearFolder = await GetOrCreateYearFolder(webPageManager, yearName, request, cancellationToken);
        var yearFolderPath = PathMatch.Children(yearFolder.WebPageItemTreePath);

        var monthFolders = await webPageFolderRetriever.Retrieve(
            PortalWebSiteChannel.CODE_NAME,
            yearFolderPath,
            q => q.WhereLike(nameof(WebPageFolder.WebPageItemName), $"{monthName}-%"),
            cancellationToken: cancellationToken);

        if (monthFolders.Any())
        {
            return monthFolders.First();
        }

        int monthFolderID = await webPageManager.CreateFolder(new CreateFolderParameters(monthName, PortalWebSiteChannel.DEFAULT_LANGUAGE)
        {
            ParentWebPageItemID = yearFolder.WebPageItemID,
            RequiresAuthentication = false
        }, CancellationToken.None);

        monthFolders = await webPageFolderRetriever.Retrieve(
            PortalWebSiteChannel.CODE_NAME,
            yearFolderPath,
            q => q.WhereEquals(nameof(WebPageFolder.WebPageItemID), monthFolderID),
            cancellationToken: cancellationToken);

        return monthFolders.First();
    }

    private async Task<WebPageFolder> GetOrCreateYearFolder(IWebPageManager webPageManager, string yearName, QAndAMonthFolderQuery request, CancellationToken cancellationToken)
    {
        var parentPath = PathMatch.Children(request.ParentPage.SystemFields.WebPageItemTreePath);

        var yearFolders = await webPageFolderRetriever.Retrieve(
            PortalWebSiteChannel.CODE_NAME,
            parentPath,
            q => q.WhereLike(nameof(WebPageFolder.WebPageItemName), $"{yearName}-%"),
            cancellationToken: cancellationToken);

        if (yearFolders.Any())
        {
            return yearFolders.First();
        }

        int yearFolderID = await webPageManager.CreateFolder(new CreateFolderParameters(yearName, PortalWebSiteChannel.DEFAULT_LANGUAGE)
        {
            ParentWebPageItemID = request.ParentPage.SystemFields.WebPageItemID,
            RequiresAuthentication = false
        }, CancellationToken.None);

        yearFolders = await webPageFolderRetriever.Retrieve(
            PortalWebSiteChannel.CODE_NAME,
            parentPath,
            q => q.WhereEquals(nameof(WebPageFolder.WebPageItemID), yearFolderID),
            cancellationToken: cancellationToken);

        return yearFolders.First();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndAMonthFolderQuery query, WebPageFolder result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(WebPageItemInfo.OBJECT_TYPE);
}
