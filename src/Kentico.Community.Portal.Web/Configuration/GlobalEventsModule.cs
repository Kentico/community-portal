using CMS;
using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Web.Configuration;
using Kentico.Community.Portal.Web.Features.Blog.Events;
using Kentico.Community.Portal.Web.Features.QAndA.Events;

[assembly: RegisterModule(typeof(GlobalEventsModule))]

namespace Kentico.Community.Portal.Web.Configuration;

internal class GlobalEventsModule : Module
{
    private IServiceProvider services = null!;

    public GlobalEventsModule() : base(nameof(GlobalEventsModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        services = parameters.Services;
        var env = services.GetRequiredService<IWebHostEnvironment>();

        /*
         * Ensure all pages that should be excluded from CD deployment end
         * in the correct code name
         */
        if (env.IsDevelopment())
        {
            WebPageEvents.Create.Before += EnsureLocalCodeNames;
            ContentItemEvents.Create.Before += EnsureLocalCodeNames;
        }

        QAndAAnswerDataInfo.TYPEINFO.Events.Insert.After += QAndAAnswerDataInfo_InsertAfter;
        ContentItemEvents.Publish.Execute += ContentItem_PublishExecute;
        WebPageEvents.Publish.Execute += WebPage_PublishExecute;

        base.OnInit(parameters);
    }

    /// <summary>
    /// Ensures that content that should be targetable by CI/CD repository.config files
    /// has codenames that can be matched by a wildcard pattern
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void EnsureLocalCodeNames(object? sender, CreateWebPageEventArgs e)
    {
        if (string.Equals(e.ContentTypeName, BlogPostPage.CONTENT_TYPE_NAME)
            || string.Equals(e.ContentTypeName, QAndAQuestionPage.CONTENT_TYPE_NAME))
        {
            if (e.Name.EndsWith("-localtest"))
            {
                return;
            }

            e.Name = e.Name[..Math.Min(90, e.Name.Length)] + "-localtest";
        }
    }
    private void EnsureLocalCodeNames(object? sender, CreateContentItemEventArgs e)
    {
        if (string.Equals(e.ContentTypeName, BlogPostContent.CONTENT_TYPE_NAME))
        {
            if (e.Name.EndsWith("-localtest"))
            {
                return;
            }

            e.Name = e.Name[..Math.Min(90, e.Name.Length)] + "-localtest";
        }
    }

    private void QAndAAnswerDataInfo_InsertAfter(object? sender, ObjectEventArgs e)
    {
        if (e.Object is not QAndAAnswerDataInfo answer)
        {
            return;
        }

        services.GetRequiredService<QAndAAnswerCreateSearchIndexTaskHandler>().Handle(answer)
            .GetAwaiter()
            .GetResult();
    }

    public void ContentItem_PublishExecute(object? sender, PublishContentItemEventArgs args)
    {
        if (string.Equals(args.ContentTypeName, BlogPostContent.CONTENT_TYPE_NAME))
        {
            services.GetRequiredService<BlogPostContentCreateSearchIndexTaskHandler>().Handle(args)
                .GetAwaiter()
                .GetResult();
        }
    }

    public void WebPage_PublishExecute(object? sender, PublishWebPageEventArgs args)
    {
        if (string.Equals(args.ContentTypeName, BlogPostPage.CONTENT_TYPE_NAME))
        {
            /*
             * Create a new lifetime scope because we can only access
             * Transient and Singleton registered services in Modules
             * and this handler uses "Scoped" services
             */
            using var scope = services.CreateScope();
            scope.ServiceProvider.GetRequiredService<BlogPostPublishCreateQAndAQuestionHandler>().Handle(args)
                .GetAwaiter()
                .GetResult();
        }
    }
}
