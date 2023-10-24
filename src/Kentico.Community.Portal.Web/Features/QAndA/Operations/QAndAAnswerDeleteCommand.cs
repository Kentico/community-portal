using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerDeleteCommand(QAndAAnswerDataInfo Answer) : ICommand<Unit>;
public class QAndAAnswerDeleteCommandHandler : DataItemCommandHandler<QAndAAnswerDeleteCommand, Unit>
{
    private readonly IQAndAAnswerDataInfoProvider provider;

    public QAndAAnswerDeleteCommandHandler(DataItemCommandTools tools, IQAndAAnswerDataInfoProvider provider) : base(tools) => this.provider = provider;

    public override Task<Unit> Handle(QAndAAnswerDeleteCommand request, CancellationToken cancellationToken)
    {
        provider.Delete(request.Answer);

        return Unit.Task;
    }
}
