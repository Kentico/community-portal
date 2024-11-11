using System.Diagnostics.Contracts;

namespace System.Linq;

public static class CollectionExtensions
{
    [Pure]
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> o) where T : class =>
        o.Where(x => x != null)!;

    [Pure]
    public static IEnumerable<T> WHereNotNull<T>(this IEnumerable<T?> enumerable) where T : struct =>
        enumerable.Where(e => e.HasValue).Select(e => e!.Value);
}
