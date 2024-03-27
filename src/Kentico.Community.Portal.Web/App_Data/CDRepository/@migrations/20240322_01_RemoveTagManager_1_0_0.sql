-- See https://github.com/Kentico/xperience-by-kentico-tag-manager/blob/main/docs/Upgrades.md#102---200

IF OBJECT_ID('KenticoTagManager_ChannelCodeSnippet', 'U') IS NOT NULL
BEGIN
    drop table KenticoTagManager_ChannelCodeSnippet

    delete
    FROM [dbo].[CMS_Class] where ClassName = 'KenticoTagManager.ChannelCodeSnippet'
END