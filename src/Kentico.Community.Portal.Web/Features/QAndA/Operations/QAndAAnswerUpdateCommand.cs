using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerUpdateCommand(QAndAAnswerDataInfo Answer, string UpdatedAnswerContent) : ICommand<Unit>;
public class QAndAAnswerUpdateCommandHandler(DataItemCommandTools tools, ISystemClock clock, IQAndAAnswerDataInfoProvider provider) : DataItemCommandHandler<QAndAAnswerUpdateCommand, Unit>(tools)
{
    private readonly ISystemClock clock = clock;
    private readonly IQAndAAnswerDataInfoProvider provider = provider;

    public override Task<Unit> Handle(QAndAAnswerUpdateCommand request, CancellationToken cancellationToken)
    {
        var answer = request.Answer;

        string filteredContent = QandAContentParser.Alphanumeric(request.UpdatedAnswerContent);
        string uniqueID = Guid.NewGuid().ToString("N");
        string codeName = $"{filteredContent[..Math.Min(42, filteredContent.Length)]}{uniqueID[..8]}";

        var now = clock.UtcNow;

        answer.QAndAAnswerDataContent = request.UpdatedAnswerContent;
        answer.QAndAAnswerDataDateModified = now;
        answer.QAndAAnswerDataCodeName = codeName;

        provider.Set(answer);

        return Unit.Task;
    }
}
