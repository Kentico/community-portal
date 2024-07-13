namespace CSharpFunctionalExtensions;

public static class MaybeCommunityExtensions
{
    /// <summary>
    /// Binds the <see cref="Maybe"> to <see cref="Maybe.None"/> if the inner value is null, empty, or whitespace
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Maybe<string> WithNullOrWhiteSpaceAsNone(this Maybe<string> value) =>
        value.Bind(v => string.IsNullOrWhiteSpace(v) ? Maybe.None : Maybe.From(v));
}
