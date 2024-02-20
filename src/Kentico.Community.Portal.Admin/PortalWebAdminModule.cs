using CMS;
using Kentico.Community.Portal.Admin;
using Kentico.Xperience.Admin.Base;

[assembly: RegisterModule(typeof(PortalWebAdminModule))]

[assembly: UICategory(
   codeName: PortalWebAdminModule.QANDA_CATEGORY,
   name: "Q&A",
   icon: Icons.Personalisation,
   order: 100)]

[assembly: UICategory(
   codeName: PortalWebAdminModule.SUPPORT_REQUESTS_CATEGORY,
   name: "Support Requests",
   icon: Icons.File,
   order: 100)]

namespace Kentico.Community.Portal.Admin;

internal class PortalWebAdminModule : AdminModule
{
    public const string QANDA_CATEGORY = "kentico-community.portal-web-admin.q-and-a";
    public const string SUPPORT_REQUESTS_CATEGORY = "kentico-community.portal-web-admin.support-requests";

    public PortalWebAdminModule() : base(nameof(PortalWebAdminModule)) { }

    protected override void OnInit()
    {
        RegisterClientModule("kentico-community", "portal-web-admin");

        base.OnInit();
    }
}
