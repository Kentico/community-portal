using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;

namespace Kentico.Community.Portal.Web.Tests.Fixtures;

/// <summary>
/// Builds a throwaway recipient list. Subscribe contacts with <see cref="ContactBuilder.SubscribedTo" />.
/// </summary>
public class RecipientListBuilder(PrivacyTestData data)
{
    private readonly PrivacyTestData data = data;

    private string? codeName;

    public RecipientListBuilder Named(string listCodeName)
    {
        codeName = listCodeName;
        return this;
    }

    public ContactGroupInfo Build()
    {
        string name = codeName ?? data.UniqueName("RecipientList");

        var list = new ContactGroupInfo
        {
            ContactGroupName = name,
            ContactGroupDisplayName = name,
            ContactGroupIsRecipientList = true,
            ContactGroupEnabled = true,
        };

        using (PrivacyTestData.WriteContext())
        {
            Service.Resolve<IInfoProvider<ContactGroupInfo>>().Set(list);
        }

        return data.Track(list);
    }
}
