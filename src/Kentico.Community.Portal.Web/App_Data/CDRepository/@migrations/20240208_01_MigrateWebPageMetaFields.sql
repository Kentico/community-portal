-- Copy content from content type fields to commond data fields for
-- website channel content types using the WebPageMeta RFS

-- Check if the required columns exist before attempting migration
IF EXISTS (SELECT *
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'CMS_ContentItemCommonData'
        AND COLUMN_NAME = 'WebpageMetaTitle')
    AND EXISTS (SELECT *
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'CMS_ContentItemCommonData'
        AND COLUMN_NAME = 'WebpageMetaDescription')
BEGIN
    -- Only execute migration if both columns exist

    UPDATE CMS_ContentItemCommonData
    SET 
        WebpageMetaTitle = P.BlogLandingPageTitle, 
        WebpageMetaDescription = P.BlogLandingPageShortDescription
    FROM CMS_ContentItemCommonData CICD
        INNER JOIN KenticoCommunity_BlogLandingPage P
        ON CICD.ContentItemCommonDataID = P.ContentItemDataCommonDataID

    UPDATE CMS_ContentItemCommonData
    SET 
        WebpageMetaTitle = P.LandingPageTitle, 
        WebpageMetaDescription = P.LandingPageShortDescription
    FROM CMS_ContentItemCommonData CICD
        INNER JOIN KenticoCommunity_LandingPage P
        ON CICD.ContentItemCommonDataID = P.ContentItemDataCommonDataID

    UPDATE CMS_ContentItemCommonData
    SET 
        WebpageMetaTitle = P.CommunityLandingPageTitle, 
        WebpageMetaDescription = P.CommunityLandingPageShortDescription
    FROM CMS_ContentItemCommonData CICD
        INNER JOIN KenticoCommunity_CommunityLandingPage P
        ON CICD.ContentItemCommonDataID = P.ContentItemDataCommonDataID

    UPDATE CMS_ContentItemCommonData
    SET 
        WebpageMetaTitle = P.SupportPageTitle, 
        WebpageMetaDescription = P.SupportPageShortDescription
    FROM CMS_ContentItemCommonData CICD
        INNER JOIN KenticoCommunity_SupportPage P
        ON CICD.ContentItemCommonDataID = P.ContentItemDataCommonDataID

    UPDATE CMS_ContentItemCommonData
    SET 
        WebpageMetaTitle = P.ResourceHubPageTitle, 
        WebpageMetaDescription = P.ResourceHubPageShortDescription
    FROM CMS_ContentItemCommonData CICD
        INNER JOIN KenticoCommunity_ResourceHubPage P
        ON CICD.ContentItemCommonDataID = P.ContentItemDataCommonDataID

    UPDATE CMS_ContentItemCommonData
    SET 
        WebpageMetaTitle = P.IntegrationsLandingPageTitle, 
        WebpageMetaDescription = P.IntegrationsLandingPageShortDescription
    FROM CMS_ContentItemCommonData CICD
        INNER JOIN KenticoCommunity_IntegrationsLandingPage P
        ON CICD.ContentItemCommonDataID = P.ContentItemDataCommonDataID

    UPDATE CMS_ContentItemCommonData
    SET 
        WebpageMetaTitle = P.QandALandingPageTitle, 
        WebpageMetaDescription = P.QandALandingPageShortDescription
    FROM CMS_ContentItemCommonData CICD
        INNER JOIN KenticoCommunity_QandALandingPage P
        ON CICD.ContentItemCommonDataID = P.ContentItemDataCommonDataID

    UPDATE CMS_ContentItemCommonData
    SET 
        WebpageMetaTitle = P.QAndANewQuestionPageTitle, 
        WebpageMetaDescription = P.QAndANewQuestionPageShortDescription
    FROM CMS_ContentItemCommonData CICD
        INNER JOIN KenticoCommunity_QAndANewQuestionPage P
        ON CICD.ContentItemCommonDataID = P.ContentItemDataCommonDataID

    PRINT 'WebPageMeta fields migration completed successfully.'
END
ELSE
BEGIN
    PRINT 'WebPageMeta columns (WebpageMetaTitle, WebpageMetaDescription) do not exist in CMS_ContentItemCommonData table. Skipping migration.'
END