using System.Text;
using System.Text.RegularExpressions;

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

    private static string? RemoveRepeatedCharacters(this string? input, char duplicate)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var sb = new StringBuilder(input.Length);
        char? current = null;

        for (int i = 0; i < input.Length; i++)
        {
            char ci = input[i];
            if (ci == current && duplicate == ci)
            {
                continue;
            }
            else
            {
                _ = sb.Append(ci);
                current = ci;
            }
        }

        return sb.ToString();
    }
}
