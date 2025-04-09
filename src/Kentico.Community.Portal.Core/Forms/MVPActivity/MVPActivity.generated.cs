//--------------------------------------------------------------------------------------------------
// <auto-generated>
//
//     This code was generated by code generator tool.
//
//     To customize the code use your own partial class. For more info about how to use and customize
//     the generated code see the documentation at https://docs.xperience.io/.
//
// </auto-generated>
//--------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CMS;
using CMS.Base;
using CMS.Helpers;
using CMS.DataEngine;
using CMS.OnlineForms;
using Kentico.Community.Portal.Core.Forms;

[assembly: RegisterBizForm(MVPActivityItem.CLASS_NAME, typeof(MVPActivityItem))]

namespace Kentico.Community.Portal.Core.Forms
{
	/// <summary>
	/// Represents a content item of type MVPActivityItem.
	/// </summary>
	public partial class MVPActivityItem : BizFormItem
	{
		#region "Constants and variables"

		/// <summary>
		/// The name of the data class.
		/// </summary>
		public const string CLASS_NAME = "BizForm.MVPActivity";


		/// <summary>
		/// The instance of the class that provides extended API for working with MVPActivityItem fields.
		/// </summary>
		private readonly MVPActivityItemFields mFields;

		#endregion


		#region "Properties"

		/// <summary>
		/// MemberID.
		/// </summary>
		[DatabaseField]
		public int MemberID
		{
			get => ValidationHelper.GetInteger(GetValue(nameof(MemberID)), 0);
			set => SetValue(nameof(MemberID), value);
		}


		/// <summary>
		/// MemberEmail.
		/// </summary>
		[DatabaseField]
		public string MemberEmail
		{
			get => ValidationHelper.GetString(GetValue(nameof(MemberEmail)), @"");
			set => SetValue(nameof(MemberEmail), value);
		}


		/// <summary>
		/// ActivityDate.
		/// </summary>
		[DatabaseField]
		public DateTime ActivityDate
		{
			get => ValidationHelper.GetDateTime(GetValue(nameof(ActivityDate)), DateTimeHelper.ZERO_TIME);
			set => SetValue(nameof(ActivityDate), value);
		}


		/// <summary>
		/// ActivityType.
		/// </summary>
		[DatabaseField]
		public string ActivityType
		{
			get => ValidationHelper.GetString(GetValue(nameof(ActivityType)), @"Social");
			set => SetValue(nameof(ActivityType), value);
		}


		/// <summary>
		/// URL.
		/// </summary>
		[DatabaseField]
		public string URL
		{
			get => ValidationHelper.GetString(GetValue(nameof(URL)), @"");
			set => SetValue(nameof(URL), value);
		}


		/// <summary>
		/// ShortDescription.
		/// </summary>
		[DatabaseField]
		public string ShortDescription
		{
			get => ValidationHelper.GetString(GetValue(nameof(ShortDescription)), @"");
			set => SetValue(nameof(ShortDescription), value);
		}


		/// <summary>
		/// Attendees.
		/// </summary>
		[DatabaseField]
		public string Attendees
		{
			get => ValidationHelper.GetString(GetValue(nameof(Attendees)), @"");
			set => SetValue(nameof(Attendees), value);
		}


		/// <summary>
		/// Impact.
		/// </summary>
		[DatabaseField]
		public string Impact
		{
			get => ValidationHelper.GetString(GetValue(nameof(Impact)), @"3");
			set => SetValue(nameof(Impact), value);
		}


		/// <summary>
		/// Effort.
		/// </summary>
		[DatabaseField]
		public string Effort
		{
			get => ValidationHelper.GetString(GetValue(nameof(Effort)), @"2");
			set => SetValue(nameof(Effort), value);
		}


		/// <summary>
		/// Satisfaction.
		/// </summary>
		[DatabaseField]
		public string Satisfaction
		{
			get => ValidationHelper.GetString(GetValue(nameof(Satisfaction)), @"3");
			set => SetValue(nameof(Satisfaction), value);
		}


		/// <summary>
		/// Gets an object that provides extended API for working with MVPActivityItem fields.
		/// </summary>
		[RegisterProperty]
		public MVPActivityItemFields Fields
		{
			get => mFields;
		}


		/// <summary>
		/// Provides extended API for working with MVPActivityItem fields.
		/// </summary>
		[RegisterAllProperties]
		public partial class MVPActivityItemFields : AbstractHierarchicalObject<MVPActivityItemFields>
		{
			/// <summary>
			/// The content item of type MVPActivityItem that is a target of the extended API.
			/// </summary>
			private readonly MVPActivityItem mInstance;


			/// <summary>
			/// Initializes a new instance of the <see cref="MVPActivityItemFields" /> class with the specified content item of type MVPActivityItem.
			/// </summary>
			/// <param name="instance">The content item of type MVPActivityItem that is a target of the extended API.</param>
			public MVPActivityItemFields(MVPActivityItem instance)
			{
				mInstance = instance;
			}


			/// <summary>
			/// MemberID.
			/// </summary>
			public int MemberID
			{
				get => mInstance.MemberID;
				set => mInstance.MemberID = value;
			}


			/// <summary>
			/// MemberEmail.
			/// </summary>
			public string MemberEmail
			{
				get => mInstance.MemberEmail;
				set => mInstance.MemberEmail = value;
			}


			/// <summary>
			/// ActivityDate.
			/// </summary>
			public DateTime ActivityDate
			{
				get => mInstance.ActivityDate;
				set => mInstance.ActivityDate = value;
			}


			/// <summary>
			/// ActivityType.
			/// </summary>
			public string ActivityType
			{
				get => mInstance.ActivityType;
				set => mInstance.ActivityType = value;
			}


			/// <summary>
			/// URL.
			/// </summary>
			public string URL
			{
				get => mInstance.URL;
				set => mInstance.URL = value;
			}


			/// <summary>
			/// ShortDescription.
			/// </summary>
			public string ShortDescription
			{
				get => mInstance.ShortDescription;
				set => mInstance.ShortDescription = value;
			}


			/// <summary>
			/// Attendees.
			/// </summary>
			public string Attendees
			{
				get => mInstance.Attendees;
				set => mInstance.Attendees = value;
			}


			/// <summary>
			/// Impact.
			/// </summary>
			public string Impact
			{
				get => mInstance.Impact;
				set => mInstance.Impact = value;
			}


			/// <summary>
			/// Effort.
			/// </summary>
			public string Effort
			{
				get => mInstance.Effort;
				set => mInstance.Effort = value;
			}


			/// <summary>
			/// Satisfaction.
			/// </summary>
			public string Satisfaction
			{
				get => mInstance.Satisfaction;
				set => mInstance.Satisfaction = value;
			}
		}

		#endregion


		#region "Constructors"

		/// <summary>
		/// Initializes a new instance of the <see cref="MVPActivityItem" /> class.
		/// </summary>
		public MVPActivityItem() : base(CLASS_NAME)
		{
			mFields = new MVPActivityItemFields(this);
		}

		#endregion
	}
}