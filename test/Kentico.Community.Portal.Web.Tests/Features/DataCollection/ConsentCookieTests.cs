using Kentico.Community.Portal.Web.Features.DataCollection;

namespace Kentico.Community.Portal.Web.Tests.Features.DataCollection;

public class ConsentCookieTests
{
    [Test]
    public void Format_Writes_Version_Flags_And_Unix_Seconds()
    {
        var sut = new ConsentCookie(true, false, DateTimeOffset.FromUnixTimeSeconds(1760486400));

        Assert.That(sut.Format(), Is.EqualTo("v2|a=1|t=0|1760486400"));
    }

    [Test]
    public void Parse_Reads_A_Formatted_Value()
    {
        var choice = new ConsentCookie(false, true, DateTimeOffset.FromUnixTimeSeconds(1760486400));

        Assert.That(ConsentCookie.Parse(choice.Format()), Is.EqualTo(choice));
    }

    [Test]
    public void Provisional_Round_Trips_With_Read_Only_Marker()
    {
        var choice = new ConsentCookie(true, false, DateTimeOffset.FromUnixTimeSeconds(1760486400), Provisional: true);

        Assert.Multiple(() =>
        {
            Assert.That(choice.Format(), Is.EqualTo("v2|a=1|t=0|1760486400|ro=1"));
            Assert.That(ConsentCookie.Parse(choice.Format()), Is.EqualTo(choice));
            Assert.That(ConsentCookie.Parse("v2|a=1|t=0|1760486400")!.Provisional, Is.False);
        });
    }

    [Test]
    public void Pending_Revoke_Round_Trips_With_Contact_Guid()
    {
        var contact = Guid.Parse("8b0bd2f4-4c7e-4d0c-9d2c-5b1d5a3f1e01");
        var choice = new ConsentCookie(false, false, DateTimeOffset.FromUnixTimeSeconds(1760486400), PendingRevoke: contact);

        Assert.Multiple(() =>
        {
            Assert.That(choice.Format(), Is.EqualTo("v2|a=0|t=0|1760486400|rv=8b0bd2f4-4c7e-4d0c-9d2c-5b1d5a3f1e01"));
            Assert.That(ConsentCookie.Parse(choice.Format()), Is.EqualTo(choice));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("4")]
    [TestCase("v1|a=1|t=1|1760486400")]
    [TestCase("v2|a=1|t=1")]
    [TestCase("v2|a=2|t=1|1760486400")]
    [TestCase("v2|t=1|a=1|1760486400")]
    [TestCase("v2|a=1|t=1|soon")]
    [TestCase("v2|a=1|t=0|1760486400|ro=0")]
    [TestCase("v2|a=0|t=0|1760486400|rv=nope")]
    [TestCase("v2|a=1|t=0|1760486400|ro=1|x")]
    // Tampered timestamps: past DateTimeOffset.MaxValue, long overflow, signed, non-ASCII digits
    [TestCase("v2|a=1|t=1|253402300800")]
    [TestCase("v2|a=1|t=1|9223372036854775807")]
    [TestCase("v2|a=1|t=1|99999999999999999999")]
    [TestCase("v2|a=1|t=1|-1")]
    [TestCase("v2|a=1|t=1|+1760486400")]
    [TestCase("v2|a=1|t=1| 1760486400")]
    [TestCase("v2|a=1|t=1|١٧٦٠٤٨٦٤٠٠")]
    public void Parse_Returns_Null_For_Missing_Legacy_Or_Malformed_Values(string? value) =>
        Assert.That(ConsentCookie.Parse(value), Is.Null);

    [TestCase("v2|a=1|t=1|0")]
    [TestCase("v2|a=1|t=1|253402300799")]
    public void Parse_Accepts_Timestamps_At_The_Range_Limits(string value) =>
        Assert.That(ConsentCookie.Parse(value), Is.Not.Null);

    [TestCase("v2|a=1|t=1|253402300800")]
    [TestCase("v2|a=1|t=1|9223372036854775807")]
    [TestCase("v2||||")]
    [TestCase("|||")]
    [TestCase("v2|a=1|t=1|1760486400|rv=")]
    [TestCase("v2|a=1|t=1|1760486400|rv=00000000-0000-0000-0000-000000000000x")]
    public void Parse_Never_Throws(string value) =>
        Assert.DoesNotThrow(() => ConsentCookie.Parse(value));
}
