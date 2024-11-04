namespace Kentico.Community.Portal.Core.Modules;

public partial class SupportRequestConfigurationInfo
{
    static SupportRequestConfigurationInfo()
    {
        TYPEINFO.ContinuousIntegrationSettings.Enabled = true;
        TYPEINFO.SensitiveColumns = [nameof(SupportRequestConfigurationExternalEndpointURL)];
    }
}
