-- Migration: Add unique constraint and indexes for QAndAQuestionReaction
-- Purpose: Ensure no duplicate reactions per member/question/type and optimize query performance

-- Create unique constraint to prevent duplicate reactions
-- A member can only have one reaction of each type per question
IF NOT EXISTS (
    SELECT 1
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
WHERE CONSTRAINT_NAME = 'UQ_QAndAQuestionReaction_MemberQuestionType'
    AND TABLE_NAME = 'KenticoCommunity_QAndAQuestionReaction'
)
BEGIN
    ALTER TABLE [KenticoCommunity_QAndAQuestionReaction]
    ADD CONSTRAINT [UQ_QAndAQuestionReaction_MemberQuestionType] 
    UNIQUE ([QAndAQuestionReactionMemberID], [QAndAQuestionReactionWebPageItemID], [QAndAQuestionReactionType])
END
GO

-- Create index on WebPageItemID for filtering reactions by question
IF NOT EXISTS (
    SELECT 1
FROM sys.indexes
WHERE name = 'IX_QAndAQuestionReaction_WebPageItemID'
    AND object_id = OBJECT_ID('KenticoCommunity_QAndAQuestionReaction')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_QAndAQuestionReaction_WebPageItemID]
    ON [KenticoCommunity_QAndAQuestionReaction] ([QAndAQuestionReactionWebPageItemID])
    INCLUDE ([QAndAQuestionReactionMemberID], [QAndAQuestionReactionType], [QAndAQuestionReactionDateModified])
END
GO

-- Create index on MemberID for filtering reactions by member
IF NOT EXISTS (
    SELECT 1
FROM sys.indexes
WHERE name = 'IX_QAndAQuestionReaction_MemberID'
    AND object_id = OBJECT_ID('KenticoCommunity_QAndAQuestionReaction')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_QAndAQuestionReaction_MemberID]
    ON [KenticoCommunity_QAndAQuestionReaction] ([QAndAQuestionReactionMemberID])
    INCLUDE ([QAndAQuestionReactionWebPageItemID], [QAndAQuestionReactionType], [QAndAQuestionReactionDateModified])
END
GO
