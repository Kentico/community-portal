using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Infrastructure;

public record WebsiteSettingsContentQuery : IQuery<WebsiteSettingsContentQueryResponse>;
public record WebsiteSettingsContentQueryResponse(WebsiteSettingsContent Settings);

public class WebsiteSettingsContentQueryHandler : ContentItemQueryHandler<WebsiteSettingsContentQuery, WebsiteSettingsContentQueryResponse>
{
    public WebsiteSettingsContentQueryHandler(ContentItemQueryTools tools) : base(tools) { }

    public override async Task<WebsiteSettingsContentQueryResponse> Handle(WebsiteSettingsContentQuery request, CancellationToken cancellationToken)
    {
        var b = new ContentItemQueryBuilder().ForContentType(WebsiteSettingsContent.CONTENT_TYPE_NAME);

        var r = await Executor.GetResult(b, ContentItemMapper.Map<WebsiteSettingsContent>, DefaultQueryOptions, cancellationToken);

        return new(r.First());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(WebsiteSettingsContentQuery query, WebsiteSettingsContentQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllContentItems(WebsiteSettingsContent.CONTENT_TYPE_NAME);
}
