namespace Kentico.Community.Portal.Web.Features.SEO.SocialImages;

/// <summary>
/// Word wrapping and font sizing for social image text, independent of the rendering library
/// </summary>
public static class SocialImageTextLayout
{
    public const string ELLIPSIS = "…";

    /// <summary>
    /// Picks the largest size in <paramref name="pointSizes"/> (largest first) whose wrap fits in
    /// <paramref name="maxLines"/> without truncation. Falls back to the smallest size, truncated.
    /// </summary>
    /// <param name="measure">Text width in pixels at a given point size</param>
    /// <param name="maxHeight">Optional block height limit, as line count × point size × <paramref name="lineHeight"/></param>
    public static TextBlock Fit(
        string text,
        IReadOnlyList<double> pointSizes,
        double maxWidth,
        int maxLines,
        Func<string, double, double> measure,
        double maxHeight = double.MaxValue,
        double lineHeight = 1)
    {
        if (pointSizes.Count == 0)
        {
            throw new ArgumentException("At least one point size is required", nameof(pointSizes));
        }

        foreach (double size in pointSizes)
        {
            var lines = Wrap(text, maxWidth, int.MaxValue, s => measure(s, size));
            if (lines.Count <= maxLines && lines.Count * size * lineHeight <= maxHeight)
            {
                return new(size, lines);
            }
        }

        double smallest = pointSizes[^1];
        return new(smallest, Wrap(text, maxWidth, maxLines, s => measure(s, smallest)));
    }

    /// <summary>
    /// Greedy word wrap. Words wider than <paramref name="maxWidth"/> are kept whole on their own line.
    /// Text beyond <paramref name="maxLines"/> is dropped and the last line ends in <see cref="ELLIPSIS"/>.
    /// </summary>
    public static IReadOnlyList<string> Wrap(string text, double maxWidth, int maxLines, Func<string, double> measure)
    {
        string[] words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        string current = "";

        foreach (string word in words)
        {
            string candidate = current.Length == 0 ? word : $"{current} {word}";
            if (current.Length == 0 || measure(candidate) <= maxWidth)
            {
                current = candidate;
                continue;
            }

            lines.Add(current);
            current = word;
        }

        if (current.Length > 0)
        {
            lines.Add(current);
        }

        if (lines.Count <= maxLines)
        {
            return lines;
        }

        var kept = lines.Take(maxLines).ToList();
        kept[^1] = Truncate(kept[^1], maxWidth, measure);
        return kept;
    }

    private static string Truncate(string line, double maxWidth, Func<string, double> measure)
    {
        string result = line;
        while (result.Length > 0 && measure(result + ELLIPSIS) > maxWidth)
        {
            int lastSpace = result.LastIndexOf(' ');
            result = lastSpace > 0 ? result[..lastSpace] : result[..^1];
        }

        return result.TrimEnd() + ELLIPSIS;
    }
}

/// <param name="PointSize">Chosen font size</param>
/// <param name="Lines">Wrapped lines, in order</param>
public record TextBlock(double PointSize, IReadOnlyList<string> Lines);
