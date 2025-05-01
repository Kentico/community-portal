-- Switch page templates for QA/UAT environments, PROD environment has already been migrated
UPDATE CD
SET CD.ContentItemCommonDataVisualBuilderTemplateConfiguration = '{"identifier":"KenticoCommunity.BlogPostPage_Components","properties":{},"fieldIdentifiers":{}}'
FROM CMS_ContentItemCommonData CD
WHERE CD.ContentItemCommonDataVisualBuilderTemplateConfiguration LIKE '%KenticoCommunity.BlogPostPage_Default%'