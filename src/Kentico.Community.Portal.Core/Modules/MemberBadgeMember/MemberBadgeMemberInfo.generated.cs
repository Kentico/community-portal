using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(MemberBadgeMemberInfo), MemberBadgeMemberInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="MemberBadgeMemberInfo"/>.
    /// </summary>
    [InfoCache(InfoCacheBy.ID)]
    public partial class MemberBadgeMemberInfo : AbstractInfo<MemberBadgeMemberInfo, IInfoProvider<MemberBadgeMemberInfo>>, IInfoWithId, IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.memberbadgemember";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<MemberBadgeMemberInfo>), OBJECT_TYPE, "KenticoCommunity.MemberBadgeMember", "MemberBadgeMemberID", "MemberBadgeMemberLastModified", "MemberBadgeMemberGUID", null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new ObjectDependency("MemberBadgeMemberMemberId", "cms.member", ObjectDependencyEnum.Required),
                new ObjectDependency("MemberBadgeMemberMemberBadgeId", "kenticocommunity.memberbadge", ObjectDependencyEnum.Required),
            },
        };


        /// <summary>
        /// Member badge member ID.
        /// </summary>
        [DatabaseField]
        public virtual int MemberBadgeMemberID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(MemberBadgeMemberID)), 0);
            set => SetValue(nameof(MemberBadgeMemberID), value);
        }


        /// <summary>
        /// Member badge member GUID.
        /// </summary>
        [DatabaseField]
        public virtual Guid MemberBadgeMemberGUID
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(MemberBadgeMemberGUID)), Guid.Empty);
            set => SetValue(nameof(MemberBadgeMemberGUID), value);
        }


        /// <summary>
        /// Member badge member member id.
        /// </summary>
        [DatabaseField]
        public virtual int MemberBadgeMemberMemberId
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(MemberBadgeMemberMemberId)), 0);
            set => SetValue(nameof(MemberBadgeMemberMemberId), value);
        }


        /// <summary>
        /// Member badge member created date.
        /// </summary>
        [DatabaseField]
        public virtual DateTime MemberBadgeMemberCreatedDate
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(MemberBadgeMemberCreatedDate)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(MemberBadgeMemberCreatedDate), value);
        }


        /// <summary>
        /// Member badge member last modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime MemberBadgeMemberLastModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(MemberBadgeMemberLastModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(MemberBadgeMemberLastModified), value);
        }


        /// <summary>
        /// Member badge member member badge id.
        /// </summary>
        [DatabaseField]
        public virtual int MemberBadgeMemberMemberBadgeId
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(MemberBadgeMemberMemberBadgeId)), 0);
            set => SetValue(nameof(MemberBadgeMemberMemberBadgeId), value);
        }


        /// <summary>
        /// Member badge member is selected.
        /// </summary>
        [DatabaseField]
        public virtual bool MemberBadgeMemberIsSelected
        {
            get => ValidationHelper.GetBoolean(GetValue(nameof(MemberBadgeMemberIsSelected)), false);
            set => SetValue(nameof(MemberBadgeMemberIsSelected), value);
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
        /// Creates an empty instance of the <see cref="MemberBadgeMemberInfo"/> class.
        /// </summary>
        public MemberBadgeMemberInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="MemberBadgeMemberInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public MemberBadgeMemberInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
