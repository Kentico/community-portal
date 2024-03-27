-- Adds taxonomy to all Q&A post pages through page title conventions

-- 0a81201d-8daa-4a54-bcc1-320914635b8f is the Blog tag in the QAndADiscussionType taxonomy
-- c50e7dd3-2b8e-47b5-96ee-3f04ccfde8b6 is the Question tag in the QAndADiscussionType taxonomy

UPDATE QP
SET QP.QAndAQuestionPageDiscussionType = '[{"Identifier":"0a81201d-8daa-4a54-bcc1-320914635b8f"}]'
FROM KenticoCommunity_QAndAQuestionPage QP
WHERE QP.QAndAQuestionPageTitle LIKE 'Blog Discussion:%'

UPDATE QP
SET QP.QAndAQuestionPageDiscussionType = '[{"Identifier":"c50e7dd3-2b8e-47b5-96ee-3f04ccfde8b6"}]'
FROM KenticoCommunity_QAndAQuestionPage QP
WHERE QP.QAndAQuestionPageTitle NOT LIKE 'Blog Discussion:%'