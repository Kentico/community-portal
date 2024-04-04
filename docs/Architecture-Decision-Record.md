# Architecture Decision Record

## 2024-04-04 - Support request processing settings

The requirement for Azurite to be running when developing locally is a complexity added when async support request processing
was added by leveraing Azure Storage. Azurite allows the Azure Storage service to be emulated locally and helps developing and testing
integrations with it.

However, it also means developers need to remember to start Azurite before starting the ASP.NET Core application. This is easy to forget
and causes exceptions to be thrown when not running because the client services cannot contact the endpoint URLs.

To help resolve this, an additional appsetting has been added to fully disable support request processing and initialization of the
storage client services has been moved behind a check of this setting.

This setting also overrides the administration configuration of this feature and the admin UI has been updated to reflect this.

In the future, we could have a better developer experience through technologies like [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview), which could manage required external services on startup.

## 2024-03-27 - Taxonomies migration

`BlogPostContent` reusable content items have simulated a taxonomy since the portal was launched, using a `BlogPostContent.BlogPostContentTaxonomy` string field.

With the introduction of [Taxonomies](https://docs.kentico.com/x/taxonomies_xp) in [v28.4.0](https://docs.kentico.com/changelog#refresh-march-21-2024) we can model taxonomies separate of the content, which makes future taxonomy management and content selection (for management and delivery) easier.

However, to [minimize the complexity of data migration](https://community.kentico.com/blog/safely-evolving-a-content-model-with-expand-and-contract) for the initial adoption of taxonomies for blog posts, we will be supporting both old and new taxonomies until all content has been migrated, which will happen _after_ the initial content type update.

`QAndABlogPostPage` web page content items will also be taking advantage of taxonomies, but since they don't have any existing taxonomy and fall into 2 very clear groups (based on title - do they start with "Blog Discussion:" or not), we can apply taxonomy during migration pretty easily.

Once all content has been migrated, we can remove the old `BlogPostContent.BlogPostContentTaxonomy` field and all of its dependee code.

An additional "general purpose" `DXTopic` taxonomy has been created which will be used by both `BlogPostContent` and `QAndAQuestionPage` content types (and possibly additional content types in the future - ex: `IntegrationContent`), however this taxonomy is not yet being used for any member experiences. It currently has a flat structure, but this can be modified in the future if it will benefit content management or visitor experience.

## 2024-02-22 - Auto search index rebuilding

Lucene search indexes are stored on the file system and when deployments in SaaS swap Azure App Service slots. After a deployment, a search index's files might not be up-to-date or even available.

Ideally, we could trigger a search index rebuild immediately after a deployment, but currently there's no way to determine (from the application's perspective) that a deployment took place.

We do have access to `ApplicationEvents.PostStart` which is an event triggered after an application has initialized and handled a request, but this event is triggered even after app pool recycles.
If we relied on this event to auto rebuild a search index, we'd have to accept that the index would be rebuilt too often, wasting resources and potentially impacting site performance after an app pool recycle.

We could also track assembly version numbers (of the deployed application) and compare the stored version to the version of the running application in the `ApplicationEvents.PostStart` event.
This would let us know when we encountered an app pool recycle and skip index rebuilding.

Because of the complexity of assembly version comparisons and the resource usage penalty without it, for now, we will manually rebuild search indexes after deployments.
This can be revisited in the future to improve the ability to fully automate deployments.

## 2024-02-19 - Support Requests Processing

Support requests (submitted through the website [support form](https://community.kentico.com/support)) were processed within the submission HTTP request.

This meant a failure to submit the support data to the internal Azure Function endpoint would prevent the user from submitting their request. In case of failures, support request data and files were stored on the transient App Service file system. If a deployment occurred or an App Service restarted, those files would be lost.

Additionally, the form submittered would have to wait for their support request submission _and_ the processing of that request (JSON serialization, file base64 encoding, POST request to the Azure Function) before they knew their submission had been successfully received or failed.

To mitigate the issues with losing form submissions, processing has been moved to `SupportMessageProcessorHostedService` which is an [ASP.NET Core background service](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-8.0&tabs=visual-studio#backgroundservice-base-class).

Submissions are now uploaded to Blob Storage as a JSON serialized file and a message is added to Queue Storage with the name of the blob. Although this still requires the serialization and encoding time, if the Azure Function endpoint is having issues, the support request isn't lost.

The background service checks for queue messages and processes them. This processing can be disabled through a new [custom module](https://docs.kentico.com/x/yASiCQ).

If processing a support request, the request is moved to a dead-letter Blob Container and a queue message is added to a dead-letter queue for future processing.

This change requires using [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage) for local development and the CI pipeline E2E tests, which adds complexity to the solution.

Future improvements include customizing the Support Requests Admin UI to allow re-queuing of failed support requests (via dead-letter queue),
and changing the dequeue delay for the background service.

## 2024-02-12 - Headless channel for content promotion

A new headless channel has been added to expose some of the solution's structured content from the Content hub - specifically Community Group Content items -
which can be used in web components to promote the Kentico Community Portal through other website channels.

This channel requires manual updating when new content is authored.

Future improvements include using [global events](https://docs.kentico.com/x/r4t1CQ) to automatically update the headless channel items
when new Content hub content items are created. For example, if the latest blog posts of each of the blog taxonomies is exposed through the headless channel,
then we should update that referenced content item when a new blog post is published.

## 2024-02-08 - Overriding Xperience views

Adding the [XperienceCommunity.PreviewComponentOutlines](https://github.com/seangwright/xperience-community-preview-component-outlines) NuGet package enables component annotations to be visible in the Preview view of the Page Builder.

![Community home page with Preview Component Outlines](/docs/images/XperienceCommunity.PreviewComponentOutlines-home-page.jpg)

This requires annotating the HTML of the components in the solution to function correctly. It normally is not possible to customize the HTML of the components built into
Xperience - the Rich Text and Form Widgets.

By adding copies of the Xperience Razor view files into this solution's application, using the same path and file name, ASP.NET Core will select our "override"
view at runtime. This means we can add our HTML annotations for the preview outlines.

There is a concern that future updates to the Form and Rich Text Widget Razor views could break the solution, as this kind of view overriding _is not_ supported by the product.
Mitigating any issues will require reviewing the [Changelog](https://docs.kentico.com/changelog) with each release and testing these components regularly to catch any
problems.

If there are any big problems in the future, we might delete these Razor views and accept not having component outlines for these Widgets.

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
