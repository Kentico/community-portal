using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAAnswerReactionCreateCommand(
    int MemberId,
    int AnswerId,
    DiscussionReactionTypes ReactionType)
    : ICommand<Result<int>>;

public class QAndAAnswerReactionCreateCommandHandler(
    DataItemCommandTools tools,
    TimeProvider clock,
    IInfoProvider<QAndAAnswerReactionInfo> provider)
    : DataItemCommandHandler<QAndAAnswerReactionCreateCommand, Result<int>>(tools)
{
    private readonly TimeProvider clock = clock;
    private readonly IInfoProvider<QAndAAnswerReactionInfo> provider = provider;

    public override async Task<Result<int>> Handle(QAndAAnswerReactionCreateCommand request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().DateTime;

        var reaction = new QAndAAnswerReactionInfo()
        {
            QAndAAnswerReactionGUID = Guid.NewGuid(),
            QAndAAnswerReactionMemberID = request.MemberId,
            QAndAAnswerReactionAnswerID = request.AnswerId,
            QAndAAnswerReactionType = request.ReactionType switch
            {
                DiscussionReactionTypes.Upvote => nameof(DiscussionReactionTypes.Upvote),
                _ => throw new InvalidOperationException($"Unknown reaction type: {request.ReactionType}")
            },
            QAndAAnswerReactionDateModified = now
        };

        try
        {
            await provider.SetAsync(reaction);
        }
        catch (Exception ex)
        {
            return Result.Failure<int>($"Could not create reaction for answer [{request.AnswerId}]: {ex}");
        }

        return Result.Success(reaction.QAndAAnswerReactionID);
    }
}
