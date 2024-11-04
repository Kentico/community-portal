using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(BizFormSettingsInfo), BizFormSettingsInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="BizFormSettingsInfo"/>.
    /// </summary>
    public partial class BizFormSettingsInfo : AbstractInfo<BizFormSettingsInfo, IInfoProvider<BizFormSettingsInfo>>, IInfoWithId, IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.bizformsettings";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<BizFormSettingsInfo>), OBJECT_TYPE, "KenticoCommunity.BizFormSettings", "BizFormSettingsID", "BizFormSettingsDateModified", "BizFormSettingsGUID", null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new ObjectDependency("BizFormSettingsBizFormID", "cms.form", ObjectDependencyEnum.Required),
                new ObjectDependency("BizFormSettingsInternalAutoresponderRecipientUserID", "cms.user", ObjectDependencyEnum.NotRequired),
            },
        };


        /// <summary>
        /// Biz form settings ID.
        /// </summary>
        [DatabaseField]
        public virtual int BizFormSettingsID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(BizFormSettingsID)), 0);
            set => SetValue(nameof(BizFormSettingsID), value);
        }


        /// <summary>
        /// Biz form settings GUID.
        /// </summary>
        [DatabaseField]
        public virtual Guid BizFormSettingsGUID
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(BizFormSettingsGUID)), Guid.Empty);
            set => SetValue(nameof(BizFormSettingsGUID), value);
        }


        /// <summary>
        /// Biz form settings date created.
        /// </summary>
        [DatabaseField]
        public virtual DateTime BizFormSettingsDateCreated
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(BizFormSettingsDateCreated)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(BizFormSettingsDateCreated), value);
        }


        /// <summary>
        /// Biz form settings date modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime BizFormSettingsDateModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(BizFormSettingsDateModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(BizFormSettingsDateModified), value);
        }


        /// <summary>
        /// Biz form settings biz form ID.
        /// </summary>
        [DatabaseField]
        public virtual int BizFormSettingsBizFormID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(BizFormSettingsBizFormID)), 0);
            set => SetValue(nameof(BizFormSettingsBizFormID), value);
        }


        /// <summary>
        /// Biz form settings internal autoresponder is enabled.
        /// </summary>
        [DatabaseField]
        public virtual bool BizFormSettingsInternalAutoresponderIsEnabled
        {
            get => ValidationHelper.GetBoolean(GetValue(nameof(BizFormSettingsInternalAutoresponderIsEnabled)), false);
            set => SetValue(nameof(BizFormSettingsInternalAutoresponderIsEnabled), value);
        }


        /// <summary>
        /// Biz form settings internal autoresponder recipient user ID.
        /// </summary>
        [DatabaseField]
        public virtual int BizFormSettingsInternalAutoresponderRecipientUserID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(BizFormSettingsInternalAutoresponderRecipientUserID)), 0);
            set => SetValue(nameof(BizFormSettingsInternalAutoresponderRecipientUserID), value);
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
        /// Creates an empty instance of the <see cref="BizFormSettingsInfo"/> class.
        /// </summary>
        public BizFormSettingsInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="BizFormSettingsInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public BizFormSettingsInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
