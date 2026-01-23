using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kentico.Community.Portal.Web.Components.TagHelpers;

/// <summary>
/// Format options for datetime display in the DateTimeTagHelper
/// </summary>
public enum DateTimeFormat
{
    /// <summary>
    /// Display both date and time (e.g., "2024/01/22 2:30 PM")
    /// </summary>
    DateTime,

    /// <summary>
    /// Display date only (e.g., "2024/01/22")
    /// </summary>
    Date
}

[HtmlTargetElement("time", Attributes = ATTRIBUTE_NAME)]
public class DateTimeTagHelper(
    UserManager<CommunityMember> userManager,
    DateTimeDisplayService dateTimeDisplayService,
    IHttpContextAccessor httpContextAccessor,
    ViewService viewService) : TagHelper
{
    public const string ATTRIBUTE_NAME = "xpc-datetime";
    private const string FORMAT_ATTRIBUTE_NAME = "xpc-datetime-format";

    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly DateTimeDisplayService dateTimeDisplayService = dateTimeDisplayService;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly ViewService viewService = viewService;

    [HtmlAttributeName(ATTRIBUTE_NAME)]
    public DateTime? DisplayDateTime { get; set; }

    [HtmlAttributeName(FORMAT_ATTRIBUTE_NAME)]
    public DateTimeFormat Format { get; set; } = DateTimeFormat.DateTime;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (DisplayDateTime is null)
        {
            await base.ProcessAsync(context, output);
            return;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        var currentMember = await userManager.CurrentUser(httpContext);
        var (convertedDate, abbreviation, _) = dateTimeDisplayService.ConvertToUserTimezone(DisplayDateTime.Value, currentMember?.TimeZone);

        // Remove the custom attributes from output
        output.Attributes.RemoveAll(ATTRIBUTE_NAME);
        output.Attributes.RemoveAll(FORMAT_ATTRIBUTE_NAME);

        // datetime attribute: UTC time (machine-readable)
        output.Attributes.SetAttribute("datetime", DisplayDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        // Merge classes instead of replacing
        string existingClass = output.Attributes.FirstOrDefault(a => a.Name == "class")?.Value.ToString() ?? "";
        string mergedClass = string.IsNullOrWhiteSpace(existingClass)
            ? "text-muted small"
            : $"{existingClass} text-muted small";
        output.Attributes.SetAttribute("class", mergedClass);

        // Merge styles instead of replacing
        string existingStyle = output.Attributes.FirstOrDefault(a => a.Name == "style")?.Value.ToString() ?? "";
        string newStyle = "background-color: rgba(0, 0, 0, 0.03); padding: 0.125rem 0.25rem; border-radius: 0.25rem;";
        string mergedStyle = string.IsNullOrWhiteSpace(existingStyle)
            ? newStyle
            : $"{existingStyle} {newStyle}";
        output.Attributes.SetAttribute("style", mergedStyle);

        // Content: Localized time with timezone abbreviation (human-readable)
        var culture = viewService.Culture;
        string dateTime = Format == DateTimeFormat.Date
            ? convertedDate.ToString("d", culture)
            : $"{convertedDate.ToString("d", culture)} {convertedDate.ToString("t", culture)}";

        string formattedDateTime = Format == DateTimeFormat.Date
            ? dateTime
            : $"{dateTime} {abbreviation}";
        output.Content.SetHtmlContent(formattedDateTime);
    }
}
