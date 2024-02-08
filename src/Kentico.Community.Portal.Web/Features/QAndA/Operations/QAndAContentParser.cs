using System.Text.RegularExpressions;
using Kentico.Community.Portal.Web.Infrastructure;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public static partial class QandAContentParser
{
    [GeneratedRegex(@"[^a-zA-Z0-9\d]")]
    private static partial Regex AlphanumericRegex();

    /// <summary>
    /// Replaces all occurrences of non-alphanumeric characeters in <paramref name="content"/> with a '-' and
    /// converts multiple '-' in a row to a single '-'
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static string Alphanumeric(string content) => AlphanumericRegex().Replace(content, "-").RemoveRepeatedCharacters('-') ?? "";
}
