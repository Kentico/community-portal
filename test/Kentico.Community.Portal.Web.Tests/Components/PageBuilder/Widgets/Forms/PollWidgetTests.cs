using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.PollForm;
using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Community.Portal.Web.Tests.Components.PageBuilder.Widgets.Forms;

public class PollWidgetTests
{
    [Test]
    public void GetFormName_Prefers_Widget_Form()
    {
        var props = new PollWidgetProperties { PollForms = [new ObjectRelatedItem { ObjectCodeName = "WidgetForm" }] };
        var poll = new PollContent { PollContentForm = ["PollForm"] };

        Assert.That(PollWidget.GetFormName(props, poll).GetValueOrDefault(), Is.EqualTo("WidgetForm"));
    }

    [Test]
    public void GetFormName_Falls_Back_To_Poll_Form_When_Widget_Form_Empty()
    {
        var props = new PollWidgetProperties();
        var poll = new PollContent { PollContentForm = ["PollForm"] };

        Assert.That(PollWidget.GetFormName(props, poll).GetValueOrDefault(), Is.EqualTo("PollForm"));
    }

    [Test]
    public void GetFormName_Is_None_When_No_Form_Set()
    {
        var props = new PollWidgetProperties { PollForms = [new ObjectRelatedItem { ObjectCodeName = " " }] };
        var poll = new PollContent { PollContentForm = [] };

        Assert.That(PollWidget.GetFormName(props, poll).HasNoValue, Is.True);
    }
}
