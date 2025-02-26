using System.ComponentModel;
using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.ViewComponents.PollFormResults;
using Kentico.Community.Portal.Web.Components.Widgets.Forms;
using Kentico.Community.Portal.Web.Components.Widgets.PollForm;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Forms.Web.Mvc.Widgets;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: PollWidget.IDENTIFIER,
    name: PollWidget.NAME,
    viewComponentType: typeof(PollWidget),
    propertiesType: typeof(PollWidgetProperties),
    Description = "Adds a poll and its associated form to the page, based on a Poll Content item.",
    IconClass = KenticoIcons.FORM)]

namespace Kentico.Community.Portal.Web.Components.Widgets.PollForm;

public class PollWidget(
    IFormMemberEngagementRetriever engagementRetriever,
    MarkdownRenderer renderer,
    IMediator mediator,
    TimeProvider time) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.PollWidget";
    public const string NAME = "Poll";
    private readonly IFormMemberEngagementRetriever engagementRetriever = engagementRetriever;
    private readonly MarkdownRenderer renderer = renderer;
    private readonly IMediator mediator = mediator;
    private readonly TimeProvider time = time;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<PollWidgetProperties> vm)
    {
        var props = vm.Properties;
        var poll = await mediator.Send(new PollContentByGUIDQuery(props.PollContents.Select(p => p.Identifier).FirstOrDefault()));

        return await Validate(props, poll)
            .Match(
                model => View("~/Components/Widgets/Forms/Poll.cshtml", model),
                model => View("~/Components/ComponentError.cshtml", model)
            );
    }

    private async Task<Result<PollWidgetViewModel, ComponentErrorViewModel>> Validate(PollWidgetProperties props, Maybe<PollContent> pollContent)
    {
        if (props.PollContents.FirstOrDefault() is null)
        {
            return Result.Failure<PollWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "No poll has been selected."));
        }

        if (!pollContent.TryGetValue(out var poll))
        {
            return Result.Failure<PollWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected content item no longer exists."));
        }

        if (!poll.PollContentForm.TryFirst().MapNullOrWhiteSpaceAsNone().TryGetValue(out string? formName))
        {
            return Result.Failure<PollWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "The selected poll has no associated form."));
        }

        bool isAuthenticated = HttpContext.User.Identity?.IsAuthenticated ?? false;
        bool hasSubmitted = await engagementRetriever.HasCurrentMemberSubmittedForm(formName);

        return new PollWidgetViewModel(poll, formName, hasSubmitted, props, isAuthenticated, renderer, time);
    }
}

public class PollWidgetProperties : IWidgetProperties
{
    [ContentItemSelectorComponent(PollContent.CONTENT_TYPE_NAME,
        Label = "Poll",
        Order = 1,
        ExplanationText = "The Poll Content and associated form to populate the widget.")]
    public IEnumerable<ContentItemReference> PollContents { get; set; } = [];

    [DropDownComponent(
        Label = "Background color",
        ExplanationText = "Determines the background color of the entire poll",
        Tooltip = "Select a color",
        DataProviderType = typeof(EnumDropDownOptionsProvider<BackgroundColors>),
        Order = 3
    )]
    public string BackgroundColor { get; set; } = nameof(BackgroundColors.White);
    public BackgroundColors BackgroundColorParsed => EnumDropDownOptionsProvider<BackgroundColors>.Parse(BackgroundColor, BackgroundColors.White);
}

public enum BackgroundColors
{
    [Description("Light")]
    Light,
    [Description("White")]
    White,
    [Description("Dark")]
    Dark,
    [Description("Secondary Light")]
    Secondary_Light
}

public class PollWidgetViewModel : IHideableComponent
{

    public bool IsHidden { get; }
    public string FormName { get; }
    public Maybe<HtmlString> Description { get; }
    public DateTime PublishedDate { get; }
    public DateTime OpenUntilDate { get; }
    public bool HasVisitorAnsweredPoll { get; }
    public bool IsPollActive { get; }
    public BackgroundColors BackgroundColor { get; }

    public PollWidgetViewModel(PollContent poll, string formName, bool hasSubmitted, PollWidgetProperties props, bool isAuthenticated, MarkdownRenderer renderer, TimeProvider time)
    {
        IsHidden = !isAuthenticated;
        PublishedDate = poll.PollContentPublishedDate;
        OpenUntilDate = poll.PollContentOpenUntilDate;
        Description = Maybe.From(poll.PollContentDescriptionMarkdown)
            .MapNullOrWhiteSpaceAsNone()
            .Map(renderer.RenderUnsafe);
        HasVisitorAnsweredPoll = hasSubmitted;
        IsPollActive = time.GetLocalNow() <= poll.PollContentOpenUntilDate;
        FormName = formName;
        BackgroundColor = props.BackgroundColorParsed;
    }

    public FormWidgetProperties ToFormWidgetProperties() =>
        new()
        {
            AfterSubmitDisplayText = "Thanks for your answer. Reload the page to see the results.",
            SelectedForm = [new() { ObjectCodeName = FormName }],
        };

    public PollResultsProperties ToPollResultsProperties() => new()
    {
        FormName = FormName,
        IsPollActive = IsPollActive,
        OpenUntilDate = OpenUntilDate,
        Description = Description,
    };
}



public record PollContentByGUIDQuery(Guid ContentItemGUID) : IQuery<Maybe<PollContent>>, ICacheByValueQuery
{
    public string CacheValueKey => ContentItemGUID.ToString();
}

public class PollContentByGUIDQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<PollContentByGUIDQuery, Maybe<PollContent>>(tools)
{
    public override async Task<Maybe<PollContent>> Handle(PollContentByGUIDQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentType(
                PollContent.CONTENT_TYPE_NAME,
                q => q.ForContentItem(request.ContentItemGUID));

        return await Executor
            .GetMappedResult<PollContent>(b, DefaultQueryOptions, cancellationToken)
            .TryFirst();
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(PollContentByGUIDQuery query, Maybe<PollContent> result, ICacheDependencyKeysBuilder builder) =>
        builder.ContentItem(query.ContentItemGUID);
}
