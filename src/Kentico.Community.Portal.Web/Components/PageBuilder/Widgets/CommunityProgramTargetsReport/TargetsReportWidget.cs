using CMS.Websites.Routing;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramTargetsReport;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramTargetsReport.Operations;
using Kentico.Community.Portal.Web.Membership;
using Kentico.PageBuilder.Web.Mvc;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: TargetsReportWidget.IDENTIFIER,
    name: TargetsReportWidget.NAME,
    viewComponentType: typeof(TargetsReportWidget),
    propertiesType: typeof(TargetsReportWidgetProperties),
    Description = "Shows progress towards annual community program targets for the current member",
    IconClass = KenticoIcons.CHECKLIST,
    AllowCache = false)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramTargetsReport;

public class TargetsReportWidget(
    IMediator mediator,
    IWebsiteChannelContext channelContext,
    TimeProvider timeProvider,
    ILogger<TargetsReportWidget> logger,
    UserManager<CommunityMember> userManager) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Widget.CommunityProgramTargetsReport";
    public const string NAME = "Targets report";

    public async Task<IViewComponentResult> InvokeAsync(TargetsReportWidgetProperties properties)
    {
        int currentMemberID = CommunityMember.GetMemberIDFromClaim(HttpContext).Value;
        int year = timeProvider.GetLocalNow().Year;

        if (currentMemberID <= 0)
        {
            return View(
                "~/Components/PageBuilder/Widgets/CommunityProgramTargetsReport/TargetsReport.cshtml",
                TargetReportsViewModel.Error(properties, "This content is only visible to authenticated members.", year));
        }

        try
        {
            // Get current member to check if internal employee
            var currentMember = await userManager.GetUserAsync(HttpContext.User);
            bool isInternalEmployee = currentMember?.IsInternalEmployee ?? false;

            IReadOnlyList<MemberSummary> availableMembers = [];
            int? memberID = currentMember?.Id;
            if (isInternalEmployee)
            {
                // Fetch all members in program for dropdown
                var membersResponse = await mediator.Send(new ProgramMembersAllQuery(), HttpContext.RequestAborted);
                availableMembers = [.. membersResponse.Members
                    .Select(m => new MemberSummary(m.Id, m.DisplayName, m.Email ?? "", m.ProgramStatus))
                    .OrderBy(m => m.DisplayName)];
                memberID = properties.EmulatedMemberID;
            }

            var resp = await mediator.Send(new TargetsReportQuery(
                MemberId: memberID ?? 0,
                ChannelName: channelContext.WebsiteChannelName,
                Year: year), HttpContext.RequestAborted);

            return View(
                "~/Components/PageBuilder/Widgets/CommunityProgramTargetsReport/TargetsReport.cshtml",
                TargetReportsViewModel.Create(
                    properties,
                    resp,
                    isInternalEmployee: isInternalEmployee,
                    currentAuthMemberId: currentMemberID,
                    emulatedMemberId: memberID ?? Maybe<int>.None,
                    availableMembers: availableMembers));
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to render community program targets report for member {MemberId}", currentMemberID);

            return View(
                "~/Components/PageBuilder/Widgets/CommunityProgramTargetsReport/TargetsReport.cshtml",
                TargetReportsViewModel.Error(properties, "Unable to load program targets right now.", year));
        }
    }
}
