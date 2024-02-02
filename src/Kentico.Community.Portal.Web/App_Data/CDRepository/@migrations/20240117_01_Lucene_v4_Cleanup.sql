-- The previous version of Lucene (3.0.0) didn't follow naming conventions for objects/tables Kentico integrations
-- This removes the old tables and class configuration since we don't have migrations for NuGet packages

IF OBJECT_ID('Lucene_LuceneContentTypeItem', 'U') IS NOT NULL
BEGIN
    DROP TABLE Lucene_LuceneContentTypeItem
    DROP TABLE Lucene_LuceneIncludedPathItem
    DROP TABLE Lucene_LuceneIndexLanguageItem
    DROP TABLE Lucene_LuceneIndexItem

    DELETE
    FROM [dbo].[CMS_Class] where ClassName like 'lucene%'
END