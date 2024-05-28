using System;
using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(SupportRequestConfigurationInfo), SupportRequestConfigurationInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="SupportRequestConfigurationInfo"/>.
    /// </summary>
    [Serializable]
    public partial class SupportRequestConfigurationInfo : AbstractInfo<SupportRequestConfigurationInfo, IInfoProvider<SupportRequestConfigurationInfo>>, IInfoWithId, IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.supportrequestconfiguration";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<SupportRequestConfigurationInfo>), OBJECT_TYPE, "KenticoCommunity.SupportRequestConfiguration", "SupportRequestConfigurationID", "SupportRequestConfigurationLastModified", "SupportRequestConfigurationGUID", null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            ContinuousIntegrationSettings =
            {
                Enabled = true
            },
            SensitiveColumns = [nameof(SupportRequestConfigurationExternalEndpointURL)]
        };


        /// <summary>
        /// Support request configuration ID.
        /// </summary>
        [DatabaseField]
        public virtual int SupportRequestConfigurationID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(SupportRequestConfigurationID)), 0);
            set => SetValue(nameof(SupportRequestConfigurationID), value);
        }


        /// <summary>
        /// Support request configuration GUID.
        /// </summary>
        [DatabaseField]
        public virtual Guid SupportRequestConfigurationGUID
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(SupportRequestConfigurationGUID)), Guid.Empty);
            set => SetValue(nameof(SupportRequestConfigurationGUID), value);
        }


        /// <summary>
        /// Support request configuration is queue processing enabled.
        /// </summary>
        [DatabaseField]
        public virtual bool SupportRequestConfigurationIsQueueProcessingEnabled
        {
            get => ValidationHelper.GetBoolean(GetValue(nameof(SupportRequestConfigurationIsQueueProcessingEnabled)), true);
            set => SetValue(nameof(SupportRequestConfigurationIsQueueProcessingEnabled), value);
        }


        /// <summary>
        /// Support request configuration external endpoint URL.
        /// </summary>
        [DatabaseField]
        public virtual string SupportRequestConfigurationExternalEndpointURL
        {
            get => ValidationHelper.GetString(GetValue(nameof(SupportRequestConfigurationExternalEndpointURL)), String.Empty);
            set => SetValue(nameof(SupportRequestConfigurationExternalEndpointURL), value);
        }


        /// <summary>
        /// Support request configuration last modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime SupportRequestConfigurationLastModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(SupportRequestConfigurationLastModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(SupportRequestConfigurationLastModified), value);
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
        protected SupportRequestConfigurationInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="SupportRequestConfigurationInfo"/> class.
        /// </summary>
        public SupportRequestConfigurationInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="SupportRequestConfigurationInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public SupportRequestConfigurationInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
