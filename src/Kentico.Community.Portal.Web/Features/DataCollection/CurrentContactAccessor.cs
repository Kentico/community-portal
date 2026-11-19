using CMS.ContactManagement;

namespace Kentico.Community.Portal.Web.Features.DataCollection;

/// <summary>
/// Injectable wrapper for <see cref="ContactManagementContext.GetCurrentContact(bool)"/>, which is
/// static. Lets cookie consent code be tested without Xperience's request context.
/// </summary>
public interface ICurrentContactAccessor
{
    /// <inheritdoc cref="ContactManagementContext.GetCurrentContact(bool)"/>
    ContactInfo? GetCurrentContact(bool createAnonymous = true);
}

public class CurrentContactAccessor : ICurrentContactAccessor
{
    public ContactInfo? GetCurrentContact(bool createAnonymous = true) =>
        ContactManagementContext.GetCurrentContact(createAnonymous);
}
