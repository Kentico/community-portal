using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Routing;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

[assembly: CMS.RegisterModule(typeof(PortalWebAdminModule))]

[assembly: UICategory(
   codeName: PortalWebAdminModule.CATEGORY,
   name: "Community",
   icon: Icons.Personalisation,
   order: 100)]

namespace Kentico.Community.Portal.Admin;

internal class PortalWebAdminModule : AdminModule
{
    public const string CATEGORY = "kentico-community.portal-web-admin.category";

    public PortalWebAdminModule()
        : base(nameof(PortalWebAdminModule))
    {
    }

    protected override void OnInit()
    {
        base.OnInit();

        RegisterClientModule("kentico-community", "portal-web-admin");

        var env = Service.Resolve<IWebHostEnvironment>();

        /*
         * Ensure all pages that should be excluded from CD deployment end
         * in the correct code name
         */
        if (env.IsDevelopment())
        {
            WebPageEvents.Create.Before += EnsureLocalCodeNames;
            ContentItemEvents.Create.Before += EnsureLocalCodeNames;
        }

        QAndAAnswerDataInfo.TYPEINFO.Events.Insert.After += CreateQuestionIndexTask;
        ContentItemEvents.Publish.Execute += CreateBlogPostIndexTask;
    }

    /// <summary>
    /// Ensures that content that should be targetable by CI/CD repository.config files
    /// has codenames that can be matched by a wildcard pattern
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void EnsureLocalCodeNames(object sender, CreateWebPageEventArgs e)
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
    private void EnsureLocalCodeNames(object sender, CreateContentItemEventArgs e)
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

    /// <summary>
    /// Ensures that new <see cref="QAndAAnswerDataInfo" /> records
    /// trigger an index update of their associated questions since the Lucene
    /// integration doesn't yet track object graphs
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CreateQuestionIndexTask(object sender, ObjectEventArgs e)
    {
        if (e.Object is not QAndAAnswerDataInfo answer)
        {
            return;
        }

        /*
         * Only perform search indexing when a site is available (eg not during CI restore)
         */
        var accessor = Service.Resolve<IHttpContextAccessor>();
        if (accessor.HttpContext is null)
        {
            return;
        }
        var channelContext = Service.Resolve<IWebsiteChannelContext>();
        if (string.IsNullOrWhiteSpace(channelContext.WebsiteChannelName))
        {
            return;
        }

        int questionWebPageID = answer.QAndAAnswerDataQuestionWebPageItemID;

        var b = new ContentItemQueryBuilder()
            .ForContentType(QAndAQuestionPage.CONTENT_TYPE_NAME, queryParameters =>
            {
                _ = queryParameters
                    .ForWebsite(channelContext.WebsiteChannelName)
                    .Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), questionWebPageID))
                    .Columns(new[] { nameof(WebPageFields.WebPageItemGUID), nameof(WebPageFields.WebPageItemTreePath) });
            });

        var executor = Service.Resolve<IContentQueryExecutor>();
        var page = executor.GetWebPageResult(b, c => new { c.WebPageItemGUID, c.WebPageItemTreePath }).GetAwaiter().GetResult().FirstOrDefault();
        if (page is null)
        {
            var log = Service.Resolve<IEventLogService>();

            log.LogWarning(
                source: nameof(CreateQuestionIndexTask),
                eventCode: "MISSING_QUESTION",
                eventDescription: $"Could not find question web site page [{questionWebPageID}] for answer [{answer.QAndAAnswerDataGUID}].{Environment.NewLine}Skipping search indexing.");

            return;
        }

        var model = new IndexedItemModel
        {
            ChannelName = channelContext.WebsiteChannelName,
            LanguageName = "en-US",
            TypeName = QAndAQuestionPage.CONTENT_TYPE_NAME,
            WebPageItemGuid = page.WebPageItemGUID,
            WebPageItemTreePath = page.WebPageItemTreePath
        };

        var taskLogger = Service.Resolve<ILuceneTaskLogger>();
        taskLogger.HandleEvent(model, WebPageEvents.Publish.Name).GetAwaiter().GetResult();
    }

    public void CreateBlogPostIndexTask(object sender, PublishContentItemEventArgs args)
    {
        /*
         * Only perform search indexing when a site is available (eg not during CI restore)
         */
        var accessor = Service.Resolve<IHttpContextAccessor>();
        if (accessor.HttpContext is null)
        {
            return;
        }
        var channelContext = Service.Resolve<IWebsiteChannelContext>();
        if (string.IsNullOrWhiteSpace(channelContext.WebsiteChannelName))
        {
            return;
        }

        int contentItemID = args.ID;

        /*
         * We only need the values required for the IndexedItemModel, which come
         * from the BlogPostPage web page item that links to the updated BlogPostContent content item
         */
        var b = new ContentItemQueryBuilder()
            .ForContentType(BlogPostPage.CONTENT_TYPE_NAME, queryParameters =>
            {
                _ = queryParameters
                    .ForWebsite(channelContext.WebsiteChannelName)
                    .Linking(nameof(BlogPostPage.BlogPostPageBlogPostContent), new[] { contentItemID })
                    .Columns(new[] { nameof(WebPageFields.WebPageItemGUID), nameof(WebPageFields.WebPageItemTreePath) });
            });

        var executor = Service.Resolve<IContentQueryExecutor>();
        var page = executor.GetWebPageResult(b, c => new { c.WebPageItemGUID, c.WebPageItemTreePath }).GetAwaiter().GetResult().FirstOrDefault();
        if (page is null)
        {
            var log = Service.Resolve<IEventLogService>();

            log.LogWarning(
                source: nameof(CreateBlogPostIndexTask),
                eventCode: "MISSING_BLOGPOSTPAGE",
                eventDescription: $"Could not find blog web site page for blog content [{contentItemID}].{Environment.NewLine}Skipping search indexing.");

            return;
        }

        var model = new IndexedItemModel
        {
            ChannelName = channelContext.WebsiteChannelName,
            LanguageName = "en-US",
            TypeName = BlogPostPage.CONTENT_TYPE_NAME,
            WebPageItemGuid = page.WebPageItemGUID,
            WebPageItemTreePath = page.WebPageItemTreePath
        };

        var taskLogger = Service.Resolve<ILuceneTaskLogger>();
        taskLogger.HandleEvent(model, WebPageEvents.Publish.Name).GetAwaiter().GetResult();
    }
}
