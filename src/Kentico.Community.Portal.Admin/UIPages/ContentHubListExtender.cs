using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CSharpFunctionalExtensions;
using Kentico.Community.Portal.Admin.UIPages;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

[assembly: PageExtender(typeof(ContentHubListExtender))]

namespace Kentico.Community.Portal.Admin.UIPages;

public class ContentHubListExtender : PageExtender<ContentHubList>
{
    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

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
                    c.Caption = "Modified";
                    c.MinWidth = 15;
                    c.MaxWidth = 20;

                    int index = configs.IndexOf(c);

                    configs.Insert(index, new ColumnConfiguration
                    {
                        Name = nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataCreatedWhen),
                        Caption = "Created",
                        MinWidth = 15,
                        MaxWidth = 20,
                        Sorting = new SortingConfiguration
                        {
                            Sortable = true,
                            DefaultDirection = SortTypeEnum.Desc
                        },
                    });
                });
        }

        configs.TryFirst(c => string.Equals(c.Name, nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataDisplayName), StringComparison.OrdinalIgnoreCase))
            .Execute(c => c.MinWidth = 40);
        configs.TryFirst(c => string.Equals(c.Name, nameof(ContentItemInfo.ContentItemIsSecured), StringComparison.OrdinalIgnoreCase))
            .Execute(c =>
            {
                c.Caption = "Secured";
                c.MinWidth = 5;
                c.MaxWidth = 5;
            });

        if (configs.FirstOrDefault(c => string.Equals(c.Name, nameof(ContentFolderInfo.ContentFolderTreePath), StringComparison.OrdinalIgnoreCase)) is { } pathConfig &&
            configs.FirstOrDefault(c => string.Equals(c.Name, nameof(ContentItemInfo.ContentItemIsSecured), StringComparison.OrdinalIgnoreCase)) is { } securedConfig)
        {
            int pathIndex = configs.IndexOf(pathConfig);
            int securedIndex = configs.IndexOf(securedConfig);

            configs[pathIndex] = securedConfig;
            configs[securedIndex] = pathConfig;
        }
    }
}

