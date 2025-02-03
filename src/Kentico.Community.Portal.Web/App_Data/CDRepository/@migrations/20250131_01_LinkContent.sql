UPDATE LC
SET LC.LinkContentLinkType = 'CommunityContribution', LC.LinkContentPublishedDate = CD.ContentItemCommonDataFirstPublishedWhen
FROM KenticoCommunity_LinkContent LC
INNER JOIN CMS_ContentItemCommonData CD
    ON CD.ContentItemCommonDataID = LC.ContentItemDataCommonDataID
INNER JOIN CMS_ContentItem CI
    ON CI.ContentItemID = CD.ContentItemCommonDataContentItemID
INNER JOIN CMS_ContentFolder CF
    ON CI.ContentItemContentFolderID = CF.ContentFolderID
INNER JOIN CMS_Class CL
    ON CI.ContentItemContentTypeID = CL.ClassID
WHERE CF.ContentFolderName = 'Newsletters-9h8zieu4'
  AND CL.ClassName = 'KenticoCommunity.LinkContent';