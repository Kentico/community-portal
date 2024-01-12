# Architecture Decision Record

## 2024-01-02 - Image media asset processing

Xperience by Kentico's media library analyzes uploaded media and extracts metadata for specific file types - specifically image width/height values (`Media_File.FileImageWidth` and `Media_File.FileImageHeight`). This is a feature that comes from previous versions of Kentico.

However, current guidance is to [model media as a reusable content type](https://docs.xperience.io/x/Do3WCQ#Generalcontentmodelingrecommendations-Digitalmarketingfeatures) which means using the `ContentItemAsset` type to contain the media file.

Unfortunately, this approach to media management is still immature - Xperience doesn't perform this same metadata extraction and doesn't expose obvious APIs to enable a developer to perfor it themselves.

Here's an example of what the stored asset metadata looks like:

```json
{
    "Identifier": "84366bac-bb9a-48c1-87cc-bece7b52e68c",
    "Name": "milwaukee-kentico-user-group-logo.webp",
    "Extension": ".webp",
    "Size": 89882,
    "LastModified": "2023-12-16T18:54:59.8226954Z"
}
```

Ideally, Xperience would read the media metadata and store it in this JSON structure or allow a developer to customize the metadata.

Image width/height values are important for [prevening layout shift when rendering images](https://www.aleksandrhovhannisyan.com/blog/setting-width-and-height-on-images/) on the web.

To reproduce the media library behavior store this metadata, we have a custom `MediaAssetContentMetadataHandler` which uses the `MetadataExtractor` library to read the minimum amount of file binary to determine the stored width/height of an image file.
This metadata extraction is performed every time the custom `MediaAssetContent` content type is updated since we don't have any other hook into media file upload process where we could gather metadata and store it.

Ideally, in the future when Xperience's media asset pipeline is more advanced we can move this custom behavior to a different area of the application or remove it altogether.

## 2023-12-04 - Explicit channel context for data queries/commands

There are several global events triggered by operations performed in the Admin application.
Namely, publishing a Q&A question when a new blog post is published and connecting the two pieces of content.

These global events perform some of the data retrieval and update operations that are executed from the context
of the live Community Portal website channel, but the query and command code relied on the `IWebsiteChannelContext` internally to determine the current channel.
This meant when these commands and queries were executed from the Admin and the Admin domain was different than that of a website channel, the "channel context"
was null and would fail when accessed.

This kind of assumption was usually safe in earlier version of Kentico and Xperience by Kentico _before_ v27 because the Admin domain was always associated
with a specific site. Many customers might also be safe with this assumption in Xperience by Kentico v27+ if they have 1 website channel and access the Admin
from the same domain as the live site. But, this is not a best practice since _any_ channel (email, website, headless) can be managed in the Admin from a
single Admin domain, meaning an assumed website channel - based on the admin domain - might not be correct for a given operation. This issue is
encountered immediately when hosting a solution in the Xperience SaaS environment which uses separate domains for Admin, email and website channels.

To make the query and command code more reusable, and to remove the implicit assumption that data/content commands/queries will always be executed from
the context of a request to a website channel, the command/query C# record classes have been updated to require the channel name as a parameter
(noted by implementing the interface `IChannelContentQuery`).

The query/command channel name is populated by `IWebsiteChannelContext` for all operations triggered by engagement with a website channel,
but it is supplied in other ways when triggered by some enagement in the Admin.
