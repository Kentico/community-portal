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
using CMS.ContentEngine;

namespace Kentico.Community.Portal.Core.Content
{
	/// <summary>
	/// Represents a content item of type <see cref="WebsiteGlobalContent"/>.
	/// </summary>
	[RegisterContentTypeMapping(CONTENT_TYPE_NAME)]
	public partial class WebsiteGlobalContent : IContentItemFieldsSource
	{
		/// <summary>
		/// Code name of the content type.
		/// </summary>
		public const string CONTENT_TYPE_NAME = "KenticoCommunity.WebsiteGlobalContent";


		/// <summary>
		/// Represents system properties for a content item.
		/// </summary>
		[SystemField]
		public ContentItemFields SystemFields { get; set; }


		/// <summary>
		/// WebsiteGlobalContentDisplayName.
		/// </summary>
		public string WebsiteGlobalContentDisplayName { get; set; }


		/// <summary>
		/// WebsiteGlobalContentPageTitleFormat.
		/// </summary>
		public string WebsiteGlobalContentPageTitleFormat { get; set; }


		/// <summary>
		/// WebsiteGlobalContentLogoImageContent.
		/// </summary>
		public IEnumerable<ImageContent> WebsiteGlobalContentLogoImageContent { get; set; }


		/// <summary>
		/// WebsiteGlobalContentRobotsTxt.
		/// </summary>
		public string WebsiteGlobalContentRobotsTxt { get; set; }
	}
}