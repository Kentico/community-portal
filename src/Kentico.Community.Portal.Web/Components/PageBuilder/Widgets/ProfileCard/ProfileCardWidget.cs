using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.ProfileCard;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: ProfileCardWidget.IDENTIFIER,
    viewComponentType: typeof(ProfileCardWidget),
    name: ProfileCardWidget.NAME,
    propertiesType: typeof(ProfileCardWidgetProperties),
    Description = "Card featuring a community member's profile",
    IconClass = KenticoIcons.ID_CARD,
    AllowCache = true
)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.ProfileCard;

public class ProfileCardWidget(IMediator mediator, MarkdownRenderer markdownRenderer) : ViewComponent
{
    public const string NAME = "Profile card";
    public const string IDENTIFIER = "Kentico.Community.Portal.Web.Widgets.ProfileCard";

    private readonly IMediator mediator = mediator;
    private readonly MarkdownRenderer markdownRenderer = markdownRenderer;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<ProfileCardWidgetProperties> vm)
    {
        var props = vm.Properties;
        var resp = await mediator.Send(new MemberProfileContentsQuery(props.ProfileReferences.Select(p => p.Identifier).FirstOrDefault()));

        return Validate(resp, vm.Properties)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/ProfileCard/ProfileCard.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm)
            );
    }

    private Result<ProfileCardWidgetViewModel, ComponentErrorViewModel> Validate(Maybe<MemberProfileContentsQueryResponse> respM, ProfileCardWidgetProperties props)
    {
        if (!props.ProfileReferences.Any() || !respM.TryGetValue(out var resp))
        {
            return Result.Failure<ProfileCardWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "Select at least 1 valid member profile."));
        }

        if (!resp.Profile.MemberProfileContentPhotoImageContent.Any())
        {
            return Result.Failure<ProfileCardWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "One of your selected profiles does not have a photo."));
        }

        return new ProfileCardWidgetViewModel(resp, props, markdownRenderer);
    }
}

public class ProfileCardWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(MemberProfileContent.CONTENT_TYPE_NAME,
        Label = "Profiles",
        ExplanationText = "The member profiles to feature",
        MinimumItems = 1,
        MaximumItems = 1,
        Order = 1
    )]
    public IEnumerable<ContentItemReference> ProfileReferences { get; set; } = [];

    [MarkdownComponent(
        Label = "Profile",
        ExplanationText = "If provided, this markdown replaces the default content from the Member's profile.",
        Order = 2)]
    [TrackContentItemReference(typeof(MarkdownContentItemReferenceExtractor))]
    public string ProfileMarkdown { get; set; } = "";
}

public class ProfileCardWidgetViewModel : BaseWidgetViewModel
{
    public ProfileCardWidgetViewModel(MemberProfileContentsQueryResponse resp, ProfileCardWidgetProperties props, MarkdownRenderer markdownRenderer)
    {
        var (content, member) = resp;

        DisplayName = Maybe.From(content.MemberProfileContentFullName)
            .MapNullOrWhiteSpaceAsNone()
            .Or(member.Map(m => m.DisplayName))
            .GetValueOrDefault("");
        ShortName = member
            .Map(m => m.FirstName)
            .GetValueOrDefault(content.MemberProfileContentFullName);
        Photo = content
            .MemberProfileContentPhotoImageContent
            .TryFirst()
            .Map(ImageViewModel.Create);
        Member = member;

        ProfileHTML = Maybe.From(props.ProfileMarkdown)
            .MapNullOrWhiteSpaceAsNone()
            .Map(markdownRenderer.RenderUnsafe);
    }

    public string DisplayName { get; }
    public string ShortName { get; }
    public Maybe<ImageViewModel> Photo { get; }
    public Maybe<CommunityMember> Member { get; }
    public Maybe<HtmlString> ProfileHTML { get; }

    protected override string WidgetName => ProfileCardWidget.NAME;
}

public record MemberProfileContentsQuery(Guid ContentItemGUID) : IQuery<Maybe<MemberProfileContentsQueryResponse>>, ICacheByValueQuery
{
    public string CacheValueKey => ContentItemGUID.ToString();
}

public record MemberProfileContentsQueryResponse(MemberProfileContent Profile, Maybe<CommunityMember> Member);
public class MemberProfileContentQueryHandler(ContentItemQueryTools tools, IInfoProvider<MemberInfo> memberProvider) : ContentItemQueryHandler<MemberProfileContentsQuery, Maybe<MemberProfileContentsQueryResponse>>(tools)
{
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;

    public override async Task<Maybe<MemberProfileContentsQueryResponse>> Handle(MemberProfileContentsQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(q => q.OfContentType(MemberProfileContent.CONTENT_TYPE_NAME).WithContentTypeFields().WithLinkedItems(1))
            .Parameters(q => q.Where(w => w.WhereContentItem(request.ContentItemGUID)));

        var contents = await Executor.GetMappedResult<MemberProfileContent>(b, DefaultQueryOptions, cancellationToken);

        if (contents.FirstOrDefault() is not MemberProfileContent content)
        {
            return Maybe<MemberProfileContentsQueryResponse>.None;
        }

        var members = await memberProvider.Get()
            .WhereEquals(nameof(MemberInfo.MemberID), contents.Select(c => c.MemberProfileContentMemberID).FirstOrDefault())
            .GetEnumerableTypedResultAsync();

        if (members.FirstOrDefault() is not MemberInfo memberInfo)
        {
            return Maybe<MemberProfileContentsQueryResponse>.None;
        }

        return new MemberProfileContentsQueryResponse(content, memberInfo.AsCommunityMember());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(MemberProfileContentsQuery query, Maybe<MemberProfileContentsQueryResponse> result, ICacheDependencyKeysBuilder builder)
    {
        result.Execute(r => builder
            .ContentItem(r.Profile.SystemFields.ContentItemID)
            .Object(MemberInfo.OBJECT_TYPE, r.Profile.MemberProfileContentMemberID));

        return builder;
    }
}
