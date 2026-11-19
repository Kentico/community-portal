using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.AccountManagement;

namespace Kentico.Community.Portal.Web.Tests.Components.PageBuilder.Widgets.AccountManagement;

public class UpdateBadgesRequestTests
{
    [Test]
    public void SelectedBadges_Returns_Empty_When_Badges_Is_Null()
    {
        // A member with no badges posts no badge fields, so model binding leaves Badges null
        var sut = new UpdateBadgesRequest(null);

        Assert.That(sut.SelectedBadges, Is.Empty);
    }

    [Test]
    public void SelectedBadges_Returns_Posted_Badges()
    {
        var badge = new SelectedBadgeViewModel { BadgeId = 1, BadgeCodeName = "badge", IsSelected = true };
        var sut = new UpdateBadgesRequest([badge]);

        Assert.That(sut.SelectedBadges, Is.EqualTo(new[] { badge }));
    }
}

public class BadgesFormViewModelTests
{
    [Test]
    public void HasBadges_Is_False_When_Member_Has_No_Badges()
    {
        var sut = new BadgesFormViewModel([]);

        Assert.Multiple(() =>
        {
            Assert.That(sut.HasBadges, Is.False);
            Assert.That(sut.InitialSelectedCount, Is.Zero);
            Assert.That(sut.RemainingSlots, Is.EqualTo(BadgesFormViewModel.MAX_SELECTED_BADGES));
        });
    }
}