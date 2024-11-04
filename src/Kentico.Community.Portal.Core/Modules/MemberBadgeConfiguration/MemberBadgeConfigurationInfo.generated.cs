using System;
using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(MemberBadgeConfigurationInfo), MemberBadgeConfigurationInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="MemberBadgeConfigurationInfo"/>.
    /// </summary>
    public partial class MemberBadgeConfigurationInfo : AbstractInfo<MemberBadgeConfigurationInfo, IInfoProvider<MemberBadgeConfigurationInfo>>, IInfoWithId, IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.memberbadgeconfiguration";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<MemberBadgeConfigurationInfo>), OBJECT_TYPE, "KenticoCommunity.MemberBadgeConfiguration", "MemberBadgeConfigurationID", "MemberBadgeConfigurationLastModified", "MemberBadgeConfigurationGUID", null, null, null, null, null)
        {
            TouchCacheDependencies = true,
        };


        /// <summary>
        /// Member badge configuration ID.
        /// </summary>
        [DatabaseField]
        public virtual int MemberBadgeConfigurationID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(MemberBadgeConfigurationID)), 0);
            set => SetValue(nameof(MemberBadgeConfigurationID), value);
        }


        /// <summary>
        /// Member badge configuration GUID.
        /// </summary>
        [DatabaseField]
        public virtual Guid MemberBadgeConfigurationGUID
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(MemberBadgeConfigurationGUID)), Guid.Empty);
            set => SetValue(nameof(MemberBadgeConfigurationGUID), value);
        }


        /// <summary>
        /// Member badge configuration last modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime MemberBadgeConfigurationLastModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(MemberBadgeConfigurationLastModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(MemberBadgeConfigurationLastModified), value);
        }


        /// <summary>
        /// Member badge configuration is logging verbose.
        /// </summary>
        [DatabaseField]
        public virtual bool MemberBadgeConfigurationIsLoggingVerbose
        {
            get => ValidationHelper.GetBoolean(GetValue(nameof(MemberBadgeConfigurationIsLoggingVerbose)), false);
            set => SetValue(nameof(MemberBadgeConfigurationIsLoggingVerbose), value);
        }


        /// <summary>
        /// Member badge configuration re execute loop delay minutes.
        /// </summary>
        [DatabaseField]
        public virtual int MemberBadgeConfigurationReExecuteLoopDelayMinutes
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(MemberBadgeConfigurationReExecuteLoopDelayMinutes)), 60);
            set => SetValue(nameof(MemberBadgeConfigurationReExecuteLoopDelayMinutes), value);
        }


        /// <summary>
        /// Member badge configuration next rule process delay seconds.
        /// </summary>
        [DatabaseField]
        public virtual int MemberBadgeConfigurationNextRuleProcessDelaySeconds
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(MemberBadgeConfigurationNextRuleProcessDelaySeconds)), 60);
            set => SetValue(nameof(MemberBadgeConfigurationNextRuleProcessDelaySeconds), value);
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
        /// Creates an empty instance of the <see cref="MemberBadgeConfigurationInfo"/> class.
        /// </summary>
        public MemberBadgeConfigurationInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="MemberBadgeConfigurationInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public MemberBadgeConfigurationInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
