using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Tests.Membership;

public class CommunityMemberTests
{
    [TestCase("", "", "testuser", "testuser",
        TestName = "DisplayName_Returns_UserName_When_FirstAndLastName_Empty")]
    [TestCase("Jane", "", "testuser", "Jane",
        TestName = "DisplayName_Returns_FirstName_When_Only_FirstName_Set")]
    [TestCase("", "Smith", "testuser", "Smith",
        TestName = "DisplayName_Returns_LastName_When_Only_LastName_Set")]
    [TestCase("Jane", "Smith", "testuser", "Jane Smith",
        TestName = "DisplayName_Returns_FullName_When_BothNames_Set")]
    public void DisplayName_Returns_Expected_Value(
        string firstName, string lastName, string userName, string expectedDisplayName)
    {
        // Arrange
        var sut = new CommunityMember
        {
            FirstName = firstName,
            LastName = lastName,
            UserName = userName
        };

        // Act
        string displayName = sut.DisplayName;

        // Assert
        Assert.That(displayName, Is.EqualTo(expectedDisplayName));
    }

    [Test]
    public void DisplayName_Returns_Empty_String_When_All_Names_Empty()
    {
        // Arrange
        var sut = new CommunityMember
        {
            FirstName = "",
            LastName = "",
            UserName = null
        };

        // Act
        string displayName = sut.DisplayName;

        // Assert
        Assert.That(displayName, Is.EqualTo(""));
    }
}

public class CommunityMemberIsInternalEmployeeTests
{
    [Test]
    public void IsInternalEmployee_Is_True_For_Confirmed_KenticoComEmail()
    {
        var sut = new CommunityMember { Email = "person@kentico.com", EmailConfirmed = true };

        Assert.That(sut.IsInternalEmployee, Is.True);
    }

    [TestCase("Person@Kentico.com", TestName = "IsInternalEmployee_Is_True_Regardless_Of_Email_Casing")]
    [TestCase("person@KENTICO.COM", TestName = "IsInternalEmployee_Is_True_Regardless_Of_Domain_Casing")]
    public void IsInternalEmployee_Ignores_Case(string email)
    {
        var sut = new CommunityMember { Email = email, EmailConfirmed = true };

        Assert.That(sut.IsInternalEmployee, Is.True);
    }

    [Test]
    public void IsInternalEmployee_Is_False_When_Email_Not_Confirmed()
    {
        var sut = new CommunityMember { Email = "person@kentico.com", EmailConfirmed = false };

        Assert.That(sut.IsInternalEmployee, Is.False);
    }

    [Test]
    public void IsInternalEmployee_Is_False_For_NonKenticoComEmail()
    {
        var sut = new CommunityMember { Email = "person@example.com", EmailConfirmed = true };

        Assert.That(sut.IsInternalEmployee, Is.False);
    }

    [Test]
    public void IsInternalEmployee_Is_False_When_Email_Is_Null()
    {
        var sut = new CommunityMember { Email = null, EmailConfirmed = true };

        Assert.That(sut.IsInternalEmployee, Is.False);
    }
}

public class CommunityMemberAlternateEmailTests
{
    [Test]
    public void VerifiedAlternateEmail_Is_Null_When_Not_Verified()
    {
        var sut = new CommunityMember { AlternateEmail = "jane@agency.com", AlternateEmailVerifiedDate = null };

        Assert.That(sut.IsAlternateEmailVerified, Is.False);
        Assert.That(sut.VerifiedAlternateEmail, Is.Null);
    }

    [Test]
    public void VerifiedAlternateEmail_Is_Email_When_Verified()
    {
        var sut = new CommunityMember { AlternateEmail = "jane@agency.com", AlternateEmailVerifiedDate = DateTime.Now };

        Assert.That(sut.IsAlternateEmailVerified, Is.True);
        Assert.That(sut.VerifiedAlternateEmail, Is.EqualTo("jane@agency.com"));
    }

    [Test]
    public void VerifiedAlternateEmail_Is_Null_When_Email_Empty_Even_With_Date()
    {
        var sut = new CommunityMember { AlternateEmail = "", AlternateEmailVerifiedDate = DateTime.Now };

        Assert.That(sut.VerifiedAlternateEmail, Is.Null);
    }
}
