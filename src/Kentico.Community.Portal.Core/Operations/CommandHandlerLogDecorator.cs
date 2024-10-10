using CMS.Core;
using MediatR;

namespace Kentico.Community.Portal.Core.Operations;

public class CommandHandlerLogDecorator<TCommand, TResult>(IEventLogService log)
    : IPipelineBehavior<TCommand, TResult> where TCommand : ICommand<TResult>
{
    private readonly IEventLogService log = log;

    public async Task<TResult> Handle(TCommand request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            log.LogException(
                nameof(CommandHandlerLogDecorator<TCommand, TResult>),
                "COMMAND_FAILURE",
                ex,
                $"{request.GetType().Name} command failed");

            throw;
        }
    }
}
