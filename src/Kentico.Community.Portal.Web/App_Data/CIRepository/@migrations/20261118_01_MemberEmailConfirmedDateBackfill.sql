-- Migration: Backfill CMS_Member.MemberEmailConfirmedDate for existing members
-- Purpose: One-time set of the email confirmed date for members who existed before the column.
--          Enabled members confirmed their email (Xperience treats EmailConfirmed as MemberEnabled):
--          the date becomes MemberCreated, or now if that's missing. Disabled members stay NULL - a
--          member who never confirmed and one an administrator disabled can't be told apart.
--          See docs/adr/0005-separate-email-confirmation-from-enabled.md.
-- Runs after the CD/CI restore (After.txt): the restore adds the column.
-- Safe to run more than once: only updates rows where the date is still NULL.

UPDATE [dbo].[CMS_Member]
SET [MemberEmailConfirmedDate] = COALESCE([MemberCreated], GETDATE())
WHERE [MemberEnabled] = 1
    AND [MemberEmailConfirmedDate] IS NULL
