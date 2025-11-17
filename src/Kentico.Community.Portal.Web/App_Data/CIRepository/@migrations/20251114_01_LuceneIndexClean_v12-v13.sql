IF OBJECT_ID(N'[dbo].[KenticoLucene_LuceneContentTypeItem]', N'U') IS NOT NULL DROP TABLE [dbo].[KenticoLucene_LuceneContentTypeItem];
IF OBJECT_ID(N'[dbo].[KenticoLucene_LuceneIncludedPathItem]', N'U') IS NOT NULL DROP TABLE [dbo].[KenticoLucene_LuceneIncludedPathItem];
IF OBJECT_ID(N'[dbo].[KenticoLucene_LuceneIndexLanguageItem]', N'U') IS NOT NULL DROP TABLE [dbo].[KenticoLucene_LuceneIndexLanguageItem];
IF OBJECT_ID(N'[dbo].[KenticoLucene_LuceneIndexItem]', N'U') IS NOT NULL DROP TABLE [dbo].[KenticoLucene_LuceneIndexItem];
IF OBJECT_ID(N'[dbo].[KenticoLucene_LuceneIndexAssemblyVersionItem]', N'U') IS NOT NULL DROP TABLE [dbo].[KenticoLucene_LuceneIndexAssemblyVersionItem];
IF OBJECT_ID(N'[dbo].[KenticoLucene_LuceneReusableContentTypeItem]', N'U') IS NOT NULL DROP TABLE [dbo].[KenticoLucene_LuceneReusableContentTypeItem];


delete
FROM [dbo].[CMS_Class] where ClassName like 'KenticoLucene%'

delete
from [CMS_Resource] where ResourceName = 'CMS.Integration.Lucene'