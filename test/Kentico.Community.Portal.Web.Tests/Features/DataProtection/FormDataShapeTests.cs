using Kentico.Community.Portal.Core.DataProtection;

namespace Kentico.Community.Portal.Web.Tests.Features.DataProtection;

public class FormDataShapeTests
{
    [Test]
    public void Detects_Member_Fields_By_Component()
    {
        var shape = FormDataShape.Detect(
        [
            ("AuthorID", FormDataShape.MEMBER_ID_COMPONENT),
            ("AuthorEmail", FormDataShape.MEMBER_EMAIL_COMPONENT),
            ("Notes", "Kentico.TextArea"),
        ], null);

        Assert.Multiple(() =>
        {
            Assert.That(shape.MemberIDColumn, Is.EqualTo("AuthorID"));
            Assert.That(shape.MemberEmailColumn, Is.EqualTo("AuthorEmail"));
            Assert.That(shape.EmailColumns, Is.Empty);
            Assert.That(shape.HasPersonalIdentifiers, Is.True);
        });
    }

    [Test]
    public void Detects_Member_Fields_By_Name_When_Component_Differs()
    {
        // A poll created before the hidden member components existed
        var shape = FormDataShape.Detect([("MemberID", "Kentico.TextInput"), ("memberemail", null)], null);

        Assert.Multiple(() =>
        {
            Assert.That(shape.MemberIDColumn, Is.EqualTo("MemberID"));
            Assert.That(shape.MemberEmailColumn, Is.EqualTo("memberemail"));
        });
    }

    [Test]
    public void Detects_Email_Inputs_And_The_Contact_Email_Mapping()
    {
        var shape = FormDataShape.Detect(
        [
            ("CompanyEmail", FormDataShape.EMAIL_COMPONENT),
            ("ContactAddress", "Kentico.TextInput"),
        ], contactEmailMappedField: "contactaddress");

        Assert.That(shape.EmailColumns, Is.EqualTo(new[] { "CompanyEmail", "ContactAddress" }));
    }

    [Test]
    public void Does_Not_Repeat_A_Mapped_Email_Input()
    {
        var shape = FormDataShape.Detect([("Email", FormDataShape.EMAIL_COMPONENT)], contactEmailMappedField: "email");

        Assert.That(shape.EmailColumns, Is.EqualTo(new[] { "Email" }));
    }

    [Test]
    public void Ignores_A_Mapping_To_A_Missing_Field()
    {
        var shape = FormDataShape.Detect([("Feedback", "Kentico.TextArea")], contactEmailMappedField: "Email");

        Assert.Multiple(() =>
        {
            Assert.That(shape.EmailColumns, Is.Empty);
            Assert.That(shape.HasPersonalIdentifiers, Is.False);
        });
    }

    [Test]
    public void AllEmailColumns_Puts_Member_Email_First_Without_Duplicates()
    {
        var shape = new FormDataShape("MemberID", "MemberEmail", ["Email", "memberemail"]);

        Assert.That(shape.AllEmailColumns, Is.EqualTo(new[] { "MemberEmail", "Email" }));
    }

    [Test]
    public void ErasedEmail_Is_Unique_Per_Member_And_Non_Routable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ErasureOptionKeys.ErasedEmail(42), Is.EqualTo("erased-42@erased.invalid"));
            Assert.That(ErasureOptionKeys.ErasedEmail(0), Is.EqualTo("erased@erased.invalid"));
        });
    }
}
