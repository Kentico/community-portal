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
	/// Represents a content item of type <see cref="EmailDefaultsContent"/>.
	/// </summary>
	[RegisterContentTypeMapping(CONTENT_TYPE_NAME)]
	public partial class EmailDefaultsContent : IContentItemFieldsSource
	{
		/// <summary>
		/// Code name of the content type.
		/// </summary>
		public const string CONTENT_TYPE_NAME = "KenticoCommunity.EmailDefaultsContent";


		/// <summary>
		/// Represents system properties for a content item.
		/// </summary>
		[SystemField]
		public ContentItemFields SystemFields { get; set; }


		/// <summary>
		/// EmailDefaultsContentDefaultURL.
		/// </summary>
		public string EmailDefaultsContentDefaultURL { get; set; }


		/// <summary>
		/// EmailDefaultsContentHeaderLogo.
		/// </summary>
		public IEnumerable<MediaAssetContent> EmailDefaultsContentHeaderLogo { get; set; }


		/// <summary>
		/// EmailDefaultsContentHeaderLinkURL.
		/// </summary>
		public string EmailDefaultsContentHeaderLinkURL { get; set; }


		/// <summary>
		/// EmailDefaultsContentSocialLinks.
		/// </summary>
		public IEnumerable<SocialLinkContent> EmailDefaultsContentSocialLinks { get; set; }


		/// <summary>
		/// EmailDefaultsContentFooterLogo.
		/// </summary>
		public IEnumerable<MediaAssetContent> EmailDefaultsContentFooterLogo { get; set; }


		/// <summary>
		/// EmailDefaultsContentFooterLinkURL.
		/// </summary>
		public string EmailDefaultsContentFooterLinkURL { get; set; }


		/// <summary>
		/// EmailDefaultsContentHTMLStyles.
		/// </summary>
		public string EmailDefaultsContentHTMLStyles { get; set; }
	}
}