using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(DiscussionEventInfo), DiscussionEventInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="DiscussionEventInfo"/>.
    /// </summary>
    public partial class DiscussionEventInfo : AbstractInfo<DiscussionEventInfo, IInfoProvider<DiscussionEventInfo>>, IInfoWithId
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.discussionevent";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<DiscussionEventInfo>), OBJECT_TYPE, "KenticoCommunity.DiscussionEvent", "DiscussionEventID", "DiscussionEventDateModified", null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new ObjectDependency("DiscussionEventWebsiteChannelID", "cms.websitechannel", ObjectDependencyEnum.Required),
                new ObjectDependency("DiscussionEventFromMemberID", "cms.member", ObjectDependencyEnum.RequiredHasDefault),
            },
        };


        /// <summary>
        /// Discussion event ID.
        /// </summary>
        [DatabaseField]
        public virtual int DiscussionEventID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(DiscussionEventID)), 0);
            set => SetValue(nameof(DiscussionEventID), value);
        }


        /// <summary>
        /// Discussion event type.
        /// </summary>
        [DatabaseField]
        public virtual string DiscussionEventType
        {
            get => ValidationHelper.GetString(GetValue(nameof(DiscussionEventType)), String.Empty);
            set => SetValue(nameof(DiscussionEventType), value);
        }


        /// <summary>
        /// Discussion event date modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime DiscussionEventDateModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(DiscussionEventDateModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(DiscussionEventDateModified), value);
        }


        /// <summary>
        /// Discussion event web page item ID.
        /// </summary>
        [DatabaseField]
        public virtual int DiscussionEventWebPageItemID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(DiscussionEventWebPageItemID)), 0);
            set => SetValue(nameof(DiscussionEventWebPageItemID), value);
        }


        /// <summary>
        /// Discussion event website channel ID.
        /// </summary>
        [DatabaseField]
        public virtual int DiscussionEventWebsiteChannelID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(DiscussionEventWebsiteChannelID)), 0);
            set => SetValue(nameof(DiscussionEventWebsiteChannelID), value);
        }


        /// <summary>
        /// Discussion event from member ID.
        /// </summary>
        [DatabaseField]
        public virtual int DiscussionEventFromMemberID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(DiscussionEventFromMemberID)), 0);
            set => SetValue(nameof(DiscussionEventFromMemberID), value);
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
        /// Creates an empty instance of the <see cref="DiscussionEventInfo"/> class.
        /// </summary>
        public DiscussionEventInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="DiscussionEventInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public DiscussionEventInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
