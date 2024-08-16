using CMS.DataEngine;
using CSharpFunctionalExtensions;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Community.Portal.Admin.UIPages;

public class ContentTypeGeneralSelectorDataProvider
    : IGeneralSelectorDataProvider
{
    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Inavlid", Value = "" };

    public async Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        var items = await DataClassInfoProvider.GetClasses()
            .WhereEquals(nameof(DataClassInfo.ClassContentTypeType), ClassContentTypeType.WEBSITE)
            .GetEnumerableTypedResultAsync();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            items = items.Where(i => i.ClassDisplayName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return new PagedSelectListItems<string>()
        {
            NextPageAvailable = false,
            Items = items.Select(i => new ObjectSelectorListItem<string> { IsValid = true, Text = i.ClassDisplayName, Value = i.ClassName }),
        };
    }

    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(IEnumerable<string> selectedValues, CancellationToken cancellationToken)
    {
        var items = await DataClassInfoProvider.GetClasses()
            .WhereEquals(nameof(DataClassInfo.ClassContentTypeType), ClassContentTypeType.WEBSITE)
            .GetEnumerableTypedResultAsync();

        return (selectedValues ?? []).Select(GetSelectedItemByValue(items));

    }

    private Func<string, ObjectSelectorListItem<string>> GetSelectedItemByValue(IEnumerable<DataClassInfo> websiteChannelContentTypes) =>
        (string className) => websiteChannelContentTypes
            .TryFirst(c => string.Equals(c.ClassName, className))
            .Map(c => new ObjectSelectorListItem<string> { IsValid = true, Text = c.ClassDisplayName, Value = c.ClassName })
            .GetValueOrDefault(InvalidItem);

}
