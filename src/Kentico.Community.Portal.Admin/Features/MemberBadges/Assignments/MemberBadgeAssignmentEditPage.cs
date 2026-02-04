using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Membership;

using Kentico.Community.Portal.Admin.Features.MemberBadges;

using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(MemberBadgeAssignSectionPage),
    slug: "edit",
    uiPageType: typeof(MemberBadgeAssignmentEditPage),
    name: "Edit",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder
    )]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
internal class MemberBadgeAssignmentEditPage(IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IMemberBadgeMemberInfoProvider memberBadgeMemberInfoProvider,
    IContentQueryExecutor contentQueryExecutor,
    IMemberBadgeInfoProvider memberBadgeProvider,
    IProgressiveCache cache,
    TimeProvider clock) : ModelEditPage<MemberBadgesAssignmentConfigurationModel>(formItemCollectionProvider, formDataBinder)
{
    private MemberBadgesAssignmentConfigurationModel model = new();

    [PageParameter(typeof(IntPageModelBinder))]
    public int MemberID { get; set; }

    private readonly IMemberBadgeMemberInfoProvider memberBadgeMemberInfoProvider = memberBadgeMemberInfoProvider;
    private readonly IContentQueryExecutor contentQueryExecutor = contentQueryExecutor;
    private readonly IMemberBadgeInfoProvider memberBadgeProvider = memberBadgeProvider;
    private readonly IProgressiveCache cache = cache;
    private readonly TimeProvider clock = clock;

    protected override MemberBadgesAssignmentConfigurationModel Model => model;

    protected override async Task<ICollection<IFormItem>> GetFormItems()
    {
        if (model.MemberId == 0)
        {
            // We initialize the model here because the Model property is a sync getter and initialization is async
            model = await GetConfigurationModel();
        }

        return await base.GetFormItems();
    }

    protected override async Task<ICommandResponse> ProcessFormData(MemberBadgesAssignmentConfigurationModel model, ICollection<IFormItem> formItems)
    {
        var memberBadgeRelationships = await memberBadgeMemberInfoProvider.GetAllMemberBadgeRelationshipsCached();
        var deletedBadgeIds = new List<int>();
        var assignedBadges = new List<MemberBadgeMemberInfo>();

        foreach (var badge in model.ManuallyAssignedBadges)
        {
            if (!badge.IsAssigned && memberBadgeRelationships.HasEntry(MemberID, badge.MemberBadgeID))
            {
                deletedBadgeIds.Add(badge.MemberBadgeID);
            }
            else if (badge.IsAssigned && !memberBadgeRelationships.HasEntry(MemberID, badge.MemberBadgeID))
            {
                bool isAlwaysSelected = PortalMemberBadges.IsAlwaysSelected(badge.MemberBadgeCodeName);

                assignedBadges.Add(new MemberBadgeMemberInfo
                {
                    MemberBadgeMemberMemberBadgeId = badge.MemberBadgeID,
                    MemberBadgeMemberMemberId = MemberID,
                    MemberBadgeMemberIsSelected = isAlwaysSelected,
                    MemberBadgeMemberCreatedDate = clock.GetUtcNow().DateTime
                });
            }
        }

        var removeUnassignedBadgesQuery = memberBadgeMemberInfoProvider
            .Get()
            .WhereIn(nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberBadgeId), deletedBadgeIds)
            .WhereEquals(nameof(MemberBadgeMemberInfo.MemberBadgeMemberMemberId), MemberID);

        memberBadgeMemberInfoProvider.BulkDelete(removeUnassignedBadgesQuery);
        memberBadgeMemberInfoProvider.BulkInsert(assignedBadges);

        // Clear cache since bulk operations bypass normal cache invalidation
        memberBadgeMemberInfoProvider.ClearCache();

        return Response().AddSuccessMessage("Badge assigment updated.");
    }

    private async Task<MemberBadgesAssignmentConfigurationModel> GetConfigurationModel()
    {
        var allBadges = await memberBadgeProvider.GetAllMemberBadgesCached();
        var assignedBadges = await memberBadgeMemberInfoProvider.GetAllMemberBadgesForMemberCached(MemberID);
        var manuallyAssignedBadges = new List<MemberBadgeAssignmentModel>();
        var ruleAssignedBadges = new List<MemberBadgeAssignmentModel>();

        foreach (var badge in allBadges)
        {
            string? badgeImageUrl = await RetrieveMediaAssetUrl(badge);
            bool isAssigned = assignedBadges.Any(x => x.MemberBadgeMemberMemberBadgeId == badge.MemberBadgeID);

            var assignmentModel = new MemberBadgeAssignmentModel(badge, isAssigned, badgeImageUrl, badge.MemberBadgeCodeName);

            if (badge.MemberBadgeIsRuleAssigned)
            {
                ruleAssignedBadges.Add(assignmentModel);
            }
            else
            {
                manuallyAssignedBadges.Add(assignmentModel);
            }
        }

        return new MemberBadgesAssignmentConfigurationModel(MemberID, manuallyAssignedBadges, ruleAssignedBadges);
    }

    public async Task<string?> RetrieveMediaAssetUrl(MemberBadgeInfo badge)
    {
        if (badge.MemberBadgeImageContent.FirstOrDefault() is not ContentItemReference imageRef)
        {
            return null;
        }

        var contentItemGUID = imageRef.Identifier;

        string? url = await cache.Load(async cs =>
        {
            var b = new ContentItemQueryBuilder()
                .ForContentTypes(q => q.OfContentType(ImageContent.CONTENT_TYPE_NAME).WithContentTypeFields())
                .Parameters(q => q.Where(w => w.WhereContentItem(contentItemGUID)));

            var contentItems = await contentQueryExecutor.GetMappedResult<ImageContent>(b, options: new ContentQueryExecutionOptions { ForPreview = true, IncludeSecuredItems = false });

            if (contentItems.FirstOrDefault() is not ImageContent image)
            {
                return null;
            }

            cs.GetCacheDependency = () => CacheHelper.GetCacheDependency($"contentitem|byguid|{contentItemGUID}");

            return image.ImageContentAsset.Url;
        }, new CacheSettings(3, $"{nameof(MemberBadgeAssignmentEditPage)}|{nameof(RetrieveMediaAssetUrl)}|{contentItemGUID}"));

        return url;
    }
}

public class MemberBadgesAssignmentConfigurationModel
{
    public int MemberId { get; set; }

    [MemberBadgesAssignmentComponent(Label = "Assigned Badges")]
    public IEnumerable<MemberBadgeAssignmentModel> ManuallyAssignedBadges { get; set; } = [];

    [MemberBadgesRuleAssignedListComponent(Label = "Rule assigned badges")]
    public IEnumerable<MemberBadgeAssignmentModel> RuleAssignedBadges { get; set; } = [];
    public MemberBadgesAssignmentConfigurationModel() { }
    public MemberBadgesAssignmentConfigurationModel(int memberId, IEnumerable<MemberBadgeAssignmentModel> manuallyAssignedBadges, IEnumerable<MemberBadgeAssignmentModel> ruleAssignedBadges)
    {
        MemberId = memberId;
        ManuallyAssignedBadges = manuallyAssignedBadges;
        RuleAssignedBadges = ruleAssignedBadges;
    }
}
