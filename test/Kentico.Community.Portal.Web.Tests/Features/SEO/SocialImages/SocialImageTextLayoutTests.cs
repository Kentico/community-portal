using Kentico.Community.Portal.Web.Features.SEO.SocialImages;

namespace Kentico.Community.Portal.Web.Tests.Features.SEO.SocialImages;

public class SocialImageTextLayoutTests
{
    /// <summary>
    /// 1px per character at size 1, so widths are easy to reason about
    /// </summary>
    private static double Measure(string text, double size) => text.Length * size;

    [Test]
    public void Wraps_On_Word_Boundaries()
    {
        var lines = SocialImageTextLayout.Wrap("one two three four", 9, 10, s => Measure(s, 1));

        Assert.That(lines, Is.EqualTo(new[] { "one two", "three", "four" }));
    }

    [Test]
    public void Keeps_Overlong_Word_Whole() =>
        Assert.That(
            SocialImageTextLayout.Wrap("a supercalifragilistic b", 5, 10, s => Measure(s, 1)),
            Is.EqualTo(new[] { "a", "supercalifragilistic", "b" }));

    [Test]
    public void Truncates_With_Ellipsis_Past_Max_Lines()
    {
        var lines = SocialImageTextLayout.Wrap("one two three four five six", 9, 2, s => Measure(s, 1));

        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Count.EqualTo(2));
            Assert.That(lines[^1], Does.EndWith(SocialImageTextLayout.ELLIPSIS));
            Assert.That(lines.Select(l => Measure(l, 1)), Has.All.LessThanOrEqualTo(9));
        });
    }

    [Test]
    public void Picks_Largest_Size_That_Fits()
    {
        var block = SocialImageTextLayout.Fit("one two three", [3, 2, 1], 13, 1, Measure);

        Assert.Multiple(() =>
        {
            Assert.That(block.PointSize, Is.EqualTo(1));
            Assert.That(block.Lines, Is.EqualTo(new[] { "one two three" }));
        });
    }

    [Test]
    public void Steps_Down_Size_When_Block_Exceeds_Max_Height()
    {
        // At size 2 the text wraps to 3 lines (height 6), over the limit of 5
        var block = SocialImageTextLayout.Fit("one two three", [2, 1], 13, 3, Measure, maxHeight: 5);

        Assert.Multiple(() =>
        {
            Assert.That(block.PointSize, Is.EqualTo(1));
            Assert.That(block.Lines, Is.EqualTo(new[] { "one two three" }));
        });
    }

    [Test]
    public void Falls_Back_To_Smallest_Size_Truncated()
    {
        var block = SocialImageTextLayout.Fit("one two three four five six seven", [3, 2], 20, 1, Measure);

        Assert.Multiple(() =>
        {
            Assert.That(block.PointSize, Is.EqualTo(2));
            Assert.That(block.Lines, Has.Count.EqualTo(1));
            Assert.That(block.Lines[0], Does.EndWith(SocialImageTextLayout.ELLIPSIS));
        });
    }

    [Test]
    public void Collapses_Whitespace() =>
        Assert.That(
            SocialImageTextLayout.Wrap("  one \n\t two  ", 100, 10, s => Measure(s, 1)),
            Is.EqualTo(new[] { "one two" }));
}
