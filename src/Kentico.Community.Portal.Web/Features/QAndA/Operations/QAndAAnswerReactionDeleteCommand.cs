using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerReactionDeleteCommand(
    int MemberId,
    int AnswerId,
    DiscussionReactionTypes ReactionType)
    : ICommand<Result>;

public class QAndAAnswerReactionDeleteCommandHandler(
    DataItemCommandTools tools,
    IInfoProvider<QAndAAnswerReactionInfo> provider)
    : DataItemCommandHandler<QAndAAnswerReactionDeleteCommand, Result>(tools)
{
    private readonly IInfoProvider<QAndAAnswerReactionInfo> provider = provider;

    public override async Task<Result> Handle(QAndAAnswerReactionDeleteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var reaction = await provider.Get()
                .WhereEquals(nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionMemberID), request.MemberId)
                .WhereEquals(nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionAnswerID), request.AnswerId)
                .WhereEquals(nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionType), request.ReactionType switch
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
            return Result.Failure($"Could not delete reaction for answer [{request.AnswerId}] and member [{request.MemberId}]: {ex}");
        }

        return Result.Success();
    }
}
