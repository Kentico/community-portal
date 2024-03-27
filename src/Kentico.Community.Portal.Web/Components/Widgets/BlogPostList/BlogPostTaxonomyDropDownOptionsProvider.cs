using Kentico.Community.Portal.Web.Features.Blog;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using MediatR;

namespace Kentico.Community.Portal.Web.Components.Widgets.BlogPostList;

public class BlogPostTaxonomyDropDownOptionsProvider(IMediator mediator) : IDropDownOptionsProvider
{
    private readonly IMediator mediator = mediator;

    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var res = await mediator.Send(new BlogPostTaxonomiesQuery());

        return res.Items.Select(i => new DropDownOptionItem() { Text = i.DisplayName, Value = i.Value });
    }
}
