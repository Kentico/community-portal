using System.Configuration;
using CMS;
using CMS.DataEngine;
using Kentico.Community.Portal.Web.Tests.Configuration;

[assembly: RegisterModule(typeof(TestSettingsModule))]

namespace Kentico.Community.Portal.Web.Tests.Configuration;

public class TestSettingsModule : Module
{
    public TestSettingsModule() : base(nameof(TestSettingsModule))
    {
    }

    protected override void OnPreInit()
    {
        base.OnPreInit();

        ReplaceCMSTestConnectionString();
    }

    /// <summary>
    /// Replaces the application config test connection string with one from a local configuration
    /// file if it exists
    /// </summary>
    private void ReplaceCMSTestConnectionString()
    {
        var config = ConfigurationManager.OpenMappedExeConfiguration(new()
        {
            ExeConfigFilename = "Tests.Local.config"
        }, ConfigurationUserLevel.None);

        var conn = config.ConnectionStrings.ConnectionStrings["CMSTestConnectionString"];
        if (conn is null)
        {
            return;
        }

        var appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var existingAppConn = appConfig.ConnectionStrings.ConnectionStrings["CMSTestConnectionString"];

        if (existingAppConn is null)
        {
            appConfig.ConnectionStrings.ConnectionStrings.Add(new()
            {
                Name = "CMSTestConnectionString",
                ConnectionString = conn.ConnectionString
            });
        }
        else
        {
            appConfig.ConnectionStrings.ConnectionStrings["CMSTestConnectionString"].ConnectionString = conn.ConnectionString;
        }

        appConfig.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection("connectionStrings");
    }
}
