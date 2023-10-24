DECLARE @DocumentID int;
DECLARE @NodeID int;
DECLARE @PageID int;

-- Delete the Support page
SELECT @DocumentID = t.DocumentID
    , @NodeID = t.NodeID
    , @PageID = p.PageID
FROM View_CMS_Tree_Joined t
INNER JOIN Kentico_Page p
    on t.DocumentForeignKeyValue = p.PageID
WHERE t.NodeAlias = 'Support' AND t.ClassName = 'Kentico.Page'

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
