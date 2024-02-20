using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerDeleteCommand(QAndAAnswerDataInfo Answer) : ICommand<Unit>;
public class QAndAAnswerDeleteCommandHandler(DataItemCommandTools tools, IInfoProvider<QAndAAnswerDataInfo> provider) : DataItemCommandHandler<QAndAAnswerDeleteCommand, Unit>(tools)
{
    private readonly IInfoProvider<QAndAAnswerDataInfo> provider = provider;

    public override Task<Unit> Handle(QAndAAnswerDeleteCommand request, CancellationToken cancellationToken)
    {
        provider.Delete(request.Answer);

        return Unit.Task;
    }
}
