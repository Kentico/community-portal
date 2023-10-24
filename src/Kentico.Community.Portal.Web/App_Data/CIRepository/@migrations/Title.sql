update dbo.CMS_SettingsKey
set KeyValue = 'Kentico DevNet'
where KeyName = 'CMSPageTitlePrefix'

update dbo.CMS_SettingsKey
set KeyValue = '{%pagetitle_orelse_name%} | {%prefix%}'
where KeyName = 'CMSPageTitleFormat'
