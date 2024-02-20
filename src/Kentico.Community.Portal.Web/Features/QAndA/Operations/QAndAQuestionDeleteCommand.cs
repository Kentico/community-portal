using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionDeleteCommand(QAndAQuestionPage Question, int WebsiteChannelID) : ICommand<Unit>;
public class QAndAQuestionDeleteCommandHandler(WebPageCommandTools tools, IInfoProvider<UserInfo> users, IInfoProvider<QAndAAnswerDataInfo> provider) : WebPageCommandHandler<QAndAQuestionDeleteCommand, Unit>(tools)
{
    private readonly IInfoProvider<UserInfo> users = users;
    private readonly IInfoProvider<QAndAAnswerDataInfo> provider = provider;

    public override async Task<Unit> Handle(QAndAQuestionDeleteCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetPublicMemberContentAuthor();

        var webPageManager = WebPageManagerFactory.Create(request.WebsiteChannelID, user.UserID);

        provider.BulkDelete(new WhereCondition().WhereEquals(nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID), request.Question.SystemFields.WebPageItemID));

        await webPageManager.Delete(request.Question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);

        return Unit.Value;
    }
}
