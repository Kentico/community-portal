using Kentico.Community.Portal.Admin.Features.MemberBadges;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormComponent(
    identifier: MemberBadgesAssignmentComponent.IDENTIFIER,
    componentType: typeof(MemberBadgesAssignmentComponent),
    name: "Member Badges Assignment")]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

public class MemberBadgesAssignmentComponentProperties : FormComponentProperties
{

}

public class MemberBadgesAssignmentComponentClientProperties : FormComponentClientProperties<IEnumerable<MemberBadgeAssignmentModel>>
{

}

public sealed class MemberBadgesAssignmentComponentAttribute : FormComponentAttribute
{

}

[ComponentAttribute(typeof(MemberBadgesAssignmentComponentAttribute))]
public class MemberBadgesAssignmentComponent : FormComponent<MemberBadgesAssignmentComponentProperties, MemberBadgesAssignmentComponentClientProperties, IEnumerable<MemberBadgeAssignmentModel>>
{
    public const string IDENTIFIER = "kentico-community.portal-web-admin.member-badges-assignment";

    internal List<MemberBadgeAssignmentModel>? Value { get; set; }

    public override string ClientComponentName => "@kentico-community/portal-web-admin/MemberBadgesAssignment";

    public override IEnumerable<MemberBadgeAssignmentModel> GetValue() => Value ?? [];
    public override void SetValue(IEnumerable<MemberBadgeAssignmentModel> value) => Value = value.ToList();

    protected override async Task ConfigureClientProperties(MemberBadgesAssignmentComponentClientProperties properties)
    {
        properties.Value = Value ?? [];

        await base.ConfigureClientProperties(properties);
    }
}

public class MemberBadgeAssignmentModel
{
    public int MemberBadgeID { get; set; } = 0;
    public string MemberBadgeDescription { get; set; } = string.Empty;
    public string MemberBadgeDisplayName { get; set; } = string.Empty;
    public string? BadgeImageRelativePath { get; set; }
    public bool IsAssigned { get; set; }
    public MemberBadgeAssignmentModel() { }
    public MemberBadgeAssignmentModel(MemberBadgeInfo badge, bool isAssigned, string? badgeImageRelativePath)
    {
        MemberBadgeID = badge.MemberBadgeID;
        MemberBadgeDescription = badge.MemberBadgeShortDescription;
        MemberBadgeDisplayName = badge.MemberBadgeDisplayName;
        IsAssigned = isAssigned;
        BadgeImageRelativePath = badgeImageRelativePath?.TrimStart('~');
    }
}
