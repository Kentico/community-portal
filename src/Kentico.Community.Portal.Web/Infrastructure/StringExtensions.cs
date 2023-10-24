using System.Text;

namespace Kentico.Community.Portal.Web.Infrastructure;

public static class StringExtensions
{
    public static string? RemoveRepeatedCharacters(this string? input, char duplicate)
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
