using CMS.DataEngine;
using CMS.DataEngine.Internal;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Infrastructure;
using MediatR;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionDeleteCommand(QAndAQuestionPage Question) : ICommand<Unit>;
public class QAndAQuestionDeleteCommandHandler : WebPageCommandHandler<QAndAQuestionDeleteCommand, Unit>
{
    private readonly IInfoProvider<UserInfo> users;
    private readonly IQAndAAnswerDataInfoProvider provider;

    public QAndAQuestionDeleteCommandHandler(WebPageCommandTools tools, IInfoProvider<UserInfo> users, IQAndAAnswerDataInfoProvider provider) : base(tools)
    {
        this.users = users;
        this.provider = provider;
    }

    public override async Task<Unit> Handle(QAndAQuestionDeleteCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetPublicMemberContentAuthor();

        var webPageManager = WebPageManagerFactory.Create(WebsiteChannelContext.WebsiteChannelID, user.UserID);

        provider.BulkDelete(new WhereCondition().WhereEquals(nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID), request.Question.SystemFields.WebPageItemID));

        await webPageManager.Delete(request.Question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);

        return Unit.Value;
    }
}
