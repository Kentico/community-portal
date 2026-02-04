using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(QAndAQuestionReactionInfo), QAndAQuestionReactionInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="QAndAQuestionReactionInfo"/>.
    /// </summary>
    public partial class QAndAQuestionReactionInfo : AbstractInfo<QAndAQuestionReactionInfo, IInfoProvider<QAndAQuestionReactionInfo>>, IInfoWithId, IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.qandaquestionreaction";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<QAndAQuestionReactionInfo>), OBJECT_TYPE, "KenticoCommunity.QAndAQuestionReaction", "QAndAQuestionReactionID", "QAndAQuestionReactionDateModified", "QAndAQuestionReactionGUID", null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new ObjectDependency("QAndAQuestionReactionMemberID", "cms.member", ObjectDependencyEnum.Required),
            },
        };


        /// <summary>
        /// Q and A question reaction ID.
        /// </summary>
        [DatabaseField]
        public virtual int QAndAQuestionReactionID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(QAndAQuestionReactionID)), 0);
            set => SetValue(nameof(QAndAQuestionReactionID), value);
        }


        /// <summary>
        /// Q and A question reaction GUID.
        /// </summary>
        [DatabaseField]
        public virtual Guid QAndAQuestionReactionGUID
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(QAndAQuestionReactionGUID)), Guid.Empty);
            set => SetValue(nameof(QAndAQuestionReactionGUID), value);
        }


        /// <summary>
        /// Q and A question reaction web page item ID.
        /// </summary>
        [DatabaseField]
        public virtual int QAndAQuestionReactionWebPageItemID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(QAndAQuestionReactionWebPageItemID)), 0);
            set => SetValue(nameof(QAndAQuestionReactionWebPageItemID), value);
        }


        /// <summary>
        /// Q and A question reaction member ID.
        /// </summary>
        [DatabaseField]
        public virtual int QAndAQuestionReactionMemberID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(QAndAQuestionReactionMemberID)), 0);
            set => SetValue(nameof(QAndAQuestionReactionMemberID), value);
        }


        /// <summary>
        /// Q and A question reaction type.
        /// </summary>
        [DatabaseField]
        public virtual string QAndAQuestionReactionType
        {
            get => ValidationHelper.GetString(GetValue(nameof(QAndAQuestionReactionType)), String.Empty);
            set => SetValue(nameof(QAndAQuestionReactionType), value);
        }


        /// <summary>
        /// Q and A question reaction date modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime QAndAQuestionReactionDateModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(QAndAQuestionReactionDateModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(QAndAQuestionReactionDateModified), value);
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
        /// Creates an empty instance of the <see cref="QAndAQuestionReactionInfo"/> class.
        /// </summary>
        public QAndAQuestionReactionInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="QAndAQuestionReactionInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public QAndAQuestionReactionInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
