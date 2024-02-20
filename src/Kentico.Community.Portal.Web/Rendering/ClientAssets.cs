using Microsoft.Extensions.Options;
using Vite.AspNetCore;

namespace Kentico.Community.Portal.Web.Rendering;

public class ClientAssets
{
    private readonly IWebHostEnvironment env;
    private readonly IViteManifest manifest;
    private readonly string basePath;

    public ClientAssets(IWebHostEnvironment env, IViteManifest manifest, IOptions<ViteOptions> options)
    {
        this.env = env;
        this.manifest = manifest;
        basePath = $"/{options.Value.Base?.Trim('/')}";
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
            return $"{basePath}/{assetPath}";
        }

        if (manifest.ContainsKey(assetPath))
        {
            string file = $"{basePath}/{manifest[assetPath]?.File}";
            return file;
        }

        return "";
    }
}
