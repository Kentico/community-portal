using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Community.Portal.Core.Modules;

[assembly: RegisterObjectType(typeof(FormRetentionPolicyInfo), FormRetentionPolicyInfo.OBJECT_TYPE)]

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="FormRetentionPolicyInfo"/>.
    /// </summary>
    public partial class FormRetentionPolicyInfo : AbstractInfo<FormRetentionPolicyInfo, IInfoProvider<FormRetentionPolicyInfo>>, IInfoWithId, IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticocommunity.formretentionpolicy";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<FormRetentionPolicyInfo>), OBJECT_TYPE, "KenticoCommunity.FormRetentionPolicy", "FormRetentionPolicyID", "FormRetentionPolicyDateModified", "FormRetentionPolicyGUID", null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new ObjectDependency("FormRetentionPolicyFormID", "cms.form", ObjectDependencyEnum.Required),
            },
        };


        /// <summary>
        /// Form retention policy ID.
        /// </summary>
        [DatabaseField]
        public virtual int FormRetentionPolicyID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(FormRetentionPolicyID)), 0);
            set => SetValue(nameof(FormRetentionPolicyID), value);
        }


        /// <summary>
        /// Form retention policy GUID.
        /// </summary>
        [DatabaseField]
        public virtual Guid FormRetentionPolicyGUID
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(FormRetentionPolicyGUID)), Guid.Empty);
            set => SetValue(nameof(FormRetentionPolicyGUID), value);
        }


        /// <summary>
        /// Form retention policy form ID.
        /// </summary>
        [DatabaseField]
        public virtual int FormRetentionPolicyFormID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(FormRetentionPolicyFormID)), 0);
            set => SetValue(nameof(FormRetentionPolicyFormID), value);
        }


        /// <summary>
        /// Form retention policy type.
        /// </summary>
        [DatabaseField]
        public virtual string FormRetentionPolicyType
        {
            get => ValidationHelper.GetString(GetValue(nameof(FormRetentionPolicyType)), String.Empty);
            set => SetValue(nameof(FormRetentionPolicyType), value);
        }


        /// <summary>
        /// Form retention policy retention days.
        /// </summary>
        [DatabaseField]
        public virtual int FormRetentionPolicyRetentionDays
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(FormRetentionPolicyRetentionDays)), 0);
            set => SetValue(nameof(FormRetentionPolicyRetentionDays), value, 0);
        }


        /// <summary>
        /// Form retention policy date modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime FormRetentionPolicyDateModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(FormRetentionPolicyDateModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(FormRetentionPolicyDateModified), value);
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
        /// Creates an empty instance of the <see cref="FormRetentionPolicyInfo"/> class.
        /// </summary>
        public FormRetentionPolicyInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="FormRetentionPolicyInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public FormRetentionPolicyInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}