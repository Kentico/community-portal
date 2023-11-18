using CMS;
using Kentico.Community.Portal.Admin;
using Kentico.Xperience.Admin.Base;

[assembly: RegisterModule(typeof(PortalWebAdminModule))]

[assembly: UICategory(
   codeName: PortalWebAdminModule.CATEGORY,
   name: "Community",
   icon: Icons.Personalisation,
   order: 100)]

namespace Kentico.Community.Portal.Admin;

internal class PortalWebAdminModule : AdminModule
{
    public const string CATEGORY = "kentico-community.portal-web-admin.category";

    public PortalWebAdminModule() : base(nameof(PortalWebAdminModule)) { }

    protected override void OnInit()
    {
        RegisterClientModule("kentico-community", "portal-web-admin");

        base.OnInit();
    }
}
