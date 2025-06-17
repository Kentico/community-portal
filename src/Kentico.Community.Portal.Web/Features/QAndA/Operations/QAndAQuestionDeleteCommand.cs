using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionDeleteCommand(QAndAQuestionPage Question) : ICommand<Result>;
public class QAndAQuestionDeleteCommandHandler(WebPageCommandTools tools, IInfoProvider<UserInfo> users, IInfoProvider<QAndAAnswerDataInfo> provider) : WebPageCommandHandler<QAndAQuestionDeleteCommand, Result>(tools)
{
    private readonly IInfoProvider<UserInfo> users = users;
    private readonly IInfoProvider<QAndAAnswerDataInfo> provider = provider;

    public override async Task<Result> Handle(QAndAQuestionDeleteCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetPublicMemberContentAuthor();
        var webPageManager = WebPageManagerFactory.Create(request.Question.SystemFields.WebPageItemWebsiteChannelId, user.UserID);

        try
        {
            using var transaction = new CMSTransactionScope();
            provider.BulkDelete(new WhereCondition().WhereEquals(nameof(QAndAAnswerDataInfo.QAndAAnswerDataQuestionWebPageItemID), request.Question.SystemFields.WebPageItemID));

            await webPageManager.Delete(request.Question.SystemFields.WebPageItemID, PortalWebSiteChannel.DEFAULT_LANGUAGE, cancellationToken);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Could not delete question [{request.Question.SystemFields.WebPageItemTreePath}]: {ex}");
        }

        return Result.Success();
    }
}
