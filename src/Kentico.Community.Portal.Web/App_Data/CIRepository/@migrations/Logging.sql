update dbo.CMS_SettingsKey
set KeyValue = 'True'
where KeyName in ('CMSAnalyticsEnabled', 'CMSEnableOnlineMarketing', 'CMSContentPersonalizationEnabled')