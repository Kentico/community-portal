using CMS.Core;
using CMS.OnlineForms;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;
using MediatR;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.Forms;

public class PollFormResultsViewComponent(IMediator mediator) : ViewComponent
{
    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(PollResultsProperties props)
    {
        var formAndNameResp = await mediator.Send(new FormByFormNameQuery(props.FormName));
        if (!formAndNameResp.TryGetValue(out var formAndName))
        {
            return View("~/Components/ComponentError.cshtml");
        }

        var formData = FormParser.GetPollFormData(formAndName.Form);
        var resultsResp = await mediator.Send(new PollResultsQuery(formAndName.FormClassName));

        var currentMemberID = CommunityMember.GetMemberIDFromClaim(HttpContext);

        var answer = resultsResp.ResultsByMember.TryGetValue(currentMemberID.Value, out string? key) && formData.Options.TryGetValue(key, out string? ans)
            ? ans
            : Maybe<string>.None;

        return View("~/Components/PageBuilder/Widgets/Forms/PollFormResults.cshtml", new PollFormResultsViewModel(
            formData,
            resultsResp.ResultsByAnswer,
            props.Description,
            props.IsPollActive,
            props.OpenUntilDate,
            answer));
    }
}

public class PollResultsProperties
{
    public required string FormName { get; init; }
    public Maybe<HtmlString> Description { get; init; }
    public bool IsPollActive { get; init; }
    public DateTime OpenUntilDate { get; init; }
}

public record PollFormResultsViewModel(
    PollFormData FormData,
    Dictionary<string, int> Results,
    Maybe<HtmlString> Description,
    bool IsPollActive,
    DateTime OpenUntilDate,
    Maybe<string> CurrentMembersAnswer);

public record PollResultsQuery(FormClassName FormClassName) : IQuery<PollResultsQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => FormClassName.Value;
}
public record PollResultsQueryResponse(Dictionary<string, int> ResultsByAnswer, Dictionary<int, string> ResultsByMember);

public class PollResultsQueryHandler(DataItemQueryTools tools, IConversionService conversion) : DataItemQueryHandler<PollResultsQuery, PollResultsQueryResponse>(tools)
{
    private readonly IConversionService conversion = conversion;

    public override async Task<PollResultsQueryResponse> Handle(PollResultsQuery request, CancellationToken cancellationToken = default)
    {
        var items = await BizFormItemProvider.GetItems(request.FormClassName.Value)
            // All Poll forms require a "Question" field
            .Columns("Question", "MemberID")
            .WhereNotEmpty("Question")
            .GetDataContainerResultAsync();

        var resultsByAnswer = items
            .GroupBy(i => conversion.GetString(i.GetValue("Question"), ""))
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .ToDictionary(g => g.Key, g => g.Count());

        var resultsByMember = items
            .ToDictionary(i => conversion.GetInteger(i.GetValue("MemberID"), 0), i => conversion.GetString(i.GetValue("Question"), ""));

        return new(resultsByAnswer, resultsByMember);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(PollResultsQuery query, PollResultsQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(query.FormClassName.Value);
}
