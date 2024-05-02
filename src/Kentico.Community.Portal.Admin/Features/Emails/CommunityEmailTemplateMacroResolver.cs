using CMS;
using CMS.Core;
using CMS.EmailLibrary;
using CMS.MacroEngine;
using Kentico.Community.Portal.Admin;

#pragma warning disable CS0618 // Type or member is obsolete

[assembly: RegisterImplementation(
    typeof(IEmailTemplateMacroResolver),
    typeof(CommunityEmailTemplateMacroResolver),
    Lifestyle = Lifestyle.Singleton,
    Priority = RegistrationPriority.SystemDefault)]

namespace Kentico.Community.Portal.Admin;

public class CommunityEmailTemplateMacroResolver(IEmailTemplateMacroResolver emailTemplateMacroResolver)
    : IEmailTemplateMacroResolver
{
    private readonly IEmailTemplateMacroResolver emailTemplateMacroResolver = emailTemplateMacroResolver;

    public MacroResolver Get()
    {
        var macroResolver = emailTemplateMacroResolver.Get();

        macroResolver.RegisterNamespace(EmailNamespace.Instance, allowAnonymous: true);

        return macroResolver;
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
