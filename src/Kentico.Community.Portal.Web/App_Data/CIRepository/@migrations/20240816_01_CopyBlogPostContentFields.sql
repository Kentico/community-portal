-- Updates all blog post content items to transfer their content item specific field values to the reusable field schema fields

DECLARE @JsonString NVARCHAR(MAX);

SELECT @JsonString = N'[{"Identifier":"' + LOWER(ContentItemGUID) + N'"}]'
FROM CMS_ContentItem
WHERE ContentItemName LIKE 'KenticoBackgroundBanner_Shapes%'

UPDATE CD
SET 
    CD.ListableItemTitle = KB.BlogPostContentTitle,
    CD.ListableItemShortDescription = KB.BlogPostContentShortDescription,
    CD.ListableItemFeaturedImage = @JsonString
FROM CMS_ContentItemCommonData CD
INNER JOIN KenticoCommunity_BlogPostContent as KB
    ON CD.ContentItemCommonDataID = KB.ContentItemDataCommonDataID
WHERE CD.ListableItemTitle IS NULL AND CD.ListableItemShortDescription IS NULL AND CD.ListableItemFeaturedImage IS NULL