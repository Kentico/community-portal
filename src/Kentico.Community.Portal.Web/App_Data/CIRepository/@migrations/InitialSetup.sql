update dbo.CMS_SettingsKey
set KeyValue = '/home'
where KeyName = 'CMSHomePagePath'

update dbo.CMS_SettingsKey
set KeyValue = 'Kentico DevNet'
where KeyName = 'CMSPageTitlePrefix'