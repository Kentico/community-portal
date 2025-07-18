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
using CMS.Websites;

namespace Kentico.Community.Portal.Core.Content
{
	/// <summary>
	/// Represents a page of type <see cref="BlogPostPage"/>.
	/// </summary>
	[RegisterContentTypeMapping(CONTENT_TYPE_NAME)]
	public partial class BlogPostPage : IWebPageFieldsSource, IWebPageMetaFields, IBasicItemFields, ICoreTaxonomyFields
	{
		/// <summary>
		/// Code name of the content type.
		/// </summary>
		public const string CONTENT_TYPE_NAME = "KenticoCommunity.BlogPostPage";


		/// <summary>
		/// Represents system properties for a web page item.
		/// </summary>
		[SystemField]
		public WebPageFields SystemFields { get; set; }


		/// <summary>
		/// BlogPostPageAuthorContent.
		/// </summary>
		public IEnumerable<AuthorContent> BlogPostPageAuthorContent { get; set; }


		/// <summary>
		/// BlogPostPagePublishedDate.
		/// </summary>
		public DateTime BlogPostPagePublishedDate { get; set; }


		/// <summary>
		/// BlogPostPageBlogType.
		/// </summary>
		public IEnumerable<TagReference> BlogPostPageBlogType { get; set; }


		/// <summary>
		/// BlogPostPageQAndAQuestionPages.
		/// </summary>
		public IEnumerable<QAndAQuestionPage> BlogPostPageQAndAQuestionPages { get; set; }


		/// <summary>
		/// BlogPostPageBlogPostContent.
		/// </summary>
		public IEnumerable<BlogPostContent> BlogPostPageBlogPostContent { get; set; }


		/// <summary>
		/// WebPageMetaTitle.
		/// </summary>
		public string WebPageMetaTitle { get; set; }


		/// <summary>
		/// WebPageMetaShortDescription.
		/// </summary>
		public string WebPageMetaShortDescription { get; set; }


		/// <summary>
		/// WebPageMetaExcludeFromSitemap.
		/// </summary>
		public bool WebPageMetaExcludeFromSitemap { get; set; }


		/// <summary>
		/// WebPageMetaRobots.
		/// </summary>
		public string WebPageMetaRobots { get; set; }


		/// <summary>
		/// WebPageCanonicalURL.
		/// </summary>
		public string WebPageCanonicalURL { get; set; }


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