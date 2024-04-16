-- Populates the new KenticoCommunity_QAndAAnswerData.QAndAAnswerDataWebsiteChannelID column
-- which is used to more easily connect the record to the channel it was created for

UPDATE KenticoCommunity_QAndAAnswerData
SET QAndAAnswerDataWebsiteChannelID = (
    SELECT W.WebsiteChannelID
    FROM CMS_Channel C
    INNER JOIN CMS_WebsiteChannel W
        ON C.ChannelID = W.WebsiteChannelChannelID
    WHERE C.ChannelName = 'devnet')

