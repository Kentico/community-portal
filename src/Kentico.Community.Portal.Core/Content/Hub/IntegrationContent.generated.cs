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
	/// Represents a content item of type <see cref="IntegrationContent"/>.
	/// </summary>
	[RegisterContentTypeMapping(CONTENT_TYPE_NAME)]
	public partial class IntegrationContent : IContentItemFieldsSource, IFeaturedImageFields, IBasicItemFields, ICoreTaxonomyFields
	{
		/// <summary>
		/// Code name of the content type.
		/// </summary>
		public const string CONTENT_TYPE_NAME = "KenticoCommunity.IntegrationContent";


		/// <summary>
		/// Represents system properties for a content item.
		/// </summary>
		[SystemField]
		public ContentItemFields SystemFields { get; set; }


		/// <summary>
		/// IntegrationContentPublishedDate.
		/// </summary>
		public DateTime IntegrationContentPublishedDate { get; set; }


		/// <summary>
		/// IntegrationContentRepositoryLinkURL.
		/// </summary>
		public string IntegrationContentRepositoryLinkURL { get; set; }


		/// <summary>
		/// IntegrationContentLibraryLinkURL.
		/// </summary>
		public string IntegrationContentLibraryLinkURL { get; set; }


		/// <summary>
		/// IntegrationContentIntegrationType.
		/// </summary>
		public IEnumerable<TagReference> IntegrationContentIntegrationType { get; set; }


		/// <summary>
		/// IntegrationContentHasMemberAuthor.
		/// </summary>
		public bool IntegrationContentHasMemberAuthor { get; set; }


		/// <summary>
		/// IntegrationContentAuthorMemberID.
		/// </summary>
		public int IntegrationContentAuthorMemberID { get; set; }


		/// <summary>
		/// IntegrationContentAuthorName.
		/// </summary>
		public string IntegrationContentAuthorName { get; set; }


		/// <summary>
		/// IntegrationContentAuthorLinkURL.
		/// </summary>
		public string IntegrationContentAuthorLinkURL { get; set; }


		/// <summary>
		/// FeaturedImageImageContent.
		/// </summary>
		public IEnumerable<ImageContent> FeaturedImageImageContent { get; set; }


		/// <summary>
		/// BasicItemTitle.
		/// </summary>
		public string BasicItemTitle { get; set; }


		/// <summary>
		/// BasicItemShortDescription.
		/// </summary>
		public string BasicItemShortDescription { get; set; }


		/// <summary>
		/// CoreTaxonomyDXTopics.
		/// </summary>
		public IEnumerable<TagReference> CoreTaxonomyDXTopics { get; set; }
	}
}