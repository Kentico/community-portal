using System;
using System.Data;
using System.Runtime.Serialization;
using System.Collections.Generic;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(QAndAAnswerDataInfo), QAndAAnswerDataInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="QAndAAnswerDataInfo"/>.
    /// </summary>
    [Serializable, InfoCache(InfoCacheBy.ID)]
    public partial class QAndAAnswerDataInfo : AbstractInfo<QAndAAnswerDataInfo, IQAndAAnswerDataInfoProvider>
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.qandaanswerdata";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(QAndAAnswerDataInfoProvider), OBJECT_TYPE, "KenticoCommunity.QAndAAnswerData", "QAndAAnswerDataID", "QAndAAnswerDataDateModified", "QAndAAnswerDataGUID", "QAndAAnswerDataCodeName", "QAndAAnswerDataCodeName", null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new ObjectDependency("QAndAAnswerDataAuthorMemberID", "cms.member", ObjectDependencyEnum.RequiredHasDefault),
                new ObjectDependency("QAndAAnswerDataQuestionWebPageItemID", "cms.webpageitem", ObjectDependencyEnum.Binding),
            },
            ContinuousIntegrationSettings =
            {
                Enabled = true
            }
        };


        /// <summary>
        /// Q and A answer data ID.
        /// </summary>
        [DatabaseField]
        public virtual int QAndAAnswerDataID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(QAndAAnswerDataID)), 0);
            set => SetValue(nameof(QAndAAnswerDataID), value);
        }


        /// <summary>
        /// Q and A answer data GUID.
        /// </summary>
        [DatabaseField]
        public virtual Guid QAndAAnswerDataGUID
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(QAndAAnswerDataGUID)), Guid.Empty);
            set => SetValue(nameof(QAndAAnswerDataGUID), value);
        }


        /// <summary>
        /// Q and A answer data code name.
        /// </summary>
        [DatabaseField]
        public virtual string QAndAAnswerDataCodeName
        {
            get => ValidationHelper.GetString(GetValue(nameof(QAndAAnswerDataCodeName)), String.Empty);
            set => SetValue(nameof(QAndAAnswerDataCodeName), value);
        }


        /// <summary>
        /// Q and A answer data date created.
        /// </summary>
        [DatabaseField]
        public virtual DateTime QAndAAnswerDataDateCreated
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(QAndAAnswerDataDateCreated)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(QAndAAnswerDataDateCreated), value);
        }


        /// <summary>
        /// Q and A answer data date modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime QAndAAnswerDataDateModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(QAndAAnswerDataDateModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(QAndAAnswerDataDateModified), value);
        }


        /// <summary>
        /// Q and A answer data author member ID.
        /// </summary>
        [DatabaseField]
        public virtual int QAndAAnswerDataAuthorMemberID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(QAndAAnswerDataAuthorMemberID)), 0);
            set => SetValue(nameof(QAndAAnswerDataAuthorMemberID), value);
        }


        /// <summary>
        /// Q and A answer data content.
        /// </summary>
        [DatabaseField]
        public virtual string QAndAAnswerDataContent
        {
            get => ValidationHelper.GetString(GetValue(nameof(QAndAAnswerDataContent)), String.Empty);
            set => SetValue(nameof(QAndAAnswerDataContent), value);
        }


        /// <summary>
        /// Q and A answer data question web page item ID.
        /// </summary>
        [DatabaseField]
        public virtual int QAndAAnswerDataQuestionWebPageItemID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(QAndAAnswerDataQuestionWebPageItemID)), 0);
            set => SetValue(nameof(QAndAAnswerDataQuestionWebPageItemID), value);
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
        protected QAndAAnswerDataInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="QAndAAnswerDataInfo"/> class.
        /// </summary>
        public QAndAAnswerDataInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="QAndAAnswerDataInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public QAndAAnswerDataInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
