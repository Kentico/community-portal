using System;
using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(SupportRequestProcessingEventInfo), SupportRequestProcessingEventInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="SupportRequestProcessingEventInfo"/>.
    /// </summary>
    [Serializable, InfoCache(InfoCacheBy.ID)]
    public partial class SupportRequestProcessingEventInfo : AbstractInfo<SupportRequestProcessingEventInfo, IInfoProvider<SupportRequestProcessingEventInfo>>, IInfoWithId
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.supportrequestprocessingevent";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<SupportRequestProcessingEventInfo>), OBJECT_TYPE, "KenticoCommunity.SupportRequestProcessingEvent", "SupportRequestProcessingEventID", "SupportRequestProcessingEventLastModified", null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
        };


        /// <summary>
        /// Support request processing event ID.
        /// </summary>
        [DatabaseField]
        public virtual int SupportRequestProcessingEventID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(SupportRequestProcessingEventID)), 0);
            set => SetValue(nameof(SupportRequestProcessingEventID), value);
        }


        /// <summary>
        /// Support request processing event last modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime SupportRequestProcessingEventLastModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(SupportRequestProcessingEventLastModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(SupportRequestProcessingEventLastModified), value);
        }


        /// <summary>
        /// Support request processing event message ID.
        /// </summary>
        [DatabaseField]
        public virtual string SupportRequestProcessingEventMessageID
        {
            get => ValidationHelper.GetString(GetValue(nameof(SupportRequestProcessingEventMessageID)), String.Empty);
            set => SetValue(nameof(SupportRequestProcessingEventMessageID), value);
        }


        /// <summary>
        /// Support request processing event message.
        /// </summary>
        [DatabaseField]
        public virtual string SupportRequestProcessingEventMessage
        {
            get => ValidationHelper.GetString(GetValue(nameof(SupportRequestProcessingEventMessage)), String.Empty);
            set => SetValue(nameof(SupportRequestProcessingEventMessage), value);
        }


        /// <summary>
        /// Support request processing event status.
        /// </summary>
        [DatabaseField]
        public virtual string SupportRequestProcessingEventStatus
        {
            get => ValidationHelper.GetString(GetValue(nameof(SupportRequestProcessingEventStatus)), String.Empty);
            set => SetValue(nameof(SupportRequestProcessingEventStatus), value);
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
        protected SupportRequestProcessingEventInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="SupportRequestProcessingEventInfo"/> class.
        /// </summary>
        public SupportRequestProcessingEventInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="SupportRequestProcessingEventInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public SupportRequestProcessingEventInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
