using System;
using System.Data;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(MemberBadgeInfo), MemberBadgeInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="MemberBadgeInfo"/>.
    /// </summary>
    [Serializable, InfoCache(InfoCacheBy.ID)]
    public partial class MemberBadgeInfo : AbstractInfo<MemberBadgeInfo, IInfoProvider<MemberBadgeInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.memberbadge";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<MemberBadgeInfo>), OBJECT_TYPE, "KenticoCommunity.MemberBadge", "MemberBadgeID", "MemberBadgeLastModified", "MemberBadgeGUID", "MemberBadgeCodeName", "MemberBadgeDisplayName", null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
            },
            ContinuousIntegrationSettings =
            {
                Enabled = true
            },
        };


        /// <summary>
        /// Member badge ID.
        /// </summary>
        [DatabaseField]
        public virtual int MemberBadgeID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(MemberBadgeID)), 0);
            set => SetValue(nameof(MemberBadgeID), value);
        }


        /// <summary>
        /// Member badge GUID.
        /// </summary>
        [DatabaseField]
        public virtual Guid MemberBadgeGUID
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(MemberBadgeGUID)), Guid.Empty);
            set => SetValue(nameof(MemberBadgeGUID), value);
        }


        /// <summary>
        /// Member badge display name.
        /// </summary>
        [DatabaseField]
        public virtual string MemberBadgeDisplayName
        {
            get => ValidationHelper.GetString(GetValue(nameof(MemberBadgeDisplayName)), String.Empty);
            set => SetValue(nameof(MemberBadgeDisplayName), value);
        }


        /// <summary>
        /// Member badge code name.
        /// </summary>
        [DatabaseField]
        public virtual string MemberBadgeCodeName
        {
            get => ValidationHelper.GetString(GetValue(nameof(MemberBadgeCodeName)), String.Empty);
            set => SetValue(nameof(MemberBadgeCodeName), value);
        }


        /// <summary>
        /// Member badge is rule assigned.
        /// </summary>
        [DatabaseField]
        public virtual bool MemberBadgeIsRuleAssigned
        {
            get => ValidationHelper.GetBoolean(GetValue(nameof(MemberBadgeIsRuleAssigned)), false);
            set => SetValue(nameof(MemberBadgeIsRuleAssigned), value);
        }


        /// <summary>
        /// Member badge short description.
        /// </summary>
        [DatabaseField]
        public virtual string MemberBadgeShortDescription
        {
            get => ValidationHelper.GetString(GetValue(nameof(MemberBadgeShortDescription)), String.Empty);
            set => SetValue(nameof(MemberBadgeShortDescription), value);
        }


        /// <summary>
        /// Member badge date created.
        /// </summary>
        [DatabaseField]
        public virtual DateTime MemberBadgeDateCreated
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(MemberBadgeDateCreated)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(MemberBadgeDateCreated), value);
        }


        /// <summary>
        /// Member badge date modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime MemberBadgeDateModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(MemberBadgeDateModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(MemberBadgeDateModified), value);
        }


        /// <summary>
        /// Member badge image content.
        /// </summary>
        [DatabaseField(ValueType = typeof(string))]
        public IEnumerable<global::CMS.ContentEngine.ContentItemReference> MemberBadgeImageContent
        {
            get => global::CMS.DataEngine.Internal.JsonDataTypeConverter.ConvertToModels<global::CMS.ContentEngine.ContentItemReference>(GetValue(nameof(MemberBadgeImageContent), String.Empty));
            set => SetValue(nameof(MemberBadgeImageContent), global::CMS.DataEngine.Internal.JsonDataTypeConverter.ConvertToString<IEnumerable<global::CMS.ContentEngine.ContentItemReference>>(value, Enumerable.Empty<global::CMS.ContentEngine.ContentItemReference>(), null));
        }


        /// <summary>
        /// Member badge is enabled for rule assignment.
        /// </summary>
        [DatabaseField]
        public virtual bool MemberBadgeIsEnabledForRuleAssignment
        {
            get => ValidationHelper.GetBoolean(GetValue(nameof(MemberBadgeIsEnabledForRuleAssignment)), true);
            set => SetValue(nameof(MemberBadgeIsEnabledForRuleAssignment), value);
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
        /// Constructor for de-serialization.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected MemberBadgeInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="MemberBadgeInfo"/> class.
        /// </summary>
        public MemberBadgeInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="MemberBadgeInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public MemberBadgeInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
