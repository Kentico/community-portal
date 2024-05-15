using CMS.ContentEngine.Internal;
using CMS.Helpers;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: PageExtender(typeof(ReusableFieldSchemaListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class ReusableFieldSchemaListExtender(IReusableFieldSchemaManager schemaManager)
    : PageExtender<ReusableFieldSchemaList>
{
    private readonly IReusableFieldSchemaManager schemaManager = schemaManager;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var configs = Page.PageConfiguration.ColumnConfigurations;

        configs
            .TryFirst(c => string.Equals(c.Name, nameof(ReusableFieldSchema.DisplayName), StringComparison.OrdinalIgnoreCase))
            .Execute(c => c.MaxWidth = 35);
        configs
            .TryFirst(c => string.Equals(c.Name, nameof(ReusableFieldSchema.Name), StringComparison.OrdinalIgnoreCase))
            .Execute(c => c.MaxWidth = 35);

        _ = configs.AddColumn("usedByContentTypes", "Used by", minWidth: 50, maxWidth: 100, formatter: (data, dc) =>
        {
            var schemaGUID = ValidationHelper.GetGuid(dc[nameof(ReusableFieldSchema.Guid)], default);
            var contentTypes = schemaManager.GetContentTypesWithSchema(schemaGUID) ?? [];

            return string.Join(", ", contentTypes);
        });
    }
}
