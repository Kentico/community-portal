using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerCreateCommand(
    CommunityMember MemberAuthor,
    string AnswerContent,
    QAndAQuestionPage ParentQuestion)
    : ICommand<Result<int>>;
public class QAndAAnswerCreateCommandHandler(
    DataItemCommandTools tools,
    TimeProvider clock,
    IInfoProvider<QAndAAnswerDataInfo> provider)
    : DataItemCommandHandler<QAndAAnswerCreateCommand, Result<int>>(tools)
{
    private readonly TimeProvider clock = clock;
    private readonly IInfoProvider<QAndAAnswerDataInfo> provider = provider;

    public override async Task<Result<int>> Handle(QAndAAnswerCreateCommand request, CancellationToken cancellationToken)
    {
        string filteredContent = QandAContentParser.Alphanumeric(request.AnswerContent);
        string uniqueID = Guid.NewGuid().ToString("N");
        string codeName = $"{filteredContent[..Math.Min(42, filteredContent.Length)]}{uniqueID[..8]}";
        var now = clock.GetUtcNow().DateTime;

        var answer = new QAndAAnswerDataInfo()
        {
            QAndAAnswerDataGUID = Guid.NewGuid(),
            QAndAAnswerDataAuthorMemberID = request.MemberAuthor.Id,
            QAndAAnswerDataContent = request.AnswerContent,
            QAndAAnswerDataDateCreated = now,
            QAndAAnswerDataDateModified = now,
            QAndAAnswerDataQuestionWebPageItemID = request.ParentQuestion.SystemFields.WebPageItemID,
            QAndAAnswerDataCodeName = codeName,
            QAndAAnswerDataWebsiteChannelID = request.ParentQuestion.SystemFields.WebPageItemWebsiteChannelId
        };

        try
        {
            await provider.SetAsync(answer);
        }
        catch (Exception ex)
        {
            return Result.Failure<int>($"Could not save answer for question [{request.ParentQuestion.SystemFields.WebPageItemTreePath}]: {ex}");
        }

        return Result.Success(answer.QAndAAnswerDataID);
    }
}
