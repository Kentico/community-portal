-- Updates all blog post content items to transfer their content item specific field values to the reusable field schema fields

DECLARE @JsonString NVARCHAR(MAX);
DECLARE @Sql NVARCHAR(MAX) = '';
DECLARE @ColumnsToUpdate NVARCHAR(MAX) = '';
DECLARE @ColumnsExist BIT = 0;

-- Check for the existence of the columns in KenticoCommunity_BlogPostContent
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'KenticoCommunity_BlogPostContent' 
           AND COLUMN_NAME = 'BlogPostContentTitle')
BEGIN
    SET @ColumnsToUpdate = @ColumnsToUpdate + 'CD.ListableItemTitle = KB.BlogPostContentTitle, ';
    SET @ColumnsExist = 1;
END

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'KenticoCommunity_BlogPostContent' 
           AND COLUMN_NAME = 'BlogPostContentShortDescription')
BEGIN
    SET @ColumnsToUpdate = @ColumnsToUpdate + 'CD.ListableItemShortDescription = KB.BlogPostContentShortDescription, ';
    SET @ColumnsExist = 1;
END

-- The ListableItemFeaturedImage is always updated, so we include it directly
SET @ColumnsToUpdate = @ColumnsToUpdate + 'CD.ListableItemFeaturedImage = @JsonString';

-- Check if there are columns to update
IF (@ColumnsExist = 1)
BEGIN
    -- Set the JSON string
    SELECT @JsonString = N'[{"Identifier":"' + LOWER(ContentItemGUID) + N'"}]'
    FROM CMS_ContentItem
    WHERE ContentItemName LIKE 'KenticoBackgroundBanner_Shapes%';

    -- Build the dynamic SQL
    SET @Sql = '
    UPDATE CD
    SET ' + @ColumnsToUpdate + '
    FROM CMS_ContentItemCommonData CD
    INNER JOIN KenticoCommunity_BlogPostContent as KB
        ON CD.ContentItemCommonDataID = KB.ContentItemDataCommonDataID
    WHERE CD.ListableItemTitle IS NULL 
      AND CD.ListableItemShortDescription IS NULL 
      AND CD.ListableItemFeaturedImage IS NULL;';

    -- Execute the dynamic SQL
    EXEC sp_executesql @Sql, N'@JsonString NVARCHAR(MAX)', @JsonString;
END
ELSE
BEGIN
    PRINT 'No valid blog post columns found to update.';
END