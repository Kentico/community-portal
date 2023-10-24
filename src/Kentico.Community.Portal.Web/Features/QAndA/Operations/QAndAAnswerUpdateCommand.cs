using System.Text.RegularExpressions;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerUpdateCommand(QAndAAnswerDataInfo Answer, string UpdatedAnswerContent) : ICommand<Unit>;
public class QAndAAnswerUpdateCommandHandler : DataItemCommandHandler<QAndAAnswerUpdateCommand, Unit>
{
    private readonly ISystemClock clock;
    private readonly IQAndAAnswerDataInfoProvider provider;

    public QAndAAnswerUpdateCommandHandler(DataItemCommandTools tools, ISystemClock clock, IQAndAAnswerDataInfoProvider provider) : base(tools)
    {
        this.clock = clock;
        this.provider = provider;
    }

    public override Task<Unit> Handle(QAndAAnswerUpdateCommand request, CancellationToken cancellationToken)
    {
        var answer = request.Answer;

        string filteredContent = Regex.Replace(request.UpdatedAnswerContent, @"[^a-zA-Z0-9\d]", "-").RemoveRepeatedCharacters('-');
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
