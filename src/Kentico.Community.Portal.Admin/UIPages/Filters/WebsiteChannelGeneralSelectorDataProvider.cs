using CMS.ContentEngine;
using CMS.DataEngine;
using CSharpFunctionalExtensions;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Community.Portal.Admin.UIPages;


public class WebsiteChannelGeneralSelectorDataProvider(IInfoProvider<ChannelInfo> channelProvider)
    : IGeneralSelectorDataProvider
{
    private readonly IInfoProvider<ChannelInfo> channelProvider = channelProvider;

    private static ObjectSelectorListItem<string> InvalidItem => new() { IsValid = false, Text = "Inavlid", Value = "" };

    public async Task<PagedSelectListItems<string>> GetItemsAsync(string searchTerm, int pageIndex, CancellationToken cancellationToken)
    {
        var items = await channelProvider.Get()
            .WhereEquals(nameof(ChannelInfo.ChannelType), ChannelType.Website.ToString())
            .GetEnumerableTypedResultAsync();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            items = items.Where(i => i.ChannelDisplayName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return new PagedSelectListItems<string>()
        {
            NextPageAvailable = false,
            Items = items.Select(i => new ObjectSelectorListItem<string> { IsValid = true, Text = i.ChannelDisplayName, Value = i.ChannelName }),
        };
    }

    public async Task<IEnumerable<ObjectSelectorListItem<string>>> GetSelectedItemsAsync(IEnumerable<string> selectedValues, CancellationToken cancellationToken)
    {
        var items = await channelProvider.Get()
            .WhereEquals(nameof(ChannelInfo.ChannelType), ChannelType.Website.ToString())
            .GetEnumerableTypedResultAsync();

        return (selectedValues ?? []).Select(GetSelectedItemByValue(items));

    }

    private Func<string, ObjectSelectorListItem<string>> GetSelectedItemByValue(IEnumerable<ChannelInfo> websiteChannelContentTypes) =>
        (string channelName) => websiteChannelContentTypes
            .TryFirst(c => string.Equals(c.ChannelName, channelName))
            .Map(c => new ObjectSelectorListItem<string> { IsValid = true, Text = c.ChannelDisplayName, Value = c.ChannelName })
            .GetValueOrDefault(InvalidItem);
}
