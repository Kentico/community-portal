using CMS;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.DataProtection;
using CMS.Membership;
using Kentico.Community.Portal.Web.Features.DataProtection;

[assembly: RegisterModule(typeof(DataProtectionModule))]
[assembly: RegisterCustomHelper(typeof(JsonPersonalDataHelper))]

namespace Kentico.Community.Portal.Web.Features.DataProtection;

/// <summary>
/// Registers the portal's identity collector, data collector and eraser for the Data protection
/// app (Xperience docs pattern). One of each: they cover members, contacts, forms (found by
/// shape), Q&amp;A, discussions, badges and email statistics.
/// </summary>
internal class DataProtectionModule : Module
{
    public DataProtectionModule() : base(nameof(DataProtectionModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        var scopeFactory = parameters.Services.GetRequiredService<IServiceScopeFactory>();

        IdentityCollectorRegister.Instance.Add(new CommunityIdentityCollector(
            parameters.Services.GetRequiredService<IInfoProvider<MemberInfo>>(),
            parameters.Services.GetRequiredService<IInfoProvider<ContactInfo>>()));
        PersonalDataCollectorRegister.Instance.Add(new CommunityPersonalDataCollector(scopeFactory));
        PersonalDataEraserRegister.Instance.Add(new CommunityPersonalDataEraser(scopeFactory));
    }
}

/// <summary>
/// Email → the member with that email and every contact with it. When no contact has the email, a
/// transient contact (ID 0) carries the email, so form data is still found.
/// </summary>
public class CommunityIdentityCollector(
    IInfoProvider<MemberInfo> memberProvider,
    IInfoProvider<ContactInfo> contactProvider) : IIdentityCollector
{
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;

    public void Collect(IDictionary<string, object> dataSubjectIdentifiersFilter, List<BaseInfo> identities)
    {
        if (!dataSubjectIdentifiersFilter.TryGetValue(PersonalDataConstants.DATA_SUBJECT_IDENTIFIER_KEY, out object? value)
            || value is not string email
            || string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        email = email.Trim();

        identities.AddRange(memberProvider.Get()
            .WhereEquals(nameof(MemberInfo.MemberEmail), email)
            .ToList());

        var contacts = contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), email)
            .ToList();

        identities.AddRange(contacts.Count > 0
            ? contacts
            : [new ContactInfo { ContactEmail = email }]);
    }
}

public class CommunityPersonalDataCollector(IServiceScopeFactory scopeFactory) : IPersonalDataCollector
{
    private readonly IServiceScopeFactory scopeFactory = scopeFactory;

    public PersonalDataCollectorResult Collect(IEnumerable<BaseInfo> identities, string outputFormat)
    {
        var subject = DataSubject.From(identities);
        if (subject.IsEmpty)
        {
            return new PersonalDataCollectorResult();
        }

        using var scope = scopeFactory.CreateScope();
        var report = scope.ServiceProvider.GetRequiredService<CommunityPersonalDataCollectorService>().Collect(subject);

        return new PersonalDataCollectorResult
        {
            Text = report.Sections.Count == 0 ? null : report.Write(outputFormat),
        };
    }
}

/// <summary>
/// Xperience's eraser API is synchronous; the work (content item deletion, index rebuild) is async.
/// </summary>
public class CommunityPersonalDataEraser(IServiceScopeFactory scopeFactory) : IPersonalDataEraser
{
    private readonly IServiceScopeFactory scopeFactory = scopeFactory;

    public void Erase(IEnumerable<BaseInfo> identities, IDictionary<string, object> configuration)
    {
        var subject = DataSubject.From(identities);
        if (subject.IsEmpty)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<CommunityPersonalDataEraserService>()
            .Erase(subject, configuration, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }
}
