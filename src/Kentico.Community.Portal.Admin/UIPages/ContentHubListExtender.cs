using CMS.ContentEngine.Internal;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: PageExtender(typeof(ContentHubListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class ContentHubListExtender : PageExtender<ContentHubList>
{
    public override Task ConfigurePage()
    {
        _ = base.ConfigurePage();

        var configs = Page.PageConfiguration.ColumnConfigurations;

        /*
         * If this column is added to the default configuration in the future
         * we don't want to override it
         */
        if (configs.FirstOrDefault(c => string.Equals(c.Name, nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataCreatedWhen), StringComparison.OrdinalIgnoreCase)) is null)
        {
            configs
                .TryFirst(c => string.Equals(c.Name, nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataModifiedWhen), StringComparison.OrdinalIgnoreCase))
                .Execute(c =>
                {
                    int index = configs.IndexOf(c);

                    configs.Insert(index, new ColumnConfiguration
                    {
                        Name = nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataCreatedWhen),
                        Caption = "Created",
                        MinWidth = 19,
                        MaxWidth = 50,
                        Sorting = new SortingConfiguration
                        {
                            Sortable = true,
                            DefaultDirection = SortTypeEnum.Desc
                        },
                    });
                });
        }

        return Task.CompletedTask;
    }
}

