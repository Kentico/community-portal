using CMS.Helpers;

namespace Kentico.Community.Portal.Core.Content;

public partial class VideoContent
{
    public string VideoContentAssetMimeType =>
        MimeTypeHelper.GetMimetype(VideoContentAsset.Metadata.Extension);
}
