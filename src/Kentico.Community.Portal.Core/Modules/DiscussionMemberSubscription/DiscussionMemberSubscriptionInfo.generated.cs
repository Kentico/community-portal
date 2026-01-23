using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(DiscussionMemberSubscriptionInfo), DiscussionMemberSubscriptionInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="DiscussionMemberSubscriptionInfo"/>.
    /// </summary>
    public partial class DiscussionMemberSubscriptionInfo : AbstractInfo<DiscussionMemberSubscriptionInfo, IInfoProvider<DiscussionMemberSubscriptionInfo>>, IInfoWithId
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.discussionmembersubscription";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<DiscussionMemberSubscriptionInfo>), OBJECT_TYPE, "KenticoCommunity.DiscussionMemberSubscription", "DiscussionMemberSubscriptionID", "DiscussionMemberSubscriptionDateModified", null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new ObjectDependency("DiscussionMemberSubscriptionMemberID", "cms.member", ObjectDependencyEnum.RequiredHasDefault),
            },
        };


        /// <summary>
        /// Discussion member subscription ID.
        /// </summary>
        [DatabaseField]
        public virtual int DiscussionMemberSubscriptionID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(DiscussionMemberSubscriptionID)), 0);
            set => SetValue(nameof(DiscussionMemberSubscriptionID), value);
        }


        /// <summary>
        /// Discussion member subscription member ID.
        /// </summary>
        [DatabaseField]
        public virtual int DiscussionMemberSubscriptionMemberID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(DiscussionMemberSubscriptionMemberID)), 0);
            set => SetValue(nameof(DiscussionMemberSubscriptionMemberID), value);
        }


        /// <summary>
        /// Discussion member subscription web page item ID.
        /// </summary>
        [DatabaseField]
        public virtual int DiscussionMemberSubscriptionWebPageItemID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(DiscussionMemberSubscriptionWebPageItemID)), 0);
            set => SetValue(nameof(DiscussionMemberSubscriptionWebPageItemID), value);
        }


        /// <summary>
        /// Discussion member subscription date modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime DiscussionMemberSubscriptionDateModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(DiscussionMemberSubscriptionDateModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(DiscussionMemberSubscriptionDateModified), value);
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
        /// Creates an empty instance of the <see cref="DiscussionMemberSubscriptionInfo"/> class.
        /// </summary>
        public DiscussionMemberSubscriptionInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="DiscussionMemberSubscriptionInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public DiscussionMemberSubscriptionInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
