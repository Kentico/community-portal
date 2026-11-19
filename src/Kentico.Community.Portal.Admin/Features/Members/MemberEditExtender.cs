using System.Net;
using CMS.ContactManagement;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using CMS.Websites;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Admin.Features.Members;
using Kentico.Community.Portal.Admin.Infrastructure;
using Kentico.Community.Portal.Core;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

[assembly: PageExtender(typeof(MemberEditExtender))]

namespace Kentico.Community.Portal.Admin.Features.Members;

/// <summary>
/// Adds links to the member's public profile and linked contact on the member General tab.
/// </summary>
public class MemberEditExtender(
    IInfoProvider<MemberInfo> memberProvider,
    IInfoProvider<ContactInfo> contactProvider,
    IInfoProvider<ChannelInfo> channelProvider,
    IInfoProvider<WebsiteChannelInfo> websiteChannelProvider,
    IWebsiteChannelDomainProvider domainProvider,
    IHttpContextAccessor contextAccessor,
    AdminLinkBuilder adminLinkBuilder) : PageExtender<MemberEdit>
{
    public const string PROFILE_LINK_TEST_ID = "member-profile-link";
    public const string CONTACT_LINK_TEST_ID = "member-contact-link";
    public const string NO_CONTACT_TEST_ID = "member-no-contact";

    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;
    private readonly IInfoProvider<ContactInfo> contactProvider = contactProvider;
    private readonly IInfoProvider<ChannelInfo> channelProvider = channelProvider;
    private readonly IInfoProvider<WebsiteChannelInfo> websiteChannelProvider = websiteChannelProvider;
    private readonly IWebsiteChannelDomainProvider domainProvider = domainProvider;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly AdminLinkBuilder adminLinkBuilder = adminLinkBuilder;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        var member = await memberProvider.GetAsync(Page.ObjectId);
        if (member is null)
        {
            return;
        }

        var lines = new List<string>();

        string profileURL = BuildMemberViewUrl(member.MemberID, await GetWebsiteDomain());
        lines.Add($"""<a href="{Encode(profileURL)}" data-testid="{PROFILE_LINK_TEST_ID}" target="_blank" rel="noopener">View public profile</a>""");

        lines.Add(await GetContactLine(member.MemberEmail));

        Page.PageConfiguration.Callouts.Add(new CalloutConfiguration
        {
            Headline = "Member links",
            Content = string.Join("<br>", lines),
            ContentAsHtml = true,
            Type = CalloutType.QuickTip,
            Placement = CalloutPlacement.OnDesk,
        });
    }

    private async Task<string> GetContactLine(string? email)
    {
        string noContact = $"""<span data-testid="{NO_CONTACT_TEST_ID}">No linked contact</span>""";

        if (string.IsNullOrWhiteSpace(email))
        {
            return noContact;
        }

        // Same lookup as MemberContactManager.GetContactByEmail (Web project): newest contact with the email
        var contact = (await contactProvider.Get()
            .WhereEquals(nameof(ContactInfo.ContactEmail), email)
            .OrderByDescending(nameof(ContactInfo.ContactCreated), nameof(ContactInfo.ContactID))
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (contact is null)
        {
            return noContact;
        }

        string contactURL = adminLinkBuilder.GetPath<ContactOverview>(new()
        {
            { typeof(ContactEditSection), contact.ContactID }
        });

        return $"""<a href="{Encode(contactURL)}" data-testid="{CONTACT_LINK_TEST_ID}">View linked contact ({Encode(email)})</a>""";
    }

    /// <summary>
    /// Same path as MembershipStatsPage.BuildMemberViewUrl.
    /// </summary>
    private string BuildMemberViewUrl(int memberID, string? domain)
    {
        string path = $"/member/{memberID}";

        // No domain resolved: relative link still works where admin and website share a host
        return string.IsNullOrWhiteSpace(domain)
            ? path
            : UriHelper.BuildAbsolute(
                scheme: contextAccessor.HttpContext?.Request.Scheme ?? Uri.UriSchemeHttps,
                host: new HostString(domain),
                pathBase: string.Empty,
                path: path);
    }

    /// <summary>
    /// Primary domain of the <see cref="PortalWebSiteChannel.CODE_NAME"/> website channel.
    /// </summary>
    private async Task<string?> GetWebsiteDomain()
    {
        var channel = (await channelProvider.Get()
            .WhereEquals(nameof(ChannelInfo.ChannelName), PortalWebSiteChannel.CODE_NAME)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();
        if (channel is null)
        {
            return null;
        }

        var websiteChannel = (await websiteChannelProvider.Get()
            .WhereEquals(nameof(WebsiteChannelInfo.WebsiteChannelChannelID), channel.ChannelID)
            .TopN(1)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        return websiteChannel is null
            ? null
            : await domainProvider.GetDomain(websiteChannel.WebsiteChannelID, default);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
