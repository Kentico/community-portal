using CMS.DataEngine;
using CMS.Websites.Routing;

using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerCreateCommand(
    CommunityMember MemberAuthor,
    string AnswerContent,
    QAndAQuestionPage ParentQuestion,
    IWebsiteChannelContext WebsiteChannelContext)
    : ICommand<Result<int>>;
public class QAndAAnswerCreateCommandHandler(
    DataItemCommandTools tools,
    ISystemClock clock,
    IInfoProvider<QAndAAnswerDataInfo> provider)
    : DataItemCommandHandler<QAndAAnswerCreateCommand, Result<int>>(tools)
{
    private readonly ISystemClock clock = clock;
    private readonly IInfoProvider<QAndAAnswerDataInfo> provider = provider;

    public override Task<Result<int>> Handle(QAndAAnswerCreateCommand request, CancellationToken cancellationToken)
    {
        string filteredContent = QandAContentParser.Alphanumeric(request.AnswerContent);
        string uniqueID = Guid.NewGuid().ToString("N");
        string codeName = $"{filteredContent[..Math.Min(42, filteredContent.Length)]}{uniqueID[..8]}";
        var now = clock.UtcNow;

        var answer = new QAndAAnswerDataInfo()
        {
            QAndAAnswerDataGUID = Guid.NewGuid(),
            QAndAAnswerDataAuthorMemberID = request.MemberAuthor.Id,
            QAndAAnswerDataContent = request.AnswerContent,
            QAndAAnswerDataDateCreated = now,
            QAndAAnswerDataDateModified = now,
            QAndAAnswerDataQuestionWebPageItemID = request.ParentQuestion.SystemFields.WebPageItemID,
            QAndAAnswerDataCodeName = codeName,
            QAndAAnswerDataWebsiteChannelID = request.WebsiteChannelContext.WebsiteChannelID
        };

        try
        {
            provider.Set(answer);
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<int>($"Could not save answer for question [{request.ParentQuestion.SystemFields.WebPageItemTreePath}]: {ex}"));
        }

        return Task.FromResult(Result.Success(answer.QAndAAnswerDataID));
    }
}
