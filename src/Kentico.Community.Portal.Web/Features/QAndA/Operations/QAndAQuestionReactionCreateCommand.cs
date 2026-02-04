using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionReactionCreateCommand(
    int MemberId,
    int WebPageItemId,
    DiscussionReactionTypes ReactionType)
    : ICommand<Result<int>>;

public class QAndAQuestionReactionCreateCommandHandler(
    DataItemCommandTools tools,
    TimeProvider clock,
    IInfoProvider<QAndAQuestionReactionInfo> provider)
    : DataItemCommandHandler<QAndAQuestionReactionCreateCommand, Result<int>>(tools)
{
    private readonly TimeProvider clock = clock;
    private readonly IInfoProvider<QAndAQuestionReactionInfo> provider = provider;

    public override async Task<Result<int>> Handle(QAndAQuestionReactionCreateCommand request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().DateTime;

        var reaction = new QAndAQuestionReactionInfo()
        {
            QAndAQuestionReactionGUID = Guid.NewGuid(),
            QAndAQuestionReactionMemberID = request.MemberId,
            QAndAQuestionReactionWebPageItemID = request.WebPageItemId,
            QAndAQuestionReactionType = request.ReactionType switch
            {
                DiscussionReactionTypes.Upvote => nameof(DiscussionReactionTypes.Upvote),
                _ => throw new InvalidOperationException($"Unknown reaction type: {request.ReactionType}")
            },
            QAndAQuestionReactionDateModified = now
        };

        try
        {
            await provider.SetAsync(reaction);
        }
        catch (Exception ex)
        {
            return Result.Failure<int>($"Could not create reaction for question [{request.WebPageItemId}]: {ex}");
        }

        return Result.Success(reaction.QAndAQuestionReactionID);
    }
}
