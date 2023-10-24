using CMS.Websites;
using CMS.Websites.Routing;
using MediatR;

namespace Kentico.Community.Portal.Core.Operations;

public class WebPageCommandTools
{
    public IContentQueryExecutor Executor { get; }
    public IWebPageQueryResultMapper Mapper { get; }
    public IWebPageManagerFactory WebPageManagerFactory { get; }
    public IContentItemManagerFactory ContentItemManagerFactory { get; }
    public IWebsiteChannelContext WebsiteChannelContext { get; }

    public WebPageCommandTools(IContentQueryExecutor executor, IWebPageQueryResultMapper mapper, IWebPageManagerFactory webPageManagerFactory, IContentItemManagerFactory contentItemManagerFactory, IWebsiteChannelContext websiteChannelContext)
    {
        Executor = executor;
        Mapper = mapper;
        WebPageManagerFactory = webPageManagerFactory;
        ContentItemManagerFactory = contentItemManagerFactory;
        WebsiteChannelContext = websiteChannelContext;
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
        WebsiteChannelContext = tools.WebsiteChannelContext;
    }

    public IContentQueryExecutor Executor { get; }
    public IWebPageQueryResultMapper Mapper { get; }
    public IWebPageManagerFactory WebPageManagerFactory { get; }
    public IContentItemManagerFactory ContentItemManagerFactory { get; }
    public IWebsiteChannelContext WebsiteChannelContext { get; }

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
        WebsiteChannelContext = tools.WebsiteChannelContext;
    }

    public IContentQueryExecutor Executor { get; }
    public IWebPageQueryResultMapper Mapper { get; }
    public IWebPageManagerFactory WebPageManagerFactory { get; }
    public IContentItemManagerFactory ContentItemManagerFactory { get; }
    public IWebsiteChannelContext WebsiteChannelContext { get; }

    public abstract Task<TResult> Handle(TCommand request, CancellationToken cancellationToken);
}

public class DataItemCommandTools
{
    public DataItemCommandTools(IWebsiteChannelContext websiteChannelContext) => WebsiteChannelContext = websiteChannelContext;

    public IWebsiteChannelContext WebsiteChannelContext { get; }
}

public abstract class DataItemCommandHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public DataItemCommandHandler(DataItemCommandTools tools) => WebsiteChannelContext = tools.WebsiteChannelContext;
    public IWebsiteChannelContext WebsiteChannelContext { get; }
    public abstract Task<TResult> Handle(TCommand request, CancellationToken cancellationToken);
}
