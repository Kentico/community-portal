using Kentico.Community.Portal.Admin.Features.MemberBadges;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormComponent(
    identifier: MemberBadgesAssignmentComponent.IDENTIFIER,
    componentType: typeof(MemberBadgesAssignmentComponent),
    name: "Member Badges Assignment")]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

public sealed class MemberBadgesAssignmentComponentAttribute : FormComponentAttribute { }
public class MemberBadgesAssignmentComponentProperties : FormComponentProperties { }
public class MemberBadgesAssignmentComponentClientProperties : FormComponentClientProperties<IEnumerable<MemberBadgeAssignmentModel>> { }

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

