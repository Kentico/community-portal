using CMS.Websites;
using MediatR;

namespace Kentico.Community.Portal.Core.Operations;

public class WebPageCommandTools(
    IContentQueryExecutor executor,
    IWebPageManagerFactory webPageManagerFactory,
    IContentItemManagerFactory contentItemManagerFactory,
    IWebPageFolderRetriever webPageFolderRetriever)
{
    public IContentQueryExecutor Executor { get; } = executor;
    public IWebPageManagerFactory WebPageManagerFactory { get; } = webPageManagerFactory;
    public IContentItemManagerFactory ContentItemManagerFactory { get; } = contentItemManagerFactory;
    public IWebPageFolderRetriever WebPageFolderRetriever { get; } = webPageFolderRetriever;
}

public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult> where TCommand : ICommand<TResult> { }

public abstract class WebPageCommandHandler<TCommand, TResult>(WebPageCommandTools tools) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public IContentQueryExecutor Executor { get; } = tools.Executor;
    public IWebPageManagerFactory WebPageManagerFactory { get; } = tools.WebPageManagerFactory;
    public IContentItemManagerFactory ContentItemManagerFactory { get; } = tools.ContentItemManagerFactory;
    public IWebPageFolderRetriever WebPageFolderRetriever { get; } = tools.WebPageFolderRetriever;

    public abstract Task<TResult> Handle(TCommand request, CancellationToken cancellationToken);
}

public abstract class ContentItemCommandHandler<TCommand, TResult>(WebPageCommandTools tools) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public IContentQueryExecutor Executor { get; } = tools.Executor;
    public IWebPageManagerFactory WebPageManagerFactory { get; } = tools.WebPageManagerFactory;
    public IContentItemManagerFactory ContentItemManagerFactory { get; } = tools.ContentItemManagerFactory;

    public abstract Task<TResult> Handle(TCommand request, CancellationToken cancellationToken);
}

public class DataItemCommandTools
{
    // Placeholder for future services
}

public abstract class DataItemCommandHandler<TCommand, TResult>(DataItemCommandTools tools) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
#pragma warning disable IDE0052
    private readonly DataItemCommandTools tools = tools;

#pragma warning restore IDE0052

    public abstract Task<TResult> Handle(TCommand request, CancellationToken cancellationToken);
}
