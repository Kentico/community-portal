using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(PortalWebsiteSettingsInfo), PortalWebsiteSettingsInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="PortalWebsiteSettingsInfo"/>.
    /// </summary>
    public partial class PortalWebsiteSettingsInfo : AbstractInfo<PortalWebsiteSettingsInfo, IInfoProvider<PortalWebsiteSettingsInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.portalwebsitesettings";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<PortalWebsiteSettingsInfo>), OBJECT_TYPE, "KenticoCommunity.PortalWebsiteSettings", "PortalWebsiteSettingsID", "PortalWebsiteSettingsDateModified", "PortalWebsiteSettingsGUID", "PortalWebsiteSettingsName", "PortalWebsiteSettingsName", null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
            },
        };


        /// <summary>
        /// Portal website settings ID.
        /// </summary>
        [DatabaseField]
        public virtual int PortalWebsiteSettingsID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(PortalWebsiteSettingsID)), 0);
            set => SetValue(nameof(PortalWebsiteSettingsID), value);
        }


        /// <summary>
        /// Portal website settings name.
        /// </summary>
        [DatabaseField]
        public virtual string PortalWebsiteSettingsName
        {
            get => ValidationHelper.GetString(GetValue(nameof(PortalWebsiteSettingsName)), String.Empty);
            set => SetValue(nameof(PortalWebsiteSettingsName), value);
        }


        /// <summary>
        /// Portal website settings GUID.
        /// </summary>
        [DatabaseField]
        public virtual Guid PortalWebsiteSettingsGUID
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(PortalWebsiteSettingsGUID)), Guid.Empty);
            set => SetValue(nameof(PortalWebsiteSettingsGUID), value);
        }


        /// <summary>
        /// Portal website settings website global content.
        /// </summary>
        [DatabaseField(ValueType = typeof(string))]
        public IEnumerable<global::CMS.ContentEngine.ContentItemReference> PortalWebsiteSettingsWebsiteGlobalContent
        {
            get => global::CMS.DataEngine.Internal.JsonDataTypeConverter.ConvertToModels<global::CMS.ContentEngine.ContentItemReference>(GetValue(nameof(PortalWebsiteSettingsWebsiteGlobalContent), String.Empty));
            set => SetValue(nameof(PortalWebsiteSettingsWebsiteGlobalContent), global::CMS.DataEngine.Internal.JsonDataTypeConverter.ConvertToString<IEnumerable<global::CMS.ContentEngine.ContentItemReference>>(value, Enumerable.Empty<global::CMS.ContentEngine.ContentItemReference>(), null));
        }


        /// <summary>
        /// Portal website settings website cookie banner content.
        /// </summary>
        [DatabaseField(ValueType = typeof(string))]
        public IEnumerable<global::CMS.ContentEngine.ContentItemReference> PortalWebsiteSettingsWebsiteCookieBannerContent
        {
            get => global::CMS.DataEngine.Internal.JsonDataTypeConverter.ConvertToModels<global::CMS.ContentEngine.ContentItemReference>(GetValue(nameof(PortalWebsiteSettingsWebsiteCookieBannerContent), String.Empty));
            set => SetValue(nameof(PortalWebsiteSettingsWebsiteCookieBannerContent), global::CMS.DataEngine.Internal.JsonDataTypeConverter.ConvertToString<IEnumerable<global::CMS.ContentEngine.ContentItemReference>>(value, Enumerable.Empty<global::CMS.ContentEngine.ContentItemReference>(), null));
        }


        /// <summary>
        /// Portal website settings website alert settings content.
        /// </summary>
        [DatabaseField(ValueType = typeof(string))]
        public IEnumerable<global::CMS.ContentEngine.ContentItemReference> PortalWebsiteSettingsWebsiteAlertSettingsContent
        {
            get => global::CMS.DataEngine.Internal.JsonDataTypeConverter.ConvertToModels<global::CMS.ContentEngine.ContentItemReference>(GetValue(nameof(PortalWebsiteSettingsWebsiteAlertSettingsContent), String.Empty));
            set => SetValue(nameof(PortalWebsiteSettingsWebsiteAlertSettingsContent), global::CMS.DataEngine.Internal.JsonDataTypeConverter.ConvertToString<IEnumerable<global::CMS.ContentEngine.ContentItemReference>>(value, Enumerable.Empty<global::CMS.ContentEngine.ContentItemReference>(), null));
        }


        /// <summary>
        /// Portal website settings date created.
        /// </summary>
        [DatabaseField]
        public virtual DateTime PortalWebsiteSettingsDateCreated
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(PortalWebsiteSettingsDateCreated)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(PortalWebsiteSettingsDateCreated), value);
        }


        /// <summary>
        /// Portal website settings date modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime PortalWebsiteSettingsDateModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(PortalWebsiteSettingsDateModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(PortalWebsiteSettingsDateModified), value);
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
        /// Creates an empty instance of the <see cref="PortalWebsiteSettingsInfo"/> class.
        /// </summary>
        public PortalWebsiteSettingsInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="PortalWebsiteSettingsInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public PortalWebsiteSettingsInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
