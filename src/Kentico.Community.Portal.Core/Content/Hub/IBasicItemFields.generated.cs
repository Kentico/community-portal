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

namespace Kentico.Community.Portal.Core.Content
{
	/// <summary>
	/// Defines a contract for content types with the <see cref="IBasicItemFields"/> reusable schema assigned.
	/// </summary>
	public interface IBasicItemFields
	{
		/// <summary>
		/// Code name of the reusable field schema.
		/// </summary>
		public const string REUSABLE_FIELD_SCHEMA_NAME = "BasicItemFields";


		/// <summary>
		/// BasicItemTitle.
		/// </summary>
		public string BasicItemTitle { get; set; }


		/// <summary>
		/// BasicItemShortDescription.
		/// </summary>
		public string BasicItemShortDescription { get; set; }
	}
}