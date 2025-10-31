using MediatR;
using Microsoft.Extensions.Logging;

namespace Kentico.Community.Portal.Core.Operations;

public class CommandHandlerLogDecorator<TCommand, TResult>(ILogger<CommandHandlerLogDecorator<TCommand, TResult>> logger)
    : IPipelineBehavior<TCommand, TResult> where TCommand : ICommand<TResult>
{
    private readonly ILogger<CommandHandlerLogDecorator<TCommand, TResult>> logger = logger;

    public async Task<TResult> Handle(TCommand request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(new EventId(0, "COMMAND_FAILURE"), ex, "{CommandType} command failed", request.GetType().Name);
            throw;
        }
    }
}
