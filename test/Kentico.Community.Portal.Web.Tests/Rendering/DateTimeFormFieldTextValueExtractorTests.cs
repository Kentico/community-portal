using Kentico.Community.Portal.Web.Rendering;

namespace Kentico.Community.Portal.Web.Tests.Rendering;

public class DateTimeFormFieldTextValueExtractorTests
{
    [Test]
    public async Task Extract_Formats_Date_Using_Culture_Invariant_Pattern()
    {
        var sut = new DateTimeFormFieldTextValueExtractor();

        string result = await sut.Extract(new DateTime(2026, 5, 11), null!);

        Assert.That(result, Is.EqualTo("2026/05/11"));
    }

    [Test]
    public async Task Extract_Formats_Date_And_Time_Using_24_Hour_Pattern()
    {
        var sut = new DateTimeFormFieldTextValueExtractor();

        string result = await sut.Extract(new DateTime(2026, 5, 11, 18, 7, 30), null!);

        Assert.That(result, Is.EqualTo("2026/05/11 18:07:30"));
    }
}
