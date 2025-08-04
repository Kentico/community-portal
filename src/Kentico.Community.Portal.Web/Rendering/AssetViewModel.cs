using CMS.ContentEngine;

namespace Kentico.Community.Portal.Web.Rendering;

public record AssetViewModel(Guid ID, string Title, ContentItemAsset URLData, string AltText, string Extension);
