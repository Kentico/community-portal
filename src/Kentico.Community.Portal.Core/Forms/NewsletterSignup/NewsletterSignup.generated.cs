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

[assembly: RegisterBizForm(NewsletterSignupItem.CLASS_NAME, typeof(NewsletterSignupItem))]

namespace Kentico.Community.Portal.Core.Forms
{
	/// <summary>
	/// Represents a content item of type NewsletterSignupItem.
	/// </summary>
	public partial class NewsletterSignupItem : BizFormItem
	{
		#region "Constants and variables"

		/// <summary>
		/// The name of the data class.
		/// </summary>
		public const string CLASS_NAME = "BizForm.NewsletterSignup";


		/// <summary>
		/// The instance of the class that provides extended API for working with NewsletterSignupItem fields.
		/// </summary>
		private readonly NewsletterSignupItemFields mFields;

		#endregion


		#region "Properties"

		/// <summary>
		/// Email.
		/// </summary>
		[DatabaseField]
		public string Email
		{
			get => ValidationHelper.GetString(GetValue(nameof(Email)), @"");
			set => SetValue(nameof(Email), value);
		}


		/// <summary>
		/// ConsentAgreement.
		/// </summary>
		[DatabaseField]
		public Guid ConsentAgreement
		{
			get => ValidationHelper.GetGuid(GetValue(nameof(ConsentAgreement)), Guid.Empty);
			set => SetValue(nameof(ConsentAgreement), value);
		}


		/// <summary>
		/// Recaptcha_FormComponent.
		/// </summary>
		[DatabaseField]
		public string Recaptcha_FormComponent
		{
			get => ValidationHelper.GetString(GetValue(nameof(Recaptcha_FormComponent)), @"");
			set => SetValue(nameof(Recaptcha_FormComponent), value);
		}


		/// <summary>
		/// Gets an object that provides extended API for working with NewsletterSignupItem fields.
		/// </summary>
		[RegisterProperty]
		public NewsletterSignupItemFields Fields
		{
			get => mFields;
		}


		/// <summary>
		/// Provides extended API for working with NewsletterSignupItem fields.
		/// </summary>
		[RegisterAllProperties]
		public partial class NewsletterSignupItemFields : AbstractHierarchicalObject<NewsletterSignupItemFields>
		{
			/// <summary>
			/// The content item of type NewsletterSignupItem that is a target of the extended API.
			/// </summary>
			private readonly NewsletterSignupItem mInstance;


			/// <summary>
			/// Initializes a new instance of the <see cref="NewsletterSignupItemFields" /> class with the specified content item of type NewsletterSignupItem.
			/// </summary>
			/// <param name="instance">The content item of type NewsletterSignupItem that is a target of the extended API.</param>
			public NewsletterSignupItemFields(NewsletterSignupItem instance)
			{
				mInstance = instance;
			}


			/// <summary>
			/// Email.
			/// </summary>
			public string Email
			{
				get => mInstance.Email;
				set => mInstance.Email = value;
			}


			/// <summary>
			/// ConsentAgreement.
			/// </summary>
			public Guid ConsentAgreement
			{
				get => mInstance.ConsentAgreement;
				set => mInstance.ConsentAgreement = value;
			}


			/// <summary>
			/// Recaptcha_FormComponent.
			/// </summary>
			public string Recaptcha_FormComponent
			{
				get => mInstance.Recaptcha_FormComponent;
				set => mInstance.Recaptcha_FormComponent = value;
			}
		}

		#endregion


		#region "Constructors"

		/// <summary>
		/// Initializes a new instance of the <see cref="NewsletterSignupItem" /> class.
		/// </summary>
		public NewsletterSignupItem() : base(CLASS_NAME)
		{
			mFields = new NewsletterSignupItemFields(this);
		}

		#endregion
	}
}