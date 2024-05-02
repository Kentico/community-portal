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

    public override Task<Result> Handle(QAndAAnswerDeleteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            provider.Delete(request.Answer);
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Could not delete answer [{request.Answer.QAndAAnswerDataGUID}]: {ex}"));
        }

        return Task.FromResult(Result.Success());
    }
}
