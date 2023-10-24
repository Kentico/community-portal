DECLARE @DocumentID int;
DECLARE @NodeID int;
DECLARE @PageID int;

-- Delete the Cookies policy page
SELECT @DocumentID = t.DocumentID
    , @NodeID = t.NodeID
    , @PageID = p.PageID
FROM View_CMS_Tree_Joined t
INNER JOIN Kentico_Page p
    on t.DocumentForeignKeyValue = p.PageID
WHERE t.DocumentName = 'Cookies policy' and t.ClassName = 'Kentico.Page'

DELETE FROM kentico_page
WHERE PageID = @PageID;

DELETE FROM CMS_Document
WHERE DocumentID = @DocumentID

DELETE FROM CMS_PageUrlPath
WHERE PageUrlPathNodeID = @NodeID

DELETE FROM CMS_Tree
WHERE NodeID = @NodeID

DELETE FROM CMS_WorkflowHistory
WHERE VersionHistoryID IN (
    SELECT VersionHistoryID
    FROM CMS_VersionHistory
    WHERE DocumentID = @DocumentID
)

DELETE FROM CMS_VersionHistory
WHERE DocumentID = @DocumentID

-- Delete the Privacy page
SELECT @DocumentID = t.DocumentID
    , @NodeID = t.NodeID
    , @PageID = p.PageID
FROM View_CMS_Tree_Joined t
INNER JOIN Kentico_Page p
    on t.DocumentForeignKeyValue = p.PageID
WHERE t.DocumentName = 'Privacy' and t.ClassName = 'Kentico.Page'

DELETE FROM kentico_page
WHERE PageID = @PageID;

DELETE FROM CMS_Document
WHERE DocumentID = @DocumentID

DELETE FROM CMS_PageUrlPath
WHERE PageUrlPathNodeID = @NodeID

DELETE FROM CMS_Tree
WHERE NodeID = @NodeID

DELETE FROM CMS_WorkflowHistory
WHERE VersionHistoryID IN (
    SELECT VersionHistoryID
    FROM CMS_VersionHistory
    WHERE DocumentID = @DocumentID
)

DELETE FROM CMS_VersionHistory
WHERE DocumentID = @DocumentID

-- Delete the child documentation page
SELECT @DocumentID = t.DocumentID
    , @NodeID = t.NodeID
    , @PageID = p.PageID
FROM View_CMS_Tree_Joined t
INNER JOIN Kentico_Page p
    on t.DocumentForeignKeyValue = p.PageID
WHERE t.NodeAliasPath = '/Documentation/Documentation' and t.ClassName = 'Kentico.Page'

DELETE FROM kentico_page
WHERE PageID = @PageID;

DELETE FROM CMS_Document
WHERE DocumentID = @DocumentID

DELETE FROM CMS_PageUrlPath
WHERE PageUrlPathNodeID = @NodeID

DELETE FROM CMS_Tree
WHERE NodeID = @NodeID

DELETE FROM CMS_WorkflowHistory
WHERE VersionHistoryID IN (
    SELECT VersionHistoryID
    FROM CMS_VersionHistory
    WHERE DocumentID = @DocumentID
)

DELETE FROM CMS_VersionHistory
WHERE DocumentID = @DocumentID

-- Delete the child licenses page
SELECT @DocumentID = t.DocumentID
    , @NodeID = t.NodeID
    , @PageID = p.PageID
FROM View_CMS_Tree_Joined t
INNER JOIN Kentico_Page p
    on t.DocumentForeignKeyValue = p.PageID
WHERE t.DocumentName = 'Third party software licenses' and t.ClassName = 'Kentico.Page'

DELETE FROM kentico_page
WHERE PageID = @PageID;

DELETE FROM CMS_Document
WHERE DocumentID = @DocumentID

DELETE FROM CMS_PageUrlPath
WHERE PageUrlPathNodeID = @NodeID

DELETE FROM CMS_Tree
WHERE NodeID = @NodeID

DELETE FROM CMS_WorkflowHistory
WHERE VersionHistoryID IN (
    SELECT VersionHistoryID
    FROM CMS_VersionHistory
    WHERE DocumentID = @DocumentID
)

DELETE FROM CMS_VersionHistory
WHERE DocumentID = @DocumentID

-- Delete the other 3rd party licenses page

 SELECT @DocumentID = t.DocumentID
     , @NodeID = t.NodeID
     , @PageID = p.PageID
 FROM View_CMS_Tree_Joined t
 INNER JOIN Kentico_Page p
     on t.DocumentForeignKeyValue = p.PageID
 WHERE t.DocumentName = '3rd party licenses' and t.ClassName = 'Kentico.Page'
 
 DELETE FROM kentico_page
 WHERE PageID = @PageID;
 
 DELETE FROM CMS_Document
 WHERE DocumentID = @DocumentID
 
 DELETE FROM CMS_PageUrlPath
 WHERE PageUrlPathNodeID = @NodeID
 
 DELETE FROM CMS_Tree
 WHERE NodeID = @NodeID
 
 DELETE FROM CMS_WorkflowHistory
 WHERE VersionHistoryID IN (
     SELECT VersionHistoryID
     FROM CMS_VersionHistory
     WHERE DocumentID = @DocumentID
 )
 
 DELETE FROM CMS_VersionHistory
 WHERE DocumentID = @DocumentID

-- Delete the parent Documentation page
SELECT @DocumentID = t.DocumentID
    , @NodeID = t.NodeID
    , @PageID = p.PageID
FROM View_CMS_Tree_Joined t
INNER JOIN Kentico_Page p
    on t.DocumentForeignKeyValue = p.PageID
WHERE t.NodeAliasPath = '/Documentation' and t.ClassName = 'Kentico.Page'

DELETE FROM kentico_page
WHERE PageID = @PageID;

DELETE FROM CMS_Document
WHERE DocumentID = @DocumentID

DELETE FROM CMS_PageUrlPath
WHERE PageUrlPathNodeID = @NodeID

DELETE FROM CMS_Tree
WHERE NodeID = @NodeID

DELETE FROM CMS_WorkflowHistory
WHERE VersionHistoryID IN (
    SELECT VersionHistoryID
    FROM CMS_VersionHistory
    WHERE DocumentID = @DocumentID
)

DELETE FROM CMS_VersionHistory
WHERE DocumentID = @DocumentID