using Vite.AspNetCore.Abstractions;

namespace Kentico.Community.Portal.Web.Rendering;

public class ClientAssets
{
    private readonly IWebHostEnvironment env;
    private readonly IViteManifest manifest;

    public ClientAssets(IWebHostEnvironment env, IViteManifest manifest)
    {
        this.env = env;
        this.manifest = manifest;
    }

    /// <summary>
    /// Returns true if editormd is required on the current page
    /// </summary>
    /// <value></value>
    public bool EditormdRequired { get; internal set; } = false;
    /// <summary>
    /// Call to ensure Editormd.js is configured for the current page
    /// </summary>
    public void RequireEditormd() => EditormdRequired = true;

    /// <summary>
    /// Returns true if Prism.js is required on the current page
    /// </summary>
    /// <value></value>
    public bool PrismRequired { get; internal set; } = false;
    /// <summary>
    /// Call to ensure Prism.js is configured for the current page
    /// </summary>
    public void RequirePrism() => PrismRequired = true;

    /// <summary>
    /// Returns the normalized path to the asset for the Vite.js build system.
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public string ViteAssetPath(string assetPath)
    {
        if (env.IsDevelopment())
        {
            return $"/{assetPath}";
        }

        if (manifest.ContainsKey(assetPath))
        {
            return $"/{manifest[assetPath]?.File}";
        }

        return "";
    }
}
