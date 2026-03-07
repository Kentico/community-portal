using CMS.Helpers;

namespace Kentico.Community.Portal.Core.Content;

public partial class VideoContent
{
    public static Guid CONTENT_TYPE_GUID { get; } = new Guid("c68f9f4f-ff6c-4b72-b25f-064ec4b2abec");

    public string VideoContentAssetMimeType =>
        MimeTypeHelper.GetMimetype(VideoContentAsset.Metadata.Extension);
}
