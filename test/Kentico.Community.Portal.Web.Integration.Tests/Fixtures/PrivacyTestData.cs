using CMS.Base;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.DataProtection;

namespace Kentico.Community.Portal.Web.Tests.Fixtures;

/// <summary>
/// Creates contacts, consents and recipient lists for consent/tracking integration tests, and
/// deletes them all on <see cref="Dispose" />.
/// </summary>
/// <remarks>
/// <para>
/// Integration tests run against the local development database (<c>Tests.config</c>), so every
/// row created here must be removed again. Create one instance per test and dispose it in
/// <c>[TearDown]</c> (or a <c>using</c>).
/// </para>
/// <para>
/// Consents and recipient lists are CI-tracked object types and the local database has continuous
/// integration on, so every write runs with CI serialization off. Without that, a test run would
/// add files to <c>App_Data/CIRepository</c>.
/// </para>
/// </remarks>
public sealed class PrivacyTestData : IDisposable
{
    private readonly string runID = Guid.NewGuid().ToString("N")[..8];
    private int nameCounter;

    private readonly List<ContactInfo> contacts = [];
    private readonly List<ContactGroupInfo> recipientLists = [];
    private readonly List<ConsentInfo> consents = [];

    public ContactBuilder Contact() => new(this);

    public ConsentBuilder Consent() => new(this);

    public RecipientListBuilder RecipientList() => new(this);

    /// <summary>
    /// A unique, valid code name for this run, e.g. <c>IntegrationTest_Consent_1a2b3c4d_1</c>.
    /// </summary>
    public string UniqueName(string kind) => $"IntegrationTest_{kind}_{runID}_{++nameCounter}";

    /// <summary>
    /// An email no other test run uses, on the same non-routable domain the E2E suite uses.
    /// </summary>
    public string UniqueEmail(string kind = "contact") => $"integration-{kind}.{runID}.{++nameCounter}@kentico.local";

    /// <summary>
    /// Suppresses CI serialization (see remarks on the class) and event log noise for test writes.
    /// </summary>
    internal static CMSActionContext WriteContext() => new()
    {
        ContinuousIntegrationAllowObjectSerialization = false,
        LogEvents = false,
    };

    internal ContactInfo Track(ContactInfo contact)
    {
        contacts.Add(contact);
        return contact;
    }

    internal ContactGroupInfo Track(ContactGroupInfo recipientList)
    {
        recipientLists.Add(recipientList);
        return recipientList;
    }

    internal ConsentInfo Track(ConsentInfo consent)
    {
        consents.Add(consent);
        return consent;
    }

    /// <summary>
    /// Deletes in dependency order: contacts take their agreements, list memberships and
    /// subscription confirmations with them, which frees the lists and consents to be deleted.
    /// Rows are re-read by ID so state changed by the code under test is deleted too.
    /// </summary>
    public void Dispose()
    {
        using var context = WriteContext();

        // Deletes fail in read-only mode; a test that forgot to dispose its scope must not leak rows
        var readOnlyOptions = ReadOnlyModeScope.Options;
        bool wasReadOnly = readOnlyOptions.ReadOnly;
        readOnlyOptions.ReadOnly = false;

        try
        {
            DeleteAll(contacts);
            DeleteAll(recipientLists);
            DeleteAll(consents);
        }
        finally
        {
            readOnlyOptions.ReadOnly = wasReadOnly;
        }
    }

    private static void DeleteAll<TInfo>(List<TInfo> items)
        where TInfo : AbstractInfoBase<TInfo>, new()
    {
        var provider = Service.Resolve<IInfoProvider<TInfo>>();

        foreach (var item in Enumerable.Reverse(items))
        {
            var current = provider.Get()
                .WhereEquals(item.TypeInfo.IDColumn, item.Generalized.ObjectID)
                .TopN(1)
                .FirstOrDefault();

            if (current is not null)
            {
                provider.Delete(current);
            }
        }

        items.Clear();
    }
}
