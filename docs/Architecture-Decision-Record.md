# Architecture Decision Record

## 2025-02-13 - Polls

To make the Kentico Community Portal more interactive and informative, we want
to introduce a poll solution.

Polls would allow visitors to quickly and easily share their response to a
simple question. Once the question is answered, the results to the poll should
be visible so the user can see how others answered.

Ideally these polls could be embedded in various places across the portal and if
a visitor answered a poll they would not be able to answer that poll again (to
prevent manipulating results and prevent repeated content annoyance).

Historical poll results would also be valuable for Kentico and the community
over time because the results could show trends in adoption of product features
or interests in digital experience topics, informing everyone what time of
content or Q&A discussion questions to author.

If polls are accessible to anonymous visitors they will need to have an
anti-spam capability.

### 3rd party poll embed

Products like <https://www.poll-maker.com/>,
<https://www.mentimeter.com/features/live-polling>,
<https://www.slido.com/features-live-polling>,
<https://www.surveymonkey.com/mp/online-polls/> , and
<https://www.jotform.com/poll-maker/> all have the features needed for an
effective portal poll experience. However, they also have permanently recurring
costs (subscriptions) or significant limitations on free tiers
(users/views/submissions).

Most of these solutions will have an anti-spam solution and a good UX (once the
JS is loaded).

Polls would have to be embedded and rendered client-side which means
[layout shift](https://web.dev/articles/cls) could become a problem.

Upfront investment in time with this option is low but cost is high.

### Custom application and object type

Xperience by Kentico's extremely
[flexible and powerful administration customization capabilities](https://docs.kentico.com/x/yASiCQ)
means we could build a poll management solution from scratch, giving marketers
using the portal a simple UI to author new polls, poll questions, and view the
results or trends of poll engagement with graphs.

Polls could embedded into website experiences with widgets and follow any
business rules to ensure a high quality poll result and good visitor experience.

We would have to design an anti-spam solution, a poll content or object model,
and a way to display results (with a graph or table).

This option would require a significant amount of up-front time investment. Cost
is "free" because we'd only use Xperience's native features.

### Form Builder

A single radio button field Form Builder form is effectively a poll and the
Kentico Community Portal already has multiple form widgets (native Form Widget,
Fallback form widget) that display forms.

However, each Form Builder form is a unique object so this approach would need a
way to display form results dynamically.

This option has similar requirements to the _Custom application and object type_
option above, but it would not need a content model or widget since the Form
Builder dynamically creates content models with each form.

The upfront time cost would be medium, assuming technology challenges can be
resolved.

### Solution - Polls

We selected using the Form Builder for several reasons.

1. Existing widgets/patterns make it easy to start
1. Customizing forms in Xperience, while not supported, has been shown to be
   possible with the MVP/CL Activity and Fallback form widgets
1. We can restrict poll access to members only (using the widget personalization
   technique or explicit business logic) which avoids the spam problem
1. If polls because popular we can transition to a custom application and object
   type and even migrate data if necessary

Polls built with the Form Builder will require several "conventions" to work
correctly

1. All poll forms will need a "Hidden Member ID" field named `MemberID`
1. All poll forms will need a "Radio button" field named `Question`
1. Polls will need to be displayed with new Poll widget which has similar
   features to the Fallback form, except it uses business logic for visibility
   (we'll never want to display polls to non-members)
1. We add a new client-side js library <https://www.chartjs.org/> to the
   solution to render simple charts for poll results. Chartjs is loaded
   asynchronously, on demand as an es module (similar to Q&A and search
   modules).
1. We have a new `PollContent` reusable content type which references the Form
   and includes other content fields to improve the form experience for members.

## 2024-11-15 - Faceted search

The blog and Q&A search experiences have been updated with faceted searching
based on the taxonomy tags applied to blog post content items and Q&A question
pages.

This enhancement required large updates to the search indexing, querying, and
rendering logic for both indexes. The faceting feature is enabled by
[Kentico.Xperience.Lucene](https://github.com/Kentico/xperience-by-kentico-lucene)
using the latest Lucene.NET 4.8 beta library which features faceted search.

While the indexing and querying logic is mostly finalized, the search UX will
continue to be refined based on accuracy of querying and the tagging strategy of
content.

Publishing new blog post pages results in a Q&A question page automatically
being created for the post. This new question page is now populated with the
blog post's **DXTopics** and will drive the faceted searching for Q&A.

A future update will enable members to select their tags for their questions
from the list of tags in the **DXTopics** taxonomy.

## 2024-10-27 - Milkdown for Q&A discussions

The [editor.md](https://github.com/pandao/editor.md) library was initially used
in this application to give members a way to ask Q&A questions and answer those
questions in a way that would be secure for visitors. Authoring in markdown
means all HTML would be properly escaped and administrators would not need to
worry about which HTML tags would be allowed (all were disabled).

**editor.md** is an old, unmaintained, and large library so we began to look for
a replacement soon after we added it. Multiple options were considered, but
[Milkdown](https://milkdown.dev/playground) was the most flexible, easy to
integrate, and well maintained option.

**Milkdown** improved the editing experience with a simple no-code UI. It also
allowed us to remove jQuery (which was a dependency of **editor.md**) as part of
applying the
[October 2024 Refresh](https://docs.kentico.com/changelog#refresh-october-17-2024).

No changes were required on the data storage or backend processing side because
the content submitted by members and rendered on the website is the same set of
Markdown tags.

Future updates will allow us to use **Milkdown** in a UI Form Component for
administration content authoring.

## 2024-07-18 - Member badges

To reward Kentico community members for participating on the Kentico Community
Portal and engaging with Kentico in other ways, we've added Member Badges.

Member Badges are awards that are assigned to community member accounts. They
appear in the member account management page and can be selected for display by
members outside of their member profile. By default new badges are not selected
for display outside of a member's profile, but all awarded badges appear within
their public profile page.

Badges can be assigned manually by administrators or automatically by rules
which execute in the background and assign badges based on programmatic rules.
For example, if a member asks a question in the Q&A, they are awarded a badge.

Member Badges could have been designed with various implementations and
features. The solution selected was a middleground of simplicity in data access
and management (for administrators) and presentational experience (for members).

Although the badges could have been modeled purely as reusable content in the
Content hub, Xperience's custom modules and object types made it extremely easy
to use code name and ID values to do look ups and model relationships between
badges and members.

It's possible that we will move to a purely content-focused approach for the
badge data (title, image, description), but they work well enough when managed
from a custom module. Had the number of badges been in the hundreds, with
localized content, and delivered across all channels, then using a reusable
content type to manage their content would have had many benefits.

We did decide to use media assets in the Content hub for the badge images
instead of the Media library, primarily because of the features of the Content
hub and future deprecation of the Media library.

To perform automatic badge assignment with rules we leveraged Xperience's
`ApplicationBackgroundService` which is a more feature rich wrapper over ASP.NET
Core's
[BackgroundService class](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-8.0&tabs=visual-studio#backgroundservice-base-class).
For background operations that do not need custom scheduling and on-demand
execution, the `ApplicationBackgroundService` is an ideal replacement for
Kentico's older Scheduled Tasks, which are not available in Xperience by
Kentico.

To ensure rule processing with faulty logic doesn't negatively impact the
solution, rules can be individually disabled until data can be changed or a code
fix can be deployed.

In the future, we plan to create a page for the community that shows all the
badges available and all the members who have been awarded those badges.

## 2024-06-24 - Restricted pages

Although Xperience supports content authorization for content delivery, it's
limited to a single [secure](https://docs.kentico.com/x/8oouCw) property which
does not automatically translate to channel access restrictions - this requires
additional development.

There are several approaches which could be adopted for securing content based
on some authenticated member state:

### Custom role and permission system

We could create a custom module which would manage roles and permission
relationships to Members. This would enable completely dynamic and
administrator-driven content authorization for members, but would still require
a way to associate roles to content.

### Simple content field

Another approach would be to add a specific custom field to content items that
need some business-rule driven content authorization. The field could be a field
populated by a preset list of options, either single or multiple choice. These
options could be the value that business logic would check against or a
reference to some other data (like a role or permission) in Xperience that would
be checked.

### Solution

For now, the primary use-case is to restrict content and web pages to a small
set of groups of members - internal Kentico employees and Kentico MVPs, with the
option of expanding to tiers of community members in the future.

We also need some of both approaches above - a way to check if accessing a piece
of content requires a member status and a way to check which status a given
member has when trying to access a restricted piece of content.

To keep the solution as simple as possible but allow content (specifically web
pages) to be restricted dynamically, we've added several pieces of
infrastructure:

1. A `ContentAuthorization` taxonomy with several tags.
1. A `ContentAuthorization` reusable field schema which has a single taxonomy
   field. The tags in this field define the statuses of a member that allow
   access to the web page.
1. Assignment of the RFS to `LandingPage` web page content items (the RFS _can_
   be added to other web page content types).
1. An ASP.NET Core MVC `IAsyncAuthorizationFilter` named
   `ContentAuthorizationFilter` which authorizes access to web pages if they
   have any of the `ContentAuthorization` tags assigned to them.

The filter only performs extra work for a request when the member is
authenticated and the request is not a Page Builder request. Only 1 "status"
check (is MVP) is performed since we do not yet have other member data to check
against at this time.

We decided not to use ASP.NET Core's policy authorization since the complexity
of the requirements do not yet justify it. A full role and permission system
driven by taxonomy tags can be developed in the future if needed.

## 2024-06-11 - Member Avatars and Administration Reporting

### Member Avatars

To enhance the sense of community and allow members to personalize their own
experience in the Kentico Community Portal, we wanted to enable members to
upload their own custom avatars for their accounts.

These avatars will appear on their member profile pages and in Q&A in any
questions/discussions or answers they create.

To simplify caching, asset management, file system access, and align with SaaS'
use of Azure Storage, we are using the Xperience's integration with Azure Blob
Storage using the `StorageInitializationModule` by mapping a new storage path
for member assets.

We had considered persisting these avatars as content items within the Content
hub. While this is entirely possible (we store member generated Q&A questions as
web page items), it's not designed for it.

Marketers would never reuse this content, it doesn't benefit from any of
Xperience's content management features (multi-channel, scheduled publishing,
workflow, ect...), and these avatar items would pollute the marketer-authored
content in the Content hub.

We instead decided to keep the content management minimal and associate the
images directly with member IDs, which are a stable and unique identifier in the
production environment. Members can upload their avatar and replace it whenever
they want. If an administrator needs to "remove" an image, they can update one
of the member's fields and the fallback image will be used instead.

This architecture should lay the groundwork for future support for member
uploads of images for Q&A, if that feature becomes a priority.

### Administration Reporting

As the Kentico Community Portal has grown since launching in October 2023, a
need for more data insights has appeared. Qualitative KPIs have evolved into
quantitative ones.

To provide those insights both for point in time and overall trends, we are
adding a Reporting application to the Xperience administration which can
maintain a growing number of custom reports. Currently, these reports focus on
membership numbers and activities. They are static but can be enhanced to
provide dynamic reporting.

Xperience already has a license for and uses
[amcharts](https://www.amcharts.com/), which easily integrates into the
React-based administration UI, so report data is visualized using this library.

More reports and report enhancements can be added gradually overtime until
Xperience is able to natively provide some of this functionality.

## 2024-05-23 - Sending internal form submission autoresponders

Although Xperience has
[autoresponders for form submissions](https://docs.kentico.com/business-users/digital-marketing/emails#assign-emails-to-form-autoresponders),
those are sent to the submitter of the form.

If an administrator wants to receive an email notification that a form has been
submitted, the autoresponder itself does not help here.

To more easily associate administration users with form submission
notifications, a new `BizFormSettingsInfo` custom object type has been created,
which can store additional custom configuration for a form - in this case, the
"internal" autoresponder status (enabled/disabled) and the User assigned to
receive those emails.

These settings could be customized and expanded in the future to support
non-User email addresses, or multiple email addresses per form (however it is
possible that future marketing automation features will make this customization
unnecessary).

These custom form settings do not have their own administration UI application,
but instead are managed through the forms they are associated with through a new
custom slide-out tray in the form configuration UI.

The default setting for a new form is not to send internal autoresponders.

## 2024-05-14 - Sending emails from email channel sending domains

Xperience does not currently have the concept of a "system" or "transactional"
email/email channel. Instead,
[emails have purposes](https://docs.kentico.com/business-users/digital-marketing/emails).

Autoresponder is the closest to "transactional" until future dedicated support
for transactional emails is available.

To send an email programmatically through an email channel's sending domain in
SaaS, we must use a marketer authored email's configuration.

All of this is related to confirmation emails for support requests, which are
sent when an [a support request](https://community.kentico.com/support) is
submitted and the request is processed from the queue.

In order to send the emails from the email channel sender domain
(`community.kentico.com`), we have a placeholder email that will be used to
configure the email when it's sent by the system.

> Note: This placeholder email is defined in the `SystemEmails` class.

If we are willing to use the SaaS environment service domain, we can simply send
the email from a service domain email address, but this _can_ have issues with
deliverability and is going to be unexpected by website visitors.

## 2024-05-01 - Alpine.js

Instead of authoring difficult to test, edit, and maintain jQuery for filtering
on the Integrations page, we've added a dependency on
[Alpine.js](https://alpinejs.dev/).

This enables us to author simple interactive UIs without custom JavaScript and
co-locate all custom behavior with the markup that behavior operates on. See
`IntegrationsList.cshtml` for an example.

In the future we can convert the Blog and Q&A search experiences to use either
Alpine.js, HTMX, or both and remove most of the custom JavaScript for these
basic UIs.

## 2024-05-01 - Taxonomies migration part 2

Similar to
[2024-03-27 - Taxonomies migration](#2024-03-27---taxonomies-migration),
`IntegrationContent` content items use simulated taxonomy, created before
Taxonomy support existed in Xperience by Kentico.

To support migrating towards true taxonomies and a new filtering UI for the
Integrations page, we have introduced a new Taxonomy - "Integration Type" - and
updated the `IntegrationContent` content type with a taxonomy field.

Both the old and new taxonomy approaches will be supported simultaneously until
all content has been updated to have assigned taxonomy tags.

## 2024-04-04 - Support request processing settings

The requirement for Azurite to be running when developing locally is a
complexity added when async support request processing was added by leveraing
Azure Storage. Azurite allows the Azure Storage service to be emulated locally
and helps developing and testing integrations with it.

However, it also means developers need to remember to start Azurite before
starting the ASP.NET Core application. This is easy to forget and causes
exceptions to be thrown when not running because the client services cannot
contact the endpoint URLs.

To help resolve this, an additional appsetting has been added to fully disable
support request processing and initialization of the storage client services has
been moved behind a check of this setting.

This setting also overrides the administration configuration of this feature and
the admin UI has been updated to reflect this.

In the future, we could have a better developer experience through technologies
like
[.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview),
which could manage required external services on startup.

## 2024-03-27 - Taxonomies migration

`BlogPostContent` reusable content items have simulated a taxonomy since the
portal was launched, using a `BlogPostContent.BlogPostContentTaxonomy` string
field.

With the introduction of [Taxonomies](https://docs.kentico.com/x/taxonomies_xp)
in [v28.4.0](https://docs.kentico.com/changelog#refresh-march-21-2024) we can
model taxonomies separate of the content, which makes future taxonomy management
and content selection (for management and delivery) easier.

However, to
[minimize the complexity of data migration](https://community.kentico.com/blog/safely-evolving-a-content-model-with-expand-and-contract)
for the initial adoption of taxonomies for blog posts, we will be supporting
both old and new taxonomies until all content has been migrated, which will
happen _after_ the initial content type update.

`QAndABlogPostPage` web page content items will also be taking advantage of
taxonomies, but since they don't have any existing taxonomy and fall into 2 very
clear groups (based on title - do they start with "Blog Discussion:" or not), we
can apply taxonomy during migration pretty easily.

Once all content has been migrated, we can remove the old
`BlogPostContent.BlogPostContentTaxonomy` field and all of its dependee code.

An additional "general purpose" `DXTopic` taxonomy has been created which will
be used by both `BlogPostContent` and `QAndAQuestionPage` content types (and
possibly additional content types in the future - ex: `IntegrationContent`),
however this taxonomy is not yet being used for any member experiences. It
currently has a flat structure, but this can be modified in the future if it
will benefit content management or visitor experience.

## 2024-02-22 - Auto search index rebuilding

Lucene search indexes are stored on the file system and when deployments in SaaS
swap Azure App Service slots. After a deployment, a search index's files might
not be up-to-date or even available.

Ideally, we could trigger a search index rebuild immediately after a deployment,
but currently there's no way to determine (from the application's perspective)
that a deployment took place.

We do have access to `ApplicationEvents.PostStart` which is an event triggered
after an application has initialized and handled a request, but this event is
triggered even after app pool recycles. If we relied on this event to auto
rebuild a search index, we'd have to accept that the index would be rebuilt too
often, wasting resources and potentially impacting site performance after an app
pool recycle.

We could also track assembly version numbers (of the deployed application) and
compare the stored version to the version of the running application in the
`ApplicationEvents.PostStart` event. This would let us know when we encountered
an app pool recycle and skip index rebuilding.

Because of the complexity of assembly version comparisons and the resource usage
penalty without it, for now, we will manually rebuild search indexes after
deployments. This can be revisited in the future to improve the ability to fully
automate deployments.

## 2024-02-19 - Support Requests Processing

Support requests (submitted through the website
[support form](https://community.kentico.com/support)) were processed within the
submission HTTP request.

This meant a failure to submit the support data to the internal Azure Function
endpoint would prevent the user from submitting their request. In case of
failures, support request data and files were stored on the transient App
Service file system. If a deployment occurred or an App Service restarted, those
files would be lost.

Additionally, the form submittered would have to wait for their support request
submission _and_ the processing of that request (JSON serialization, file base64
encoding, POST request to the Azure Function) before they knew their submission
had been successfully received or failed.

To mitigate the issues with losing form submissions, processing has been moved
to `SupportMessageProcessorHostedService` which is an
[ASP.NET Core background service](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-8.0&tabs=visual-studio#backgroundservice-base-class).

Submissions are now uploaded to Blob Storage as a JSON serialized file and a
message is added to Queue Storage with the name of the blob. Although this still
requires the serialization and encoding time, if the Azure Function endpoint is
having issues, the support request isn't lost.

The background service checks for queue messages and processes them. This
processing can be disabled through a new
[custom module](https://docs.kentico.com/x/yASiCQ).

If processing a support request, the request is moved to a dead-letter Blob
Container and a queue message is added to a dead-letter queue for future
processing.

This change requires using
[Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage)
for local development and the CI pipeline E2E tests, which adds complexity to
the solution.

Future improvements include customizing the Support Requests Admin UI to allow
re-queuing of failed support requests (via dead-letter queue), and changing the
dequeue delay for the background service.

## 2024-02-12 - Headless channel for content promotion

A new headless channel has been added to expose some of the solution's
structured content from the Content hub - specifically Community Group Content
items - which can be used in web components to promote the Kentico Community
Portal through other website channels.

This channel requires manual updating when new content is authored.

Future improvements include using
[global events](https://docs.kentico.com/x/r4t1CQ) to automatically update the
headless channel items when new Content hub content items are created. For
example, if the latest blog posts of each of the blog taxonomies is exposed
through the headless channel, then we should update that referenced content item
when a new blog post is published.

## 2024-02-08 - Overriding Xperience views

Adding the
[XperienceCommunity.PreviewComponentOutlines](https://github.com/seangwright/xperience-community-preview-component-outlines)
NuGet package enables component annotations to be visible in the Preview view of
the Page Builder.

![Community home page with Preview Component Outlines](/docs/images/XperienceCommunity.PreviewComponentOutlines-home-page.jpg)

This requires annotating the HTML of the components in the solution to function
correctly. It normally is not possible to customize the HTML of the components
built into Xperience - the Rich Text and Form Widgets.

By adding copies of the Xperience Razor view files into this solution's
application, using the same path and file name, ASP.NET Core will select our
"override" view at runtime. This means we can add our HTML annotations for the
preview outlines.

There is a concern that future updates to the Form and Rich Text Widget Razor
views could break the solution, as this kind of view overriding _is not_
supported by the product. Mitigating any issues will require reviewing the
[Changelog](https://docs.kentico.com/changelog) with each release and testing
these components regularly to catch any problems.

If there are any big problems in the future, we might delete these Razor views
and accept not having component outlines for these Widgets.

## 2024-01-02 - Image media asset processing

Xperience by Kentico's media library analyzes uploaded media and extracts
metadata for specific file types - specifically image width/height values
(`Media_File.FileImageWidth` and `Media_File.FileImageHeight`). This is a
feature that comes from previous versions of Kentico.

However, current guidance is to
[model media as a reusable content type](https://docs.kentico.com/x/Do3WCQ#Generalcontentmodelingrecommendations-Digitalmarketingfeatures)
which means using the `ContentItemAsset` type to contain the media file.

Unfortunately, this approach to media management is still immature - Xperience
doesn't perform this same metadata extraction and doesn't expose obvious APIs to
enable a developer to perfor it themselves.

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

Ideally, Xperience would read the media metadata and store it in this JSON
structure or allow a developer to customize the metadata.

Image width/height values are important for
[prevening layout shift when rendering images](https://www.aleksandrhovhannisyan.com/blog/setting-width-and-height-on-images/)
on the web.

To reproduce the media library behavior store this metadata, we have a custom
`MediaAssetContentMetadataHandler` which uses the `MetadataExtractor` library to
read the minimum amount of file binary to determine the stored width/height of
an image file. This metadata extraction is performed every time the custom
`MediaAssetContent` content type is updated since we don't have any other hook
into media file upload process where we could gather metadata and store it.

Ideally, in the future when Xperience's media asset pipeline is more advanced we
can move this custom behavior to a different area of the application or remove
it altogether.

## 2023-12-04 - Explicit channel context for data queries/commands

There are several global events triggered by operations performed in the Admin
application. Namely, publishing a Q&A question when a new blog post is published
and connecting the two pieces of content.

These global events perform some of the data retrieval and update operations
that are executed from the context of the live Community Portal website channel,
but the query and command code relied on the `IWebsiteChannelContext` internally
to determine the current channel. This meant when these commands and queries
were executed from the Admin and the Admin domain was different than that of a
website channel, the "channel context" was null and would fail when accessed.

This kind of assumption was usually safe in earlier version of Kentico and
Xperience by Kentico _before_ v27 because the Admin domain was always associated
with a specific site. Many customers might also be safe with this assumption in
Xperience by Kentico v27+ if they have 1 website channel and access the Admin
from the same domain as the live site. But, this is not a best practice since
_any_ channel (email, website, headless) can be managed in the Admin from a
single Admin domain, meaning an assumed website channel - based on the admin
domain - might not be correct for a given operation. This issue is encountered
immediately when hosting a solution in the Xperience SaaS environment which uses
separate domains for Admin, email and website channels.

To make the query and command code more reusable, and to remove the implicit
assumption that data/content commands/queries will always be executed from the
context of a request to a website channel, the command/query C# record classes
have been updated to require the channel name as a parameter (noted by
implementing the interface `IChannelContentQuery`).

The query/command channel name is populated by `IWebsiteChannelContext` for all
operations triggered by engagement with a website channel, but it is supplied in
other ways when triggered by some enagement in the Admin.
