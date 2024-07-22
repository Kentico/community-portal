using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Membership;

using Kentico.Community.Portal.Admin.Features.MemberBadges;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(MemberBadgeMemberListingPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(MemberBadgeAssignBadgePage),
    name: "Edit Member Badges Assignment",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder
    )]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
internal class MemberBadgeAssignBadgePage(IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IPageUrlGenerator pageUrlGenerator,
    IMemberBadgeMemberInfoProvider memberBadgeMemberInfoProvider,
    IContentQueryExecutor contentQueryExecutor,
    IMemberBadgeInfoProvider memberBadgeProvider,
    IProgressiveCache cache,
    ISystemClock clock) : ModelEditPage<MemberBadgesAssignmentConfigurationModel>(formItemCollectionProvider, formDataBinder)
{
    private MemberBadgesAssignmentConfigurationModel model = new();

    [PageParameter(typeof(IntPageModelBinder))]
    public int MemberID { get; set; }

    private readonly IPageUrlGenerator pageUrlGenerator = pageUrlGenerator;
    private readonly IMemberBadgeMemberInfoProvider memberBadgeMemberInfoProvider = memberBadgeMemberInfoProvider;
    private readonly IContentQueryExecutor contentQueryExecutor = contentQueryExecutor;
    private readonly IMemberBadgeInfoProvider memberBadgeProvider = memberBadgeProvider;
    private readonly IProgressiveCache cache = cache;
    private readonly ISystemClock clock = clock;

    protected override MemberBadgesAssignmentConfigurationModel Model => model;

    protected override async Task<ICollection<IFormItem>> GetFormItems()
    {
        if (model.MemberId == 0)
        {
            // We initialize the model here because retrieving the Model is a synch getter and initialization is async
            model = await GetConfigurationModel();
        }

        return await base.GetFormItems();
    }

    protected override async Task<ICommandResponse> ProcessFormData(MemberBadgesAssignmentConfigurationModel model, ICollection<IFormItem> formItems)
    {
        var memberBadgeRelationships = await memberBadgeMemberInfoProvider.GetAllMemberBadgeRelationshipsCached();
        var deletedBadgeIds = new List<int>();
        var assignedBadges = new List<MemberBadgeMemberInfo>();

        foreach (var badge in model.Badges)
        {
            if (!badge.IsAssigned && memberBadgeRelationships.HasEntry(MemberID, badge.MemberBadgeID))
            {
                deletedBadgeIds.Add(badge.MemberBadgeID);
            }
            else if (badge.IsAssigned && !memberBadgeRelationships.HasEntry(MemberID, badge.MemberBadgeID))
            {
                assignedBadges.Add(new MemberBadgeMemberInfo
                {
                    MemberBadgeMemberMemberBadgeId = badge.MemberBadgeID,
                    MemberBadgeMemberMemberId = MemberID,
                    MemberBadgeMemberIsSelected = false,
                    MemberBadgeMemberCreatedDate = clock.UtcNow
                });
            }
        }

        var removeUnassignedBadgesQuery = memberBadgeMemberInfoProvider
            .Get()
            .WhereIn(nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberBadgeId), deletedBadgeIds);

        memberBadgeMemberInfoProvider.BulkDelete(removeUnassignedBadgesQuery);
        memberBadgeMemberInfoProvider.BulkInsert(assignedBadges);

        var successResponse = NavigateTo(pageUrlGenerator
            .GenerateUrl<MemberBadgeMemberListingPage>())
            .AddSuccessMessage("Badge assigment updated.");

        return await Task.FromResult<ICommandResponse>(successResponse);
    }

    private async Task<MemberBadgesAssignmentConfigurationModel> GetConfigurationModel()
    {
        var allBadges = await memberBadgeProvider.GetAllMemberBadgesCached();
        var assignedBadges = await memberBadgeMemberInfoProvider.GetAllMemberBadgesForMemberCached(MemberID);
        var badges = new List<MemberBadgeAssignmentModel>();

        foreach (var badge in allBadges.Where(b => !b.MemberBadgeIsRuleAssigned))
        {
            if (badge.MemberBadgeMediaAssetContentItem.FirstOrDefault() is not ContentItemReference reference)
            {
                continue;
            }

            string? badgeImageUrl = await RetrieveMediaAssetUrl(reference.Identifier);
            bool isAssigned = assignedBadges.Any(x => x.MemberBadgeMemberMemberBadgeId == badge.MemberBadgeID);

            var assignmentModel = new MemberBadgeAssignmentModel(badge, isAssigned, badgeImageUrl);

            badges.Add(assignmentModel);
        }

        return new MemberBadgesAssignmentConfigurationModel(MemberID, badges);
    }

    public async Task<string?> RetrieveMediaAssetUrl(Guid contentItemGUID)
    {
        string? url = await cache.Load(async cs =>
        {
            var b = new ContentItemQueryBuilder()
                .ForContentTypes(q => q.OfContentType(MediaAssetContent.CONTENT_TYPE_NAME).WithContentTypeFields())
                .Parameters(q => q.Where(w => w.WhereEquals(nameof(MediaAssetContent.SystemFields.ContentItemGUID), contentItemGUID)));

            var contentItems = await contentQueryExecutor.GetMappedResult<MediaAssetContent>(b, options: new ContentQueryExecutionOptions { ForPreview = true, IncludeSecuredItems = false });

            if (contentItems.FirstOrDefault() is not MediaAssetContent media)
            {
                return null;
            }

            cs.GetCacheDependency = () => CacheHelper.GetCacheDependency($"contentitem|byguid|{contentItemGUID}");

            return media.MediaAssetContentAssetLight.Url;
        }, new CacheSettings(3, $"{nameof(MemberBadgeAssignBadgePage)}|{nameof(RetrieveMediaAssetUrl)}|{contentItemGUID}"));

        return url;
    }
}

public class MemberBadgesAssignmentConfigurationModel
{
    public int MemberId { get; set; }

    [MemberBadgesAssignmentComponent(Label = "Assigned Badges")]
    public IEnumerable<MemberBadgeAssignmentModel> Badges { get; set; } = [];
    public MemberBadgesAssignmentConfigurationModel() { }
    public MemberBadgesAssignmentConfigurationModel(int memberId, IEnumerable<MemberBadgeAssignmentModel> badges)
    {
        MemberId = memberId;
        Badges = badges;
    }
}
