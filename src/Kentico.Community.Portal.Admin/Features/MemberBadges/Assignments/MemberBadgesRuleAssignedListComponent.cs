using Kentico.Community.Portal.Admin.Features.MemberBadges;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormComponent(
    identifier: MemberBadgesRuleAssignedListComponent.IDENTIFIER,
    componentType: typeof(MemberBadgesRuleAssignedListComponent),
    name: "Member Badges Assignment")]

namespace Kentico.Community.Portal.Admin.Features.MemberBadges;

public sealed class MemberBadgesRuleAssignedListComponentAttribute : FormComponentAttribute { }
public class MemberBadgesRuleAssignedListComponentProperties : FormComponentProperties { }
public class MemberBadgesRuleAssignedListComponentClientProperties : FormComponentClientProperties<IEnumerable<MemberBadgeAssignmentModel>> { }

[ComponentAttribute(typeof(MemberBadgesRuleAssignedListComponentAttribute))]
public class MemberBadgesRuleAssignedListComponent : FormComponent<MemberBadgesRuleAssignedListComponentProperties, MemberBadgesRuleAssignedListComponentClientProperties, IEnumerable<MemberBadgeAssignmentModel>>
{
    public const string IDENTIFIER = "kentico-community.portal-web-admin.member-badges-rule-assigned-list";

    internal List<MemberBadgeAssignmentModel>? Value { get; set; }

    public override string ClientComponentName => "@kentico-community/portal-web-admin/MemberBadgesRuleAssignedList";

    public override IEnumerable<MemberBadgeAssignmentModel> GetValue() => Value ?? [];
    public override void SetValue(IEnumerable<MemberBadgeAssignmentModel> value) => Value = value.ToList();

    protected override async Task ConfigureClientProperties(MemberBadgesRuleAssignedListComponentClientProperties properties)
    {
        properties.Value = Value ?? [];

        await base.ConfigureClientProperties(properties);
    }
}
