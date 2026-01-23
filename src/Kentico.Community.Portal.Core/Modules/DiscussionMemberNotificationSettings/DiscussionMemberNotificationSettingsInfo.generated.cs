using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(DiscussionMemberNotificationSettingsInfo), DiscussionMemberNotificationSettingsInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="DiscussionMemberNotificationSettingsInfo"/>.
    /// </summary>
    public partial class DiscussionMemberNotificationSettingsInfo : AbstractInfo<DiscussionMemberNotificationSettingsInfo, IInfoProvider<DiscussionMemberNotificationSettingsInfo>>, IInfoWithId
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.discussionmembernotificationsettings";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<DiscussionMemberNotificationSettingsInfo>), OBJECT_TYPE, "KenticoCommunity.DiscussionMemberNotificationSettings", "DiscussionMemberNotificationSettingsID", "DiscussionMemberNotificationSettingsDateModified", null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new ObjectDependency("DiscussionMemberNotificationSettingsMemberID", "cms.member", ObjectDependencyEnum.Required),
                new ObjectDependency("DiscussionMemberNotificationSettingsLatestDiscussionEventID", "kenticocommunity.discussionevent", ObjectDependencyEnum.Required),
            },
        };


        /// <summary>
        /// Discussion member notification settings ID.
        /// </summary>
        [DatabaseField]
        public virtual int DiscussionMemberNotificationSettingsID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(DiscussionMemberNotificationSettingsID)), 0);
            set => SetValue(nameof(DiscussionMemberNotificationSettingsID), value);
        }


        /// <summary>
        /// Discussion member notification settings date created.
        /// </summary>
        [DatabaseField]
        public virtual DateTime DiscussionMemberNotificationSettingsDateCreated
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(DiscussionMemberNotificationSettingsDateCreated)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(DiscussionMemberNotificationSettingsDateCreated), value);
        }


        /// <summary>
        /// Discussion member notification settings date modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime DiscussionMemberNotificationSettingsDateModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(DiscussionMemberNotificationSettingsDateModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(DiscussionMemberNotificationSettingsDateModified), value);
        }


        /// <summary>
        /// Discussion member notification settings member ID.
        /// </summary>
        [DatabaseField]
        public virtual int DiscussionMemberNotificationSettingsMemberID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(DiscussionMemberNotificationSettingsMemberID)), 0);
            set => SetValue(nameof(DiscussionMemberNotificationSettingsMemberID), value);
        }


        /// <summary>
        /// Discussion member notification settings frequency type.
        /// </summary>
        [DatabaseField]
        public virtual string DiscussionMemberNotificationSettingsFrequencyType
        {
            get => ValidationHelper.GetString(GetValue(nameof(DiscussionMemberNotificationSettingsFrequencyType)), "Weekly");
            set => SetValue(nameof(DiscussionMemberNotificationSettingsFrequencyType), value);
        }


        /// <summary>
        /// Discussion member notification settings latest discussion event ID.
        /// </summary>
        [DatabaseField]
        public virtual int DiscussionMemberNotificationSettingsLatestDiscussionEventID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(DiscussionMemberNotificationSettingsLatestDiscussionEventID)), 0);
            set => SetValue(nameof(DiscussionMemberNotificationSettingsLatestDiscussionEventID), value);
        }


        /// <summary>
        /// Discussion member notification settings auto subscribe enabled.
        /// </summary>
        [DatabaseField]
        public virtual bool DiscussionMemberNotificationSettingsAutoSubscribeEnabled
        {
            get => ValidationHelper.GetBoolean(GetValue(nameof(DiscussionMemberNotificationSettingsAutoSubscribeEnabled)), false);
            set => SetValue(nameof(DiscussionMemberNotificationSettingsAutoSubscribeEnabled), value);
        }


        /// <summary>
        /// Deletes the object using appropriate provider.
        /// </summary>
        protected override void DeleteObject()
        {
            Provider.Delete(this);
        }


        /// <summary>
        /// Updates the object using appropriate provider.
        /// </summary>
        protected override void SetObject()
        {
            Provider.Set(this);
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="DiscussionMemberNotificationSettingsInfo"/> class.
        /// </summary>
        public DiscussionMemberNotificationSettingsInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="DiscussionMemberNotificationSettingsInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public DiscussionMemberNotificationSettingsInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
