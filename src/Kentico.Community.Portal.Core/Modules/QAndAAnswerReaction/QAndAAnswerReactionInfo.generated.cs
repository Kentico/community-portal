using System;
using System.Data;
using System.Collections.Generic;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(QAndAAnswerReactionInfo), QAndAAnswerReactionInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="QAndAAnswerReactionInfo"/>.
    /// </summary>
    public partial class QAndAAnswerReactionInfo : AbstractInfo<QAndAAnswerReactionInfo, IInfoProvider<QAndAAnswerReactionInfo>>, IInfoWithId
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.qandaanswerreaction";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<QAndAAnswerReactionInfo>), OBJECT_TYPE, "KenticoCommunity.QAndAAnswerReaction", "QAndAAnswerReactionID", "QAndAAnswerReactionDateModified", null, null, null, null, "QAndAAnswerReactionAnswerID", "kenticocommunity.qandaanswerdata")
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new ObjectDependency("QAndAAnswerReactionMemberID", "cms.member", ObjectDependencyEnum.Binding),
            },
        };


        /// <summary>
        /// Q and A answer reaction ID
        /// </summary>
        [DatabaseField]
        public virtual int QAndAAnswerReactionID
        {
            get
            {
                return ValidationHelper.GetInteger(GetValue("QAndAAnswerReactionID"), 0);
            }
            set
            {
                SetValue("QAndAAnswerReactionID", value);
            }
        }


        /// <summary>
        /// Q and A answer reaction GUID
        /// </summary>
        [DatabaseField]
        public virtual Guid QAndAAnswerReactionGUID
        {
            get
            {
                return ValidationHelper.GetGuid(GetValue("QAndAAnswerReactionGUID"), Guid.Empty);
            }
            set
            {
                SetValue("QAndAAnswerReactionGUID", value);
            }
        }


        /// <summary>
        /// Q and A answer reaction answer ID
        /// </summary>
        [DatabaseField]
        public virtual int QAndAAnswerReactionAnswerID
        {
            get
            {
                return ValidationHelper.GetInteger(GetValue("QAndAAnswerReactionAnswerID"), 0);
            }
            set
            {
                SetValue("QAndAAnswerReactionAnswerID", value);
            }
        }


        /// <summary>
        /// Q and A answer reaction member ID
        /// </summary>
        [DatabaseField]
        public virtual int QAndAAnswerReactionMemberID
        {
            get
            {
                return ValidationHelper.GetInteger(GetValue("QAndAAnswerReactionMemberID"), 0);
            }
            set
            {
                SetValue("QAndAAnswerReactionMemberID", value);
            }
        }


        /// <summary>
        /// Q and A answer reaction type
        /// </summary>
        [DatabaseField]
        public virtual string QAndAAnswerReactionType
        {
            get
            {
                return ValidationHelper.GetString(GetValue("QAndAAnswerReactionType"), String.Empty);
            }
            set
            {
                SetValue("QAndAAnswerReactionType", value);
            }
        }


        /// <summary>
        /// Q and A answer reaction date modified
        /// </summary>
        [DatabaseField]
        public virtual DateTime QAndAAnswerReactionDateModified
        {
            get
            {
                return ValidationHelper.GetDateTime(GetValue("QAndAAnswerReactionDateModified"), DateTimeHelper.ZERO_TIME);
            }
            set
            {
                SetValue("QAndAAnswerReactionDateModified", value);
            }
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
        /// Creates an empty instance of the <see cref="QAndAAnswerReactionInfo"/> class.
        /// </summary>
        public QAndAAnswerReactionInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instance of the <see cref="QAndAAnswerReactionInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public QAndAAnswerReactionInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
