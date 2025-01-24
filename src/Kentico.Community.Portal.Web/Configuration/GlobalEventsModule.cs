using CMS;
using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.EmailLibrary;
using CMS.EmailLibrary.Internal;
using CMS.Helpers;
using CMS.OnlineForms;
using Kentico.Community.Portal.Core.Forms;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Configuration;
using Kentico.Community.Portal.Web.Features.Blog.Events;
using Kentico.Community.Portal.Web.Features.Members;
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
        QAndAAnswerDataInfo.TYPEINFO.Events.Delete.After += QAndAAnswerDataInfo_DeleteAfter;
        WebPageEvents.Publish.Execute += WebPage_PublishExecute;
        ChannelInfo.TYPEINFO.Events.Update.Before += Channel_ModifyBefore;
        ChannelInfo.TYPEINFO.Events.Delete.Before += Channel_DeleteBefore;
        EmailConfigurationInfo.TYPEINFO.Events.Update.Before += EmailConfiguration_ModifyBefore;
        EmailConfigurationInfo.TYPEINFO.Events.Delete.Before += EmailConfiguration_DeleteBefore;
        TaxonomyInfo.TYPEINFO.Events.Update.Before += Taxonomy_ModifyBefore;
        TaxonomyInfo.TYPEINFO.Events.Delete.Before += Taxonomy_DeleteBefore;
        TagInfo.TYPEINFO.Events.Update.Before += Tag_ModifyBefore;
        TagInfo.TYPEINFO.Events.Delete.Before += Tag_DeleteBefore;
        BizFormItemEvents.Insert.After += FormItem_InsertAfter;

        EmailContentFilterRegister.Instance
            .Register(
                () => new CustomValueFilter(),
                EmailContentFilterType.Sending,
                100);

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

    private void QAndAAnswerDataInfo_DeleteAfter(object? sender, ObjectEventArgs e)
    {
        if (e.Object is not QAndAAnswerDataInfo answer)
        {
            return;
        }

        services.GetRequiredService<QAndAAnswerDeleteSearchIndexTaskHandler>().Handle(answer)
            .GetAwaiter()
            .GetResult();
    }

    private void WebPage_PublishExecute(object? sender, PublishWebPageEventArgs args)
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

    /// <summary>
    /// Ensures channels required by the system are not deleted
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="Exception"></exception>
    private void Channel_DeleteBefore(object? sender, ObjectEventArgs args)
    {
        if (args.Object is not ChannelInfo channel)
        {
            return;
        }

        if (SystemChannels.Includes(channel))
        {
            args.Cancel();

            throw new Exception($"Cannot delete required channel '{channel.ChannelName}'");
        }
    }

    /// <summary>
    /// Ensures channels required by the system are modified in specific ways
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="Exception"></exception>
    private void Channel_ModifyBefore(object? sender, ObjectEventArgs args)
    {
        if (args.Object is not ChannelInfo channel)
        {
            return;
        }

        if (SystemChannels.Includes(channel) &&
            channel.ChangedColumns().Contains(nameof(ChannelInfo.ChannelName)))
        {
            args.Cancel();

            throw new Exception($"Cannot modify the name of required channel '{channel.ChannelDisplayName}'");
        }
    }

    /// <summary>
    /// Ensures emails required by the system are not deleted
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="Exception"></exception>
    private void EmailConfiguration_DeleteBefore(object? sender, ObjectEventArgs args)
    {
        if (args.Object is not EmailConfigurationInfo config)
        {
            return;
        }

        if (SystemEmails.Includes(config))
        {
            args.Cancel();

            throw new Exception($"Cannot delete required email '{config.EmailConfigurationName}'");
        }
    }

    /// <summary>
    /// Ensures emails required by the system are modified in specific ways
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="Exception"></exception>
    private void EmailConfiguration_ModifyBefore(object? sender, ObjectEventArgs args)
    {
        if (args.Object is not EmailConfigurationInfo config)
        {
            return;
        }

        if (SystemEmails.Includes(config) &&
            config.ChangedColumns().Contains(nameof(EmailConfigurationInfo.EmailConfigurationName)))
        {
            args.Cancel();

            throw new Exception($"Cannot modify the name of required email '{config.GetOriginalValue(nameof(EmailConfigurationInfo.EmailConfigurationName))}'");
        }
    }

    /// <summary>
    /// Ensures taxonomies required by the system are modified in specific ways
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="Exception"></exception>
    private void Taxonomy_ModifyBefore(object? sender, ObjectEventArgs args)
    {
        if (args.Object is not TaxonomyInfo taxonomy)
        {
            return;
        }

        if (SystemTaxonomies.Includes(taxonomy) &&
            taxonomy.ChangedColumns().Contains(nameof(TaxonomyInfo.TaxonomyName)))
        {
            args.Cancel();

            throw new Exception($"Cannot modify the name of required taxonomy '{taxonomy.TaxonomyTitle}'");
        }
    }

    /// <summary>
    /// Ensures taxonomies required by the system are not deleted
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="Exception"></exception>
    private void Taxonomy_DeleteBefore(object? sender, ObjectEventArgs args)
    {
        if (args.Object is not TaxonomyInfo taxonomy)
        {
            return;
        }

        if (SystemTaxonomies.Includes(taxonomy))
        {
            args.Cancel();

            throw new Exception($"Cannot delete required taxonomy '{taxonomy.TaxonomyTitle}'");
        }
    }

    /// <summary>
    /// Ensures tags required by the system are modified in specific ways
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="Exception"></exception>
    private void Tag_ModifyBefore(object? sender, ObjectEventArgs args)
    {
        if (args.Object is not TagInfo tag)
        {
            return;
        }

        if (SystemTaxonomies.Includes(tag) &&
            tag.ChangedColumns().Contains(nameof(TagInfo.TagName)))
        {
            args.Cancel();

            throw new Exception($"Cannot modify the name of required tag '{tag.TagTitle}'");
        }
    }

    /// <summary>
    /// Ensures tags required by the system are not deleted
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="Exception"></exception>
    private void Tag_DeleteBefore(object? sender, ObjectEventArgs args)
    {
        if (args.Object is not TagInfo tag)
        {
            return;
        }

        if (SystemTaxonomies.Includes(tag))
        {
            args.Cancel();

            throw new Exception($"Cannot delete required tag '{tag.TagTitle}'");
        }
    }

    private void FormItem_InsertAfter(object? sender, BizFormItemEventArgs e)
    {
        if (e.Item is null or not (MVPActivityItem or CommunityLeaderActivityItem))
        {
            return;
        }

        var builder = new CacheDependencyKeysBuilder();
        builder.AllObjects(e.Item.BizFormClassName);
        foreach (string key in builder.GetKeys())
        {
            CacheHelper.TouchKey(key);
        }
    }
}
