using System.ComponentModel.DataAnnotations;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Registration;

namespace Kentico.Community.Portal.Web.Tests.Components.PageBuilder.Widgets.Registration;

public class RegisterViewModelTests
{
    private static RegisterViewModel ValidModel() => new()
    {
        UserName = "member",
        Email = "member@example.com",
        Password = "Pass@12345",
        PasswordConfirmation = "Pass@12345",
        CaptchaToken = "token",
        AcceptTerms = true,
    };

    private static List<ValidationResult> Validate(RegisterViewModel model)
    {
        var results = new List<ValidationResult>();
        _ = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        return results;
    }

    [Test]
    public void Is_Valid_When_Terms_Accepted_And_Newsletter_Not_Requested()
    {
        var model = ValidModel();

        Assert.That(Validate(model), Is.Empty);
    }

    [Test]
    public void Is_Invalid_When_Terms_Not_Accepted()
    {
        var model = ValidModel();
        model.AcceptTerms = false;

        var results = Validate(model);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].MemberNames, Is.EqualTo(new[] { nameof(RegisterViewModel.AcceptTerms) }));
        Assert.That(results[0].ErrorMessage, Is.EqualTo("Please accept the Terms of Use to create an account."));
    }

    [Test]
    public void Newsletter_Is_Optional()
    {
        var model = ValidModel();
        model.SubscribeToNewsletter = true;

        Assert.That(Validate(model), Is.Empty);
    }
}
