using CMS.Core;

namespace Kentico.Community.Portal.Core.Operations;

public class CommandHandlerLogDecorator<TCommand, TResult>(IEventLogService log, ICommandHandler<TCommand, TResult> decorated) : ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    private readonly IEventLogService log = log;
    private readonly ICommandHandler<TCommand, TResult> decorated = decorated;

    public async Task<TResult> Handle(TCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await decorated.Handle(request, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            log.LogException(request.GetType().Name, "FAILURE", ex);

            throw;
        }
    }
}
