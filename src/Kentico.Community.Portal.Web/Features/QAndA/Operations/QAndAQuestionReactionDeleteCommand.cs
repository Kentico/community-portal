using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionReactionDeleteCommand(
    int MemberId,
    int WebPageItemId,
    DiscussionReactionTypes ReactionType)
    : ICommand<Result>;

public class QAndAQuestionReactionDeleteCommandHandler(
    DataItemCommandTools tools,
    IInfoProvider<QAndAQuestionReactionInfo> provider)
    : DataItemCommandHandler<QAndAQuestionReactionDeleteCommand, Result>(tools)
{
    private readonly IInfoProvider<QAndAQuestionReactionInfo> provider = provider;

    public override async Task<Result> Handle(QAndAQuestionReactionDeleteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var reaction = await provider.Get()
                .WhereEquals(nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionMemberID), request.MemberId)
                .WhereEquals(nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionWebPageItemID), request.WebPageItemId)
                .WhereEquals(nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionType), request.ReactionType switch
                {
                    DiscussionReactionTypes.Upvote => nameof(DiscussionReactionTypes.Upvote),
                    _ => throw new InvalidOperationException($"Unknown reaction type: {request.ReactionType}")
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (reaction is not null)
            {
                await provider.DeleteAsync(reaction);
            }
        }
        catch (Exception ex)
        {
            return Result.Failure($"Could not delete reaction for question [{request.WebPageItemId}] and member [{request.MemberId}]: {ex}");
        }

        return Result.Success();
    }
}
