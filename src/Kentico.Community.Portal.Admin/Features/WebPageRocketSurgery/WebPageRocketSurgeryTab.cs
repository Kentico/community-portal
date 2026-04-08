using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Admin.Features.WebPageRocketSurgery;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Authentication;
using Kentico.Xperience.Admin.Websites.UIPages;

[assembly: UIPage(
    typeof(WebPageLayout),
    "rocket-surgery",
    typeof(WebPageRocketSurgeryTab),
    "Rocket Surgery",
    "@kentico-community/portal-web-admin/WebPageRocketSurgery",
    1000,
    Icon = Icons.WizardStick)]

namespace Kentico.Community.Portal.Admin.Features.WebPageRocketSurgery;

[UIEvaluatePermission(KenticoCommunityPermissions.ROCKET_SURGERY.VIEW.Name)]
public class WebPageRocketSurgeryTab(
    IAuthenticatedUserAccessor userAccessor,
    IWebPageManagerFactory webPageManagerFactory,
    IPageLinkGenerator pageLinkGenerator,
    IInfoProvider<WebPageItemInfo> webPageItemProvider,
    IInfoProvider<ContentLanguageInfo> contentLanguageProvider,
    IInfoProvider<ContentItemCommonDataInfo> contentItemCommonDataProvider,
    IUIPermissionEvaluator permissionEvaluator)
    : WebPageBase<WebPageRocketSurgeryTabClientProperties>(userAccessor, webPageManagerFactory, pageLinkGenerator)
{
    public const string UPDATE_TEMPLATE_COMMAND = "UpdateTemplateConfig";
    public const string UPDATE_WIDGETS_COMMAND = "UpdateWidgets";

    private readonly IInfoProvider<WebPageItemInfo> webPageItemProvider = webPageItemProvider;
    private readonly IInfoProvider<ContentLanguageInfo> contentLanguageProvider = contentLanguageProvider;
    private readonly IInfoProvider<ContentItemCommonDataInfo> contentItemCommonDataProvider = contentItemCommonDataProvider;
    private readonly IUIPermissionEvaluator permissionEvaluator = permissionEvaluator;

    public override async Task<WebPageRocketSurgeryTabClientProperties> ConfigureTemplateProperties(
        WebPageRocketSurgeryTabClientProperties properties)
    {
        var props = await base.ConfigureTemplateProperties(properties);

        var canEditResult = await permissionEvaluator.Evaluate(KenticoCommunityPermissions.ROCKET_SURGERY.EDIT.Name);

        props.CanEdit = canEditResult.Succeeded;
        props.UpdateTemplateCommandName = UPDATE_TEMPLATE_COMMAND;
        props.UpdateWidgetsCommandName = UPDATE_WIDGETS_COMMAND;

        var commonData = await GetDraftCommonDataInfo(default);
        if (commonData is null)
        {
            return props;
        }

        props.TemplateConfiguration = commonData.ContentItemCommonDataVisualBuilderTemplateConfiguration ?? "";
        props.WidgetsConfiguration = commonData.ContentItemCommonDataVisualBuilderWidgets ?? "";
        props.IsModifiable = canEditResult.Succeeded;

        return props;
    }

    [PageCommand(
        CommandName = UPDATE_TEMPLATE_COMMAND,
        Permission = KenticoCommunityPermissions.ROCKET_SURGERY.EDIT.Name)]
    public async Task<ICommandResponse> UpdateTemplateConfig(
        UpdateRocketSurgeryTemplateConfigParams commandParams,
        CancellationToken cancellationToken = default)
    {
        var commonData = await GetDraftCommonDataInfo(cancellationToken);
        if (commonData is null)
        {
            return Response().AddErrorMessage("No draft found for this web page. Create a draft first.");
        }

        commonData.ContentItemCommonDataVisualBuilderTemplateConfiguration = commandParams.TemplateConfiguration;
        contentItemCommonDataProvider.Set(commonData);

        return ResponseFrom(new { templateConfiguration = commandParams.TemplateConfiguration })
            .AddSuccessMessage("Template configuration updated.");
    }

    [PageCommand(
        CommandName = UPDATE_WIDGETS_COMMAND,
        Permission = KenticoCommunityPermissions.ROCKET_SURGERY.EDIT.Name)]
    public async Task<ICommandResponse> UpdateWidgets(
        UpdateRocketSurgeryWidgetsParams commandParams,
        CancellationToken cancellationToken = default)
    {
        var commonData = await GetDraftCommonDataInfo(cancellationToken);
        if (commonData is null)
        {
            return Response().AddErrorMessage("No draft found for this web page. Create a draft first.");
        }

        commonData.ContentItemCommonDataVisualBuilderWidgets = commandParams.WidgetsConfiguration;
        contentItemCommonDataProvider.Set(commonData);

        return ResponseFrom(new { widgetsConfiguration = commandParams.WidgetsConfiguration })
            .AddSuccessMessage("Widgets configuration updated.");
    }

    private async Task<ContentItemCommonDataInfo?> GetDraftCommonDataInfo(CancellationToken cancellationToken)
    {
        var webPage = await webPageItemProvider.GetAsync(WebPageIdentifier.WebPageItemID);
        if (webPage is null)
        {
            return null;
        }

        var language = await contentLanguageProvider.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageName), WebPageIdentifier.LanguageName)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        if (language.FirstOrDefault() is not ContentLanguageInfo lang)
        {
            return null;
        }

        var commonDataItems = await contentItemCommonDataProvider.Get()
            .WhereEquals(
                nameof(ContentItemCommonDataInfo.ContentItemCommonDataContentItemID),
                webPage.WebPageItemContentItemID)
            .WhereEquals(
                nameof(ContentItemCommonDataInfo.ContentItemCommonDataContentLanguageID),
                lang.ContentLanguageID)
            .WhereIn(
                nameof(ContentItemCommonDataInfo.ContentItemCommonDataVersionStatus),
                [(int)VersionStatus.Draft, (int)VersionStatus.InitialDraft])
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return commonDataItems.FirstOrDefault();
    }
}

public class WebPageRocketSurgeryTabClientProperties : WebPageBaseClientProperties
{
    public string TemplateConfiguration { get; set; } = "";
    public string WidgetsConfiguration { get; set; } = "";
    public bool IsModifiable { get; set; }
    public bool CanEdit { get; set; }
    public string UpdateTemplateCommandName { get; set; } = "";
    public string UpdateWidgetsCommandName { get; set; } = "";
}

public record UpdateRocketSurgeryTemplateConfigParams(string TemplateConfiguration);
public record UpdateRocketSurgeryWidgetsParams(string WidgetsConfiguration);
