using CMS.MediaLibrary;
using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Rendering;
public record AssetViewModel(Guid ID, string Title, IMediaFileUrl URLData, string AltText, string Extension);
public record ImageAssetViewModel(Guid ID, string Title, IMediaFileUrl URLData, string AltText, AssetDimensions Dimensions, string Extension)
    : AssetViewModel(ID, Title, URLData, AltText, Extension);
