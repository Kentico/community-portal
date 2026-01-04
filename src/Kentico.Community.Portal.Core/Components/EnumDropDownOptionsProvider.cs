using System.ComponentModel;
using EnumsNET;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Community.Portal.Core.Components;

public class EnumDropDownOptionsProvider<T> : IDropDownOptionsProvider
    where T : struct, Enum
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var results = Enums
            .GetMembers<T>(EnumMemberSelection.All)
            .Select(e =>
            {
                string text = e.Attributes.OfType<DescriptionAttribute>().FirstOrDefault()?.Description ?? e.Name;

                return new DropDownOptionItem { Value = e.Value.ToString(), Text = text };
            });

        return Task.FromResult(results.AsEnumerable());
    }

    public static T Parse(string rawValue, T defaultValue) =>
        Enums.TryParse<T>(rawValue, true, out var result)
            ? result
            : defaultValue;
}
