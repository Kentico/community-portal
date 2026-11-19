# #342 WebP image inventory

Parent: #317. Depends on #321 (`Rendering/ImageVariants.cs`).

Counts come from the local CI repository (`App_Data/CIRepository`) and
`wwwroot`, not from PROD. PROD content can differ (see "Check PROD").

## First-party mechanism

Xperience **Automatic image format conversion** on a Content item asset field
(Asset uploader settings `IsFormatConversionEnabled`, `InputImageExtensions`,
`OutputImageExtension`, `OutputQuality`).

- Applies on upload, from the admin UI and from the API
  (`IContentItemManager`).
- Image variants are generated from the optimized primary image, so they
  inherit its format. No separate variant format setting exists.
- Existing assets are not converted retroactively. Editors convert one by one
  with Content hub > asset > **Adjust image** > Image format WEBP.
- `Kentico.Xperience.ImageProcessing` is already referenced by
  `Kentico.Community.Portal.Web`.

Docs:
[content types - automatically optimize image assets](https://docs.kentico.com/documentation/developers-and-admins/development/content-types#automatically-optimize-image-assets),
[content item assets - optimize image assets](https://docs.kentico.com/documentation/business-users/content-hub/content-item-assets#optimize-image-assets),
[optimize images during upgrade](https://docs.kentico.com/guides/upgrade-to-xbyk/upgrade-deep-dives/optimize-images-during-upgrade).

No custom middleware needed.

## Inventory

| Source | Where | Format today | Status |
| --- | --- | --- | --- |
| `ImageContent.ImageContentAsset` (featured images, Image widget, author photos, badges, rich text image variants, og:image) | Content hub | WebP conversion on (jpg/jpeg/png -> webp, q80). 395 of 404 primaries webp; 3 jpg, 6 png | Config OK. 4 items to convert, 5 stay (below) |
| Image variants (`Small_Scaled` .. `Large_X3_Scaled`, `Thumbnail*`) | `cms.standardmediadimensions` | Same format as primary | OK once primary is webp |
| `MemberAssetContent.MemberAssetContentImageAsset` (member avatars) | Content hub, uploaded via `AvatarImageService` (API) | WebP conversion on (jpg/jpeg/png/webp -> webp, q80). Dev data: all webp | OK |
| Default member avatar `/img/profile-photo-default.*` | `wwwroot` | was PNG | **Converted to WebP** |
| Kentico avatar for `MemberID == 0` `/img/kentico.*` | `wwwroot` | was PNG | **Converted to WebP** |
| `VideoContent.VideoContentAsset` | Content hub | mp4/webm (video), 1 animated webp | OK (not images) |
| `SocialLinkContent` logos (light/dark) | Content hub, email footer only | PNG, no conversion | Stays PNG (email) |
| og:image / twitter:image | Featured `ImageContent` asset, fallback global logo (`KenticoLogo_Block`, webp) | WebP | Stays WebP (see decision) |
| Blog hero, 404 bg, icons, link icons | `wwwroot/img/**/*.svg` | SVG | Stays SVG |
| `wwwroot/img/bg/hero-bg.jpg` | `--hero-bg` in `_section.scss` | JPG | Unused (variable never read). Not served by any page |
| `wwwroot/img/icons/picture-placeholder.png` | none | PNG | Unused (`icons.svg` symbol used instead) |
| Favicons, touch icon, web manifest icons | `wwwroot/manifest` | PNG, ICO, SVG | Stays (platform requirements) |
| CSS backgrounds | `Client/styles` | SVG only in use | OK |
| Q&A / markdown images | External hosts (imgur, postimg, docs.kentico.com, ...) | Mixed | Out of our control |
| Media library (`/getmedia/`) | not used | n/a | n/a |
| Email Builder Image widget | `ImageContent` | WebP | Risk: Outlook desktop does not render WebP. Out of scope, follow-up |

## Non-WebP `ImageContent` items (dev CI repo)

| Item | Format | Used by | Action |
| --- | --- | --- | --- |
| `ContentTypeC_CodeGenerationInPowerShell-g4qwdgre` | jpg | blog post | **Convert** |
| `Github-new-repository-repo-template-uht82izd` | jpg | blog post | **Convert** |
| `Kentico-github-repo-template-repository-f51g1z9o` | jpg | blog post | **Convert** |
| `KenticoLogo_shortcutColorPositive-4zsr9y5h` (100x100) | png | integration pages | **Convert** |
| `Kentico-logo-no-padding-4qwnftnj` | png | email defaults (logo) | Stays PNG: email clients |
| `Kentico-logo-no-padding-for-dark-bg-wbipfbfx` | png | email defaults (logo) | Stays PNG: email clients |
| `KenticoLogo_shortcutColorPositive-31i3t58n` (600x600) | png | `kentico.general` notification email template | Stays PNG: email clients |
| `KenticoMVP2025DigitalBadge-wylchtf0` | png (explicitly re-optimized to PNG) | downloadable digital badge | Stays PNG: used outside the site |
| `KenticoCommunityLeader2025DigitalBadge-quwf5zdw` | png (explicitly re-optimized to PNG) | downloadable digital badge | Stays PNG: used outside the site |

## Decisions

- **og:image stays WebP.** Xperience serves only the optimized primary and its
  variants, all in one format. A JPG rendition would need custom image
  processing (rejected: prefer first-party). Facebook and X are believed to
  accept WebP og:image; LinkedIn is uncertain. None verified here. Check a blog
  post URL in each platform's preview/debug tool on QA SaaS.
- **Email images stay PNG/JPG.** Outlook desktop does not render WebP. Covers
  `SocialLinkContent` logos and email logo `ImageContent` items. Field
  conversion on `SocialLinkContent` stays off.
- **Static PNGs converted** with SkiaSharp (Xperience's own image library),
  quality 90, alpha kept. `profile-photo-default` 14.2 KB -> 11.6 KB,
  `kentico` 4.4 KB -> 2.8 KB.

## Admin actions needed (not done here)

No content type or image variant definition change is needed. Only content:

1. Content hub, for each **Convert** item above: edit asset > **Adjust
   image** > Primary image > Image format **WEBP**, Quality **80** (matches the
   field setting) > Save > Publish. Variants regenerate from the new primary.
2. Local/dev: after editing, run CI store (`--kxp-ci-store`) so
   `App_Data/CIRepository` and `assets/contentitems` update. Commit.
3. QA SaaS / PROD: content items are not deployed by CD config v2 unless
   listed. Repeat step 1 in each environment's admin, or add the item
   identifiers to the release's CD config. Do not use the Management MCP.

### Check PROD

Find non-WebP primaries (run read-only against the PROD DB copy or QA):

```sql
SELECT cicd.ContentItemCommonDataContentItemID, ci.ContentItemName
FROM KenticoCommunity_ImageContent ic
JOIN CMS_ContentItemCommonData cicd ON cicd.ContentItemCommonDataID = ic.ContentItemDataCommonDataID
JOIN CMS_ContentItem ci ON ci.ContentItemID = cicd.ContentItemCommonDataContentItemID
WHERE JSON_VALUE(ic.ImageContentAsset, '$.Extension') <> '.webp'
```

Table name from `ClassTableName`; join columns inferred, verify before use.

## Verification

- E2E `tests/seo/webp.spec.ts`: Image widget `src` and every `srcset`
  variant on `/blog/Xperience-by-Kentico-Refresh-December-12-2024` return
  `image/webp`; `/member/avatar/0` (default avatar) returns `image/webp`.
- Hook: `test-id="imageWidgetImage"` on the Image widget `<img>`.
