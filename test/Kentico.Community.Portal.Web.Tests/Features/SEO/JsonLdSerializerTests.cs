using System.Text.Json;
using Kentico.Community.Portal.Web.Features.SEO.StructuredData;

namespace Kentico.Community.Portal.Web.Tests.Features.SEO;

public class JsonLdSerializerTests
{
    [Test]
    public void Organization_Serializes_Context_Type_And_Properties()
    {
        string json = JsonLdSerializer.Serialize(
            new OrganizationJsonLd("Kentico", "https://example.com/", "https://example.com/img/logo.svg"));

        Assert.That(json, Is.EqualTo(
            """{"@context":"https://schema.org","@type":"Organization","name":"Kentico","url":"https://example.com/","logo":"https://example.com/img/logo.svg"}"""));
    }

    [Test]
    public void Output_Cannot_Close_The_Script_Element()
    {
        string name = "</script><script>alert('x')</script> & <!--";

        string json = JsonLdSerializer.Serialize(new OrganizationJsonLd(name, "https://example.com/", "https://example.com/logo.svg"));

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Not.Contain("<"));
            Assert.That(json, Does.Not.Contain(">"));
            Assert.That(json, Does.Not.Contain("&"));
            Assert.That(JsonDocument.Parse(json).RootElement.GetProperty("name").GetString(), Is.EqualTo(name));
        });
    }
}
