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

[assembly: RegisterBizForm(MVPActivity2024Item.CLASS_NAME, typeof(MVPActivity2024Item))]

namespace Kentico.Community.Portal.Core.Forms
{
	/// <summary>
	/// Represents a content item of type MVPActivity2024Item.
	/// </summary>
	public partial class MVPActivity2024Item : BizFormItem
	{
		#region "Constants and variables"

		/// <summary>
		/// The name of the data class.
		/// </summary>
		public const string CLASS_NAME = "BizForm.MVPActivity2024";


		/// <summary>
		/// The instance of the class that provides extended API for working with MVPActivity2024Item fields.
		/// </summary>
		private readonly MVPActivity2024ItemFields mFields;

		#endregion


		#region "Properties"

		/// <summary>
		/// TextInput.
		/// </summary>
		[DatabaseField]
		public string TextInput
		{
			get => ValidationHelper.GetString(GetValue(nameof(TextInput)), @"");
			set => SetValue(nameof(TextInput), value);
		}


		/// <summary>
		/// TextArea.
		/// </summary>
		[DatabaseField]
		public string TextArea
		{
			get => ValidationHelper.GetString(GetValue(nameof(TextArea)), @"");
			set => SetValue(nameof(TextArea), value);
		}


		/// <summary>
		/// DropDown.
		/// </summary>
		[DatabaseField]
		public string DropDown
		{
			get => ValidationHelper.GetString(GetValue(nameof(DropDown)), @"");
			set => SetValue(nameof(DropDown), value);
		}


		/// <summary>
		/// TextInput_1.
		/// </summary>
		[DatabaseField]
		public string TextInput_1
		{
			get => ValidationHelper.GetString(GetValue(nameof(TextInput_1)), @"");
			set => SetValue(nameof(TextInput_1), value);
		}


		/// <summary>
		/// DropDown_1.
		/// </summary>
		[DatabaseField]
		public string DropDown_1
		{
			get => ValidationHelper.GetString(GetValue(nameof(DropDown_1)), @"");
			set => SetValue(nameof(DropDown_1), value);
		}


		/// <summary>
		/// Gets an object that provides extended API for working with MVPActivity2024Item fields.
		/// </summary>
		[RegisterProperty]
		public MVPActivity2024ItemFields Fields
		{
			get => mFields;
		}


		/// <summary>
		/// Provides extended API for working with MVPActivity2024Item fields.
		/// </summary>
		[RegisterAllProperties]
		public partial class MVPActivity2024ItemFields : AbstractHierarchicalObject<MVPActivity2024ItemFields>
		{
			/// <summary>
			/// The content item of type MVPActivity2024Item that is a target of the extended API.
			/// </summary>
			private readonly MVPActivity2024Item mInstance;


			/// <summary>
			/// Initializes a new instance of the <see cref="MVPActivity2024ItemFields" /> class with the specified content item of type MVPActivity2024Item.
			/// </summary>
			/// <param name="instance">The content item of type MVPActivity2024Item that is a target of the extended API.</param>
			public MVPActivity2024ItemFields(MVPActivity2024Item instance)
			{
				mInstance = instance;
			}


			/// <summary>
			/// TextInput.
			/// </summary>
			public string TextInput
			{
				get => mInstance.TextInput;
				set => mInstance.TextInput = value;
			}


			/// <summary>
			/// TextArea.
			/// </summary>
			public string TextArea
			{
				get => mInstance.TextArea;
				set => mInstance.TextArea = value;
			}


			/// <summary>
			/// DropDown.
			/// </summary>
			public string DropDown
			{
				get => mInstance.DropDown;
				set => mInstance.DropDown = value;
			}


			/// <summary>
			/// TextInput_1.
			/// </summary>
			public string TextInput_1
			{
				get => mInstance.TextInput_1;
				set => mInstance.TextInput_1 = value;
			}


			/// <summary>
			/// DropDown_1.
			/// </summary>
			public string DropDown_1
			{
				get => mInstance.DropDown_1;
				set => mInstance.DropDown_1 = value;
			}
		}

		#endregion


		#region "Constructors"

		/// <summary>
		/// Initializes a new instance of the <see cref="MVPActivity2024Item" /> class.
		/// </summary>
		public MVPActivity2024Item() : base(CLASS_NAME)
		{
			mFields = new MVPActivity2024ItemFields(this);
		}

		#endregion
	}
}