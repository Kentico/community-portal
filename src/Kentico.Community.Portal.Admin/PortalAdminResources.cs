using CMS.Base;
using CMS.Localization;

using Kentico.Community.Portal.Admin;

[assembly: RegisterLocalizationResource(
    typeof(PortalAdminResources),
    SystemContext.SYSTEM_CULTURE_NAME)]

namespace Kentico.Community.Portal.Admin;

internal class PortalAdminResources { }
