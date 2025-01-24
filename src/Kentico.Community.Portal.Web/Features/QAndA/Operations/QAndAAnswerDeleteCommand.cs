using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerDeleteCommand(QAndAAnswerDataInfo Answer) : ICommand<Result>;
public class QAndAAnswerDeleteCommandHandler(
    DataItemCommandTools tools,
    IInfoProvider<QAndAAnswerDataInfo> provider)
    : DataItemCommandHandler<QAndAAnswerDeleteCommand, Result>(tools)
{
    private readonly IInfoProvider<QAndAAnswerDataInfo> provider = provider;

    public override async Task<Result> Handle(QAndAAnswerDeleteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await provider.DeleteAsync(request.Answer);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Could not delete answer [{request.Answer.QAndAAnswerDataGUID}]: {ex}");
        }

        return Result.Success();
    }
}
