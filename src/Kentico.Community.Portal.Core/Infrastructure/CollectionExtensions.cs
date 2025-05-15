using System.Diagnostics.Contracts;

namespace System.Linq;

public static class CollectionExtensions
{
    [Pure]
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> o) where T : class =>
        o.Where(x => x != null)!;

    [Pure]
    public static IEnumerable<string> WhereNotNullOrWhiteSpace(this IEnumerable<string?> o) =>
        o.Where(x => !string.IsNullOrWhiteSpace(x))!;

    [Pure]
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : struct =>
        enumerable.Where(e => e.HasValue).Select(e => e!.Value);

    [Pure]
    public static IEnumerable<T> If<T>(this IEnumerable<T> items, bool condition,
        Func<IEnumerable<T>, IEnumerable<T>> projectionIf,
        Func<IEnumerable<T>, IEnumerable<T>>? projectionElse) =>
        condition
            ? projectionIf(items)
            : projectionElse is not null
                ? projectionElse(items)
                : items;

    [Pure]
    public static IEnumerable<T> OrderByDirection<T>(this IEnumerable<T> o, Func<T, string> keySelector, OrderByDirections orderByDirection) =>
        orderByDirection switch
        {
            OrderByDirections.DESC => o.OrderByDescending(keySelector),
            OrderByDirections.ASC or _ => o.OrderBy(keySelector),
        };
}

public enum OrderByDirections
{
    ASC,
    DESC
}
