using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Kentico.Community.Portal.Core.Modules;

public class TaxonomyTag
{
    public Guid Guid { get; set; }
    public string Name { get; set; }
    public string NormalizedName { get; set; }
    public string DisplayName { get; set; }
    public IReadOnlyList<TaxonomyTag> Children { get; }

    public TaxonomyTag(Tag tag, FrozenDictionary<int, ImmutableList<Tag>> tagsByParentID)
    {
        Guid = tag.Identifier;
        Name = tag.Name;
        NormalizedName = RegexTools.AlphanumericRegex().Replace(tag.Name, "").ToLowerInvariant();
        DisplayName = tag.Title;
        Children = tagsByParentID.TryGetValue(tag.ID, out var children)
            ? children.Select(t => new TaxonomyTag(t, tagsByParentID)).OrderBy(t => t.DisplayName).ToList()
            : [];
    }
}

public partial class RegexTools
{
    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    public static partial Regex AlphanumericRegex();
}

