using CMS.ContactManagement;
using Kentico.PageBuilder.Web.Mvc.Personalization;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterPersonalizationConditionType(
    identifier: IsInContactGroupConditionType.IDENTIFIER,
    type: typeof(IsInContactGroupConditionType),
    name: "Is in contact group",
    Description = "Evaluates if the current contact is in one of the contact groups.",
    IconClass = "icon-app-contact-groups",
    Hint = "Display to visitors who match at least one of the selected contact groups:")]

namespace Kentico.PageBuilder.Web.Mvc.Personalization;

/// <summary>
/// Personalization condition type based on contact group.
/// </summary>
public class IsInContactGroupConditionType : ConditionType
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Personalization.IsInContactGroup";

    /// <summary>
    /// Selected contact group code names.
    /// </summary>
    [ObjectSelectorComponent(
        ContactGroupInfo.OBJECT_TYPE,
        Label = "Contact groups",
        Order = 0,
        MaximumItems = 0)]
    public IEnumerable<ObjectRelatedItem> SelectedContactGroups { get; set; } = [];


    /// <summary>
    /// Evaluate condition type.
    /// </summary>
    /// <returns>Returns <c>true</c> if implemented condition is met.</returns>
    public override bool Evaluate()
    {
        var contact = ContactManagementContext.GetCurrentContact();
        if (contact == null)
        {
            return false;
        }

        if (!SelectedContactGroups.Any())
        {
            return contact.ContactGroups.Count == 0;
        }

        return contact.IsInAnyContactGroup(SelectedContactGroups.Select(c => c.ObjectCodeName).ToArray());
    }
}
