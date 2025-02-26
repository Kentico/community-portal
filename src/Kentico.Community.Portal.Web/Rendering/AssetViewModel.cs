using Kentico.Content.Web.Mvc;

namespace Kentico.Community.Portal.Web.Rendering;
public record AssetViewModel(Guid ID, string Title, IMediaFileUrl URLData, string AltText, string Extension);
