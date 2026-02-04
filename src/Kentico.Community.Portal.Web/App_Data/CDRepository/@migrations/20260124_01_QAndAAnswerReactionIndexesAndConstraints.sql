-- Migration: Add unique constraint and indexes for QAndAAnswerReaction
-- Purpose: Ensure no duplicate reactions per member/answer/type and optimize query performance

-- Create unique constraint to prevent duplicate reactions
-- A member can only have one reaction of each type per answer
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
    WHERE CONSTRAINT_NAME = 'UQ_QAndAAnswerReaction_MemberAnswerType'
    AND TABLE_NAME = 'KenticoCommunity_QAndAAnswerReaction'
)
BEGIN
    ALTER TABLE [KenticoCommunity_QAndAAnswerReaction]
    ADD CONSTRAINT [UQ_QAndAAnswerReaction_MemberAnswerType] 
    UNIQUE ([QAndAAnswerReactionMemberID], [QAndAAnswerReactionAnswerID], [QAndAAnswerReactionType])
END
GO

-- Create index on AnswerID for filtering reactions by answer
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_QAndAAnswerReaction_AnswerID'
    AND object_id = OBJECT_ID('KenticoCommunity_QAndAAnswerReaction')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_QAndAAnswerReaction_AnswerID]
    ON [KenticoCommunity_QAndAAnswerReaction] ([QAndAAnswerReactionAnswerID])
    INCLUDE ([QAndAAnswerReactionMemberID], [QAndAAnswerReactionType], [QAndAAnswerReactionDateModified])
END
GO

-- Create index on MemberID for filtering reactions by member
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_QAndAAnswerReaction_MemberID'
    AND object_id = OBJECT_ID('KenticoCommunity_QAndAAnswerReaction')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_QAndAAnswerReaction_MemberID]
    ON [KenticoCommunity_QAndAAnswerReaction] ([QAndAAnswerReactionMemberID])
    INCLUDE ([QAndAAnswerReactionAnswerID], [QAndAAnswerReactionType], [QAndAAnswerReactionDateModified])
END
GO
