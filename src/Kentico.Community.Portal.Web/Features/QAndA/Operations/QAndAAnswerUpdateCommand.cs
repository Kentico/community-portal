using CMS.DataEngine;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerUpdateCommand(QAndAAnswerDataInfo Answer, string UpdatedAnswerContent) : ICommand<Result>;
public class QAndAAnswerUpdateCommandHandler(
    DataItemCommandTools tools,
    ISystemClock clock,
    IInfoProvider<QAndAAnswerDataInfo> provider)
    : DataItemCommandHandler<QAndAAnswerUpdateCommand, Result>(tools)
{
    private readonly ISystemClock clock = clock;
    private readonly IInfoProvider<QAndAAnswerDataInfo> provider = provider;

    public override async Task<Result> Handle(QAndAAnswerUpdateCommand request, CancellationToken cancellationToken)
    {
        var answer = request.Answer;

        string filteredContent = QandAContentParser.Alphanumeric(request.UpdatedAnswerContent);
        string uniqueID = Guid.NewGuid().ToString("N");
        string codeName = $"{filteredContent[..Math.Min(42, filteredContent.Length)]}{uniqueID[..8]}";

        var now = clock.UtcNow;

        answer.QAndAAnswerDataContent = request.UpdatedAnswerContent;
        answer.QAndAAnswerDataDateModified = now;
        answer.QAndAAnswerDataCodeName = codeName;

        try
        {
            await provider.SetAsync(answer);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Could not update answer [{request.Answer.QAndAAnswerDataGUID}]: {ex}");
        }

        return Result.Success();
    }
}
