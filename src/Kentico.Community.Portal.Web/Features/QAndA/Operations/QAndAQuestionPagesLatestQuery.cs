using CMS.ContentEngine;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionPagesLatestQuery(int Count, string ChannelName) : IQuery<QAndAQuestionPagesLatestQueryResponse>, ICacheByValueQuery, IChannelContentQuery
{
    public string CacheValueKey => Count.ToString();
}
public record QAndAQuestionPagesLatestQueryResponse(IReadOnlyList<QAndAQuestionPage> Items);
public class QAndAQuestionPagesLatestQueryHandler(WebPageQueryTools tools) : WebPageQueryHandler<QAndAQuestionPagesLatestQuery, QAndAQuestionPagesLatestQueryResponse>(tools)
{
    public override async Task<QAndAQuestionPagesLatestQueryResponse> Handle(QAndAQuestionPagesLatestQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder()
            .ForContentTypes(
                q => q
                .ForWebsite(request.ChannelName)
                .WithContentTypeFields())
            .Parameters(
                q => q
                .Where(w => w.WhereContainsTags(nameof(QAndAQuestionPage.QAndAQuestionPageDiscussionType), [SystemTaxonomies.QAndADiscussionTypeTaxonomy.Question.TagGUID]))
                .OrderBy(new[] { new OrderByColumn(nameof(QAndAQuestionPage.QAndAQuestionPageDateCreated), OrderDirection.Descending) })
                .TopN(request.Count));

        var pages = await Executor.GetMappedWebPageResult<QAndAQuestionPage>(b, DefaultQueryOptions, cancellationToken);

        return new(pages.ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(QAndAQuestionPagesLatestQuery query, QAndAQuestionPagesLatestQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllContentItems(QAndAQuestionPage.CONTENT_TYPE_NAME);
}
