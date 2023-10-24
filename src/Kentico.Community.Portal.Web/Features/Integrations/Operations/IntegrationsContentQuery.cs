using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.Integrations;

public record IntegrationContentsQuery() : IQuery<IntegrationContentsQueryResponse>;
public record IntegrationContentsQueryResponse(IReadOnlyList<IntegrationContent> Items);
public class IntegrationContentsQueryHandler : ContentItemQueryHandler<IntegrationContentsQuery, IntegrationContentsQueryResponse>
{
    public IntegrationContentsQueryHandler(ContentItemQueryTools tools) : base(tools) { }

    public override async Task<IntegrationContentsQueryResponse> Handle(IntegrationContentsQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(IntegrationContent.CONTENT_TYPE_NAME);

        var r = await Executor.GetWebPageResult(b, c => ContentItemMapper.Map<IntegrationContent>(c), DefaultQueryOptions, cancellationToken);

        return new(r.ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(IntegrationContentsQuery query, IntegrationContentsQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllContentItems(IntegrationContent.CONTENT_TYPE_NAME);
}
