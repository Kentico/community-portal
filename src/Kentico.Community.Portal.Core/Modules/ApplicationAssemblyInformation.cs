using System.Reflection;

namespace Kentico.Community.Portal.Core.Modules;

/// <summary>
/// Exposes the current entry assembly's version and git hash information
/// </summary>
/// <remarks>
/// See https://www.hanselman.com/blog/adding-a-git-commit-hash-and-azure-devops-build-number-and-build-id-to-an-aspnet-website
/// </remarks>
public class ApplicationAssemblyInformation
{
    public ApplicationAssemblyInformation()
    {
        // Dummy version for local dev
        string versionAndHash = "1.0.0+NO_HASH";

        string infoVersion = Assembly.GetEntryAssembly()
            ?.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
            .Select(a => a.InformationalVersion)
            .FirstOrDefault() ?? "";

        if (0 < infoVersion.IndexOf('+') && infoVersion.IndexOf('+') <= infoVersion.Length)
        {
            // Hash is embedded in the version after a '+' symbol, e.g. 1.0.0+a34a913742f8845d3da5309b7b17242222d41a21
            versionAndHash = infoVersion;
        }

        Version = versionAndHash[..versionAndHash.IndexOf('+')];
        GitHash = versionAndHash[(versionAndHash.IndexOf('+') + 1)..];
    }

    /// <summary>
    /// The git hash of the code that the application's Entry Assembly was generated from
    /// </summary>
    public string GitHash { get; }
    /// <summary>
    /// The version of the application's Entry Assembly
    /// </summary>
    public string Version { get; }
}
