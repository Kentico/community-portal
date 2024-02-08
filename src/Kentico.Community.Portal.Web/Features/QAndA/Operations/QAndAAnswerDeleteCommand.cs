using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerDeleteCommand(QAndAAnswerDataInfo Answer) : ICommand<Unit>;
public class QAndAAnswerDeleteCommandHandler(DataItemCommandTools tools, IQAndAAnswerDataInfoProvider provider) : DataItemCommandHandler<QAndAAnswerDeleteCommand, Unit>(tools)
{
    private readonly IQAndAAnswerDataInfoProvider provider = provider;

    public override Task<Unit> Handle(QAndAAnswerDeleteCommand request, CancellationToken cancellationToken)
    {
        provider.Delete(request.Answer);

        return Unit.Task;
    }
}
