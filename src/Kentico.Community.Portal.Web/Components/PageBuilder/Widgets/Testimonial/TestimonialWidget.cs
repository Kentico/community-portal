using System.ComponentModel;
using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Testimonial;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: TestimonialWidget.IDENTIFIER,
    viewComponentType: typeof(TestimonialWidget),
    name: TestimonialWidget.NAME,
    propertiesType: typeof(TestimonialWidgetProperties),
    Description = TestimonialWidget.DESCRIPTION,
    IconClass = KenticoIcons.PARAGRAPH,
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Testimonial;

public class TestimonialWidget(IContentRetriever contentRetriever, IMediator mediator) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Components.Widgets.Testimonial";
    public const string NAME = "Testimonial";
    public const string DESCRIPTION = "Displays one testimonial in either Featured or Simple layout.";

    private const string FEATURED_VIEW_PATH = "~/Components/PageBuilder/Widgets/Testimonial/Testimonial_Featured.cshtml";
    private const string SIMPLE_VIEW_PATH = "~/Components/PageBuilder/Widgets/Testimonial/Testimonial_Simple.cshtml";
    private const string ERROR_NO_SELECTION = "Select one testimonial item.";
    private const string ERROR_MISSING_CONTENT = "The selected testimonial no longer exists.";
    private const string ERROR_MISSING_MEMBER = "The selected testimonial member no longer exists.";

    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<TestimonialWidgetProperties> cvm)
    {
        var props = cvm.Properties;
        var selectedTestimonial = props.Testimonial.FirstOrDefault();

        if (selectedTestimonial is null)
        {
            return View("~/Components/ComponentError.cshtml", new ComponentErrorViewModel(NAME, ComponentType.Widget, ERROR_NO_SELECTION));
        }

        var testimonial = (await contentRetriever.RetrieveContentByGuids<TestimonialContent>(
            [selectedTestimonial.Identifier],
            new RetrieveContentParameters
            {
                LinkedItemsMaxLevel = 1,
            }))
            .FirstOrDefault();

        var member = await GetMember(testimonial);

        return Validate(props, testimonial, member)
            .Match(
                vm => View(GetViewPath(vm.Layout), vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private async Task<Maybe<CommunityMember>> GetMember(TestimonialContent? testimonial)
    {
        if (testimonial is null ||
            testimonial.TestimonialContentDataSourceParsed != TestimonialContentDataSources.Member ||
            testimonial.TestimonialContentMemberID <= 0)
        {
            return Maybe<CommunityMember>.None;
        }

        var member = await mediator.Send(new MemberByIDQuery(testimonial.TestimonialContentMemberID));

        return member is null
            ? Maybe<CommunityMember>.None
            : member.AsCommunityMember();
    }

    private static string GetViewPath(TestimonialLayouts layout) => layout switch
    {
        TestimonialLayouts.Simple => SIMPLE_VIEW_PATH,
        TestimonialLayouts.Featured or _ => FEATURED_VIEW_PATH,
    };

    private static Result<TestimonialWidgetViewModel, ComponentErrorViewModel> Validate(
        TestimonialWidgetProperties props,
        TestimonialContent? testimonial,
        Maybe<CommunityMember> member)
    {
        if (testimonial is null)
        {
            return Result.Failure<TestimonialWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, ERROR_MISSING_CONTENT));
        }

        if (testimonial.TestimonialContentDataSourceParsed == TestimonialContentDataSources.Member &&
            !member.TryGetValue(out _))
        {
            return Result.Failure<TestimonialWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, ERROR_MISSING_MEMBER));
        }

        return new TestimonialWidgetViewModel(props, testimonial, member);
    }
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 3)]
public class TestimonialWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        contentTypeName: TestimonialContent.CONTENT_TYPE_NAME,
        Label = "Testimonial",
        ExplanationText = "Select one testimonial content item to display.",
        MinimumItems = 1,
        MaximumItems = 1,
        Order = 1)]
    public IEnumerable<ContentItemReference> Testimonial { get; set; } = [];

    [DropDownComponent(
        Label = "Layout",
        ExplanationText = "Select testimonial layout.",
        DataProviderType = typeof(EnumDropDownOptionsProvider<TestimonialLayouts>),
        Order = 2)]
    public string Layout { get; set; } = nameof(TestimonialLayouts.Featured);
    public TestimonialLayouts LayoutParsed => EnumDropDownOptionsProvider<TestimonialLayouts>.Parse(Layout, TestimonialLayouts.Featured);

    [DropDownComponent(
        Label = "Theme",
        ExplanationText = "Select testimonial theme.",
        DataProviderType = typeof(EnumDropDownOptionsProvider<TestimonialThemes>),
        Order = 3)]
    public string Theme { get; set; } = nameof(TestimonialThemes.Neutral);
    public TestimonialThemes ThemeParsed => EnumDropDownOptionsProvider<TestimonialThemes>.Parse(Theme, TestimonialThemes.Neutral);

    [CheckBoxComponent(
        Label = "Show title",
        ExplanationText = "Show testimonial title when available.",
        Order = 4)]
    public bool ShowTitle { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show photo",
        ExplanationText = "Show testimonial photo when available.",
        Order = 5)]
    public bool ShowPhoto { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show employment",
        ExplanationText = "Show job title and employer when available.",
        Order = 6)]
    public bool ShowEmployment { get; set; } = true;
}

public class TestimonialWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => TestimonialWidget.NAME;

    public TestimonialLayouts Layout { get; }
    public TestimonialThemes Theme { get; }
    public bool ShowTitle { get; }
    public bool ShowPhoto { get; }
    public bool ShowEmployment { get; }
    public string Title { get; }
    public string Message { get; }
    public string Name { get; }
    public string MemberProfileUrl { get; }
    public string JobTitle { get; }
    public string Employer { get; }
    public string Employment { get; }
    public bool HasTitle { get; }
    public bool HasMessage { get; }
    public bool HasName { get; }
    public bool ShouldLinkName { get; }
    public bool HasPhoto { get; }
    public bool UseMemberAvatar { get; }
    public int MemberID { get; }
    public bool ShouldShowTitle { get; }
    public bool ShouldShowName { get; }
    public bool ShouldShowEmployment { get; }
    public bool ShouldShowAttribution { get; }
    public Maybe<ImageAssetViewModel> Photo { get; }

    public TestimonialWidgetViewModel(TestimonialWidgetProperties props, TestimonialContent content, Maybe<CommunityMember> member)
    {
        Layout = props.LayoutParsed;
        Theme = props.ThemeParsed;
        ShowTitle = props.ShowTitle;
        ShowPhoto = props.ShowPhoto;
        ShowEmployment = props.ShowEmployment;
        Title = content.TestimonialContentTitle ?? string.Empty;
        Message = content.TestimonialContentMessage ?? string.Empty;

        if (content.TestimonialContentDataSourceParsed == TestimonialContentDataSources.Member &&
            member.TryGetValue(out var communityMember))
        {
            Name = communityMember.DisplayName;
            JobTitle = communityMember.JobTitle;
            Employer = communityMember.EmployerLink.Label;
            MemberID = communityMember.Id;
            MemberProfileUrl = $"/member/{MemberID}";
            UseMemberAvatar = MemberID > 0;
            Photo = Maybe<ImageAssetViewModel>.None;
        }
        else
        {
            Name = content.TestimonialContentName ?? string.Empty;
            JobTitle = content.TestimonialContentJobTitle ?? string.Empty;
            Employer = content.TestimonialContentEmployer ?? string.Empty;
            MemberID = 0;
            MemberProfileUrl = string.Empty;
            UseMemberAvatar = false;
            Photo = (content.TestimonialContentPhoto ?? [])
                .TryFirst()
                .Map(ImageAssetViewModel.Create);
        }

        HasTitle = !string.IsNullOrWhiteSpace(Title);
        HasMessage = !string.IsNullOrWhiteSpace(Message);
        HasName = !string.IsNullOrWhiteSpace(Name);
        ShouldLinkName = !string.IsNullOrWhiteSpace(MemberProfileUrl);

        Employment = string.Join(", ", new[] { JobTitle, Employer }.Where(value => !string.IsNullOrWhiteSpace(value)));
        ShouldShowTitle = ShowTitle && HasTitle;
        ShouldShowName = HasName;
        ShouldShowEmployment = ShowEmployment && !string.IsNullOrWhiteSpace(Employment);

        HasPhoto = UseMemberAvatar || Photo.TryGetValue(out _);
        ShouldShowAttribution = (ShowPhoto && HasPhoto) || ShouldShowName || ShouldShowEmployment;
    }
}

public enum TestimonialLayouts
{
    [Description("Featured")]
    Featured,
    [Description("Simple")]
    Simple,
}

public enum TestimonialThemes
{
    [Description("Primary")]
    Primary,
    [Description("Secondary")]
    Secondary,
    [Description("Neutral")]
    Neutral,
}
