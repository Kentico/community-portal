namespace CSharpFunctionalExtensions;

public static class MaybeCommunityExtensions
{
    /// <summary>
    /// Binds the <see cref="Maybe"> to <see cref="Maybe.None"/> if the inner value is null, empty, or whitespace
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Maybe<string> MapNullOrWhiteSpaceAsNone(this Maybe<string> value) =>
        value.Bind(v => string.IsNullOrWhiteSpace(v) ? Maybe.None : Maybe.From(v));

    /// <summary>
    /// Accepts a <paramref name="fallback"/> <see cref="Maybe{}"/> which is used if <paramref name="value"/> has no value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="fallback"></param>
    /// <returns></returns>
    public static Maybe<T> IfNoValue<T>(this Maybe<T> value, Maybe<T> fallback) =>
        value.Match(v => v, () => fallback);

    /// <summary>
    /// Awaits the <paramref name="sourceTask"/> and if the source contains any items
    /// returns the first as a "Some" otherwise a "None"
    /// </summary>
    /// <param name="sourceTask"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Maybe<T>> TryFirst<T>(this Task<IEnumerable<T>> sourceTask)
    {
        var source = await sourceTask;

        var item = source.FirstOrDefault();

        return item is null
            ? Maybe.None
            : item;
    }
}
