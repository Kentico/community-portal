using CMS.Websites;
using MediatR;

namespace Kentico.Community.Portal.Core.Operations;

public class WebPageCommandTools
{
    public IContentQueryExecutor Executor { get; }
    public IWebPageQueryResultMapper Mapper { get; }
    public IWebPageManagerFactory WebPageManagerFactory { get; }
    public IContentItemManagerFactory ContentItemManagerFactory { get; }

    public WebPageCommandTools(IContentQueryExecutor executor, IWebPageQueryResultMapper mapper, IWebPageManagerFactory webPageManagerFactory, IContentItemManagerFactory contentItemManagerFactory)
    {
        Executor = executor;
        Mapper = mapper;
        WebPageManagerFactory = webPageManagerFactory;
        ContentItemManagerFactory = contentItemManagerFactory;
    }
}

public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult> where TCommand : ICommand<TResult> { }

public abstract class WebPageCommandHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public WebPageCommandHandler(WebPageCommandTools tools)
    {
        Executor = tools.Executor;
        Mapper = tools.Mapper;
        WebPageManagerFactory = tools.WebPageManagerFactory;
        ContentItemManagerFactory = tools.ContentItemManagerFactory;
    }

    public IContentQueryExecutor Executor { get; }
    public IWebPageQueryResultMapper Mapper { get; }
    public IWebPageManagerFactory WebPageManagerFactory { get; }
    public IContentItemManagerFactory ContentItemManagerFactory { get; }

    public abstract Task<TResult> Handle(TCommand request, CancellationToken cancellationToken);
}

public abstract class ContentItemCommandHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public ContentItemCommandHandler(WebPageCommandTools tools)
    {
        Executor = tools.Executor;
        Mapper = tools.Mapper;
        WebPageManagerFactory = tools.WebPageManagerFactory;
        ContentItemManagerFactory = tools.ContentItemManagerFactory;
    }

    public IContentQueryExecutor Executor { get; }
    public IWebPageQueryResultMapper Mapper { get; }
    public IWebPageManagerFactory WebPageManagerFactory { get; }
    public IContentItemManagerFactory ContentItemManagerFactory { get; }

    public abstract Task<TResult> Handle(TCommand request, CancellationToken cancellationToken);
}

public class DataItemCommandTools
{
    // Placeholder for future services
}

public abstract class DataItemCommandHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
#pragma warning disable IDE0052
    private readonly DataItemCommandTools tools;
#pragma warning restore IDE0052

    public DataItemCommandHandler(DataItemCommandTools tools) => this.tools = tools;

    public abstract Task<TResult> Handle(TCommand request, CancellationToken cancellationToken);
}
