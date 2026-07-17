-- Migration: Fix malformed EmailFrom on queued emails
-- Purpose: Correct emails where EmailFrom is missing the opening quote,
--          caused by a formatting bug now fixed in MemberEmailService.

UPDATE [dbo].[CMS_Email]
SET [EmailFrom] = '"' + [EmailFrom]
WHERE [EmailStatus] = 1
    AND [EmailFrom] LIKE 'Kentico Community" <%>'
    AND [EmailFrom] NOT LIKE '"%'
