using System.Text.RegularExpressions;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerCreateCommand(CommunityMember MemberAuthor, string AnswerContent, QAndAQuestionPage ParentQuestion) : ICommand<int>;
public class QAndAAnswerCreateCommandHandler : DataItemCommandHandler<QAndAAnswerCreateCommand, int>
{
    private readonly ISystemClock clock;
    private readonly IQAndAAnswerDataInfoProvider provider;

    public QAndAAnswerCreateCommandHandler(DataItemCommandTools tools, ISystemClock clock, IQAndAAnswerDataInfoProvider provider) : base(tools)
    {
        this.clock = clock;
        this.provider = provider;
    }

    public override Task<int> Handle(QAndAAnswerCreateCommand request, CancellationToken cancellationToken)
    {
        string filteredContent = Regex.Replace(request.AnswerContent, @"[^a-zA-Z0-9\d]", "-").RemoveRepeatedCharacters('-') ?? "";
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
            QAndAAnswerDataCodeName = codeName
        };

        provider.Set(answer);

        return Task.FromResult(answer.QAndAAnswerDataID);
    }
}
