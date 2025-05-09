using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Core.Modules;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public class QAndAQuestionFormViewComponent(IMediator mediator, IWebsiteChannelContext channelContext) : ViewComponent
{
    private readonly IMediator mediator = mediator;
    private readonly IWebsiteChannelContext channelContext = channelContext;

    public async Task<IViewComponentResult> InvokeAsync(Guid? questionID = null, QAndAQuestionFormSubmissionViewModel? submission = null)
    {
        var landingResp = await mediator.Send(new QAndALandingPageQuery(channelContext.WebsiteChannelName));
        if (!landingResp.TryGetValue(out var landingPage))
        {
            ModelState.AddModelError("", $"Could not load Q&A question form.");
            return View("~/Components/ComponentError.cshtml");
        }

        var formHelpMessageHTML = new HtmlString(landingPage.QAndALandingPageMarkdownFormHelpMessageHTML);
        if (questionID is not Guid id)
        {
            var groups = await GetTagGroups(submission?.SelectedTags ?? []);
            return View("~/Features/QAndA/Components/Form/QAndAQuestionForm.cshtml",
                new QAndAQuestionFormViewModel(null, formHelpMessageHTML, groups, submission));
        }

        var questionResp = await mediator.Send(new QAndAQuestionPageByGUIDQuery(id, channelContext.WebsiteChannelName));
        if (!questionResp.TryGetValue(out var questionPage))
        {
            ModelState.AddModelError("", $"Could not find question {id}.");
            return View("~/Components/ComponentError.cshtml");
        }

        var selectedTaxonomies = questionPage.QAndAQuestionPageDXTopics
            .Select(t => t.Identifier)
            .ToList();
        var groupsWithSelected = await GetTagGroups(selectedTaxonomies);
        var vm = new QAndAQuestionFormViewModel(id, questionPage.QAndAQuestionPageTitle, questionPage.QAndAQuestionPageContent, formHelpMessageHTML, groupsWithSelected);
        return View("~/Features/QAndA/Components/Form/QAndAQuestionForm.cshtml", vm);
    }

    private async Task<IReadOnlyList<QuestionFacetGroup>> GetTagGroups(IReadOnlyList<Guid> selectedTaxonomies)
    {
        var taxonomies = await mediator.Send(new QAndATaxonomiesQuery());
        return [.. taxonomies.DXTopicsHierarchy
            .Select(parent => new QuestionFacetGroup(parent, selectedTaxonomies))
            .OrderBy(g => g.Label)];
    }
}

public class QAndAQuestionFormViewModel
{
    [Required]
    [MaxLength(100)]
    public string Title { get; }

    [Required]
    public string Content { get; }

    [HiddenInput]
    public Guid? EditedObjectID { get; }

    public HtmlString FormHelpMessageHTML { get; }
    public IReadOnlyList<QuestionFacetGroup> TagGroups { get; }
    public int SelectedTagsCount { get; set; }

    public QAndAQuestionFormViewModel(Guid? editedObjectID, string title, string content, HtmlString formHelpMessageHTML, IReadOnlyList<QuestionFacetGroup> dxTopics)
    {
        EditedObjectID = editedObjectID;
        Title = title;
        Content = content;
        FormHelpMessageHTML = formHelpMessageHTML;
        TagGroups = dxTopics;
        SelectedTagsCount = TagGroups.SelectMany(t => t.Facets).Where(f => f.IsSelected).Count();
    }

    public QAndAQuestionFormViewModel(Guid? editedObjectID, HtmlString formHelpMessageHTML, IReadOnlyList<QuestionFacetGroup> dxTopics, QAndAQuestionFormSubmissionViewModel? submission)
    {
        EditedObjectID = editedObjectID;
        Title = submission?.Title ?? "";
        Content = submission?.Content ?? "";
        FormHelpMessageHTML = formHelpMessageHTML;
        TagGroups = dxTopics;
        SelectedTagsCount = TagGroups.SelectMany(t => t.Facets).Where(f => f.IsSelected).Count();
    }
}

public class QAndAQuestionFormSubmissionViewModel
{
    public const int MAX_TAGS = 4;
    public Guid? EditedObjectID { get; set; }

    [Required(ErrorMessage = "A title is required")]
    [MaxLength(100, ErrorMessage = "Title can be at most 100 characters")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "You must provide some discussion content")]
    [DisplayName("Discussion content")]
    public string Content { get; set; } = "";

    [MaxLength(MAX_TAGS, ErrorMessage = "You can only select up to 4 tags.")]
    public List<Guid> SelectedTags { get; set; } = [];
}

public class QuestionFacetOption
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Group { get; set; } = "";
    public bool IsSelected { get; set; }

    public QuestionFacetOption(TaxonomyTag tag, string groupName, IReadOnlyList<Guid> selectedTags)
    {
        Label = tag.DisplayName;
        Value = tag.Guid.ToString();
        Group = groupName;
        IsSelected = selectedTags.Contains(tag.Guid);
    }
}


public class QuestionFacetGroup
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public IReadOnlyList<QuestionFacetOption> Facets { get; set; } = [];

    public QuestionFacetGroup(TaxonomyTag tag, IReadOnlyList<Guid> selectedTags)
    {
        Label = tag.DisplayName;
        Value = tag.Name;
        Facets = [.. tag.Children.OrderBy(g => g.DisplayName).Select(t => new QuestionFacetOption(t, tag.DisplayName, selectedTags))];
    }
}
