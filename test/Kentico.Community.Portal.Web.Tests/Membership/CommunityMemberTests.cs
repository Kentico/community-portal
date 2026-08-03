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
