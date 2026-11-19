namespace Kentico.Community.Portal.Core.Modules;

/// <summary>
/// How long a form's submissions (<c>Form_*</c> rows) are kept. One row per classified form; a
/// form without a row is "unclassified" and kept (fail-safe). Stored as data, not settings, so
/// rows for production-only forms are never overwritten by a CD deploy.
/// </summary>
public partial class FormRetentionPolicyInfo
{
    static FormRetentionPolicyInfo() => TYPEINFO.ContinuousIntegrationSettings.Enabled = true;
}
