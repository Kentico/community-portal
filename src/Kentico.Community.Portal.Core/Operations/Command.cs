using MediatR;

namespace Kentico.Community.Portal.Core.Operations;

public interface ICommand<out TResult> : IRequest<TResult> { }
