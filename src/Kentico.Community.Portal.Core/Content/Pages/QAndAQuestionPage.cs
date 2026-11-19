using Kentico.Community.Portal.Core.Modules;

namespace Kentico.Community.Portal.Core.Content;

public partial class QAndAQuestionPage
{
    public static Guid CONTENT_TYPE_GUID { get; } = new Guid("c014abca-c95e-4f1e-8bea-9821bee891fc");

    /// <summary>
    /// True for the discussion auto-created alongside a blog post (tagged with the Blog discussion type)
    /// </summary>
    public bool IsBlogDiscussion() =>
        QAndAQuestionPageDiscussionType?.Any(t => t.Identifier == SystemTaxonomies.QAndADiscussionTypeTaxonomy.BlogTag.GUID) ?? false;
}
