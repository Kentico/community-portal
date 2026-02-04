namespace Kentico.Community.Portal.Web.Features.QAndA;

/// <summary>
/// Constants for Q&A rate limiting policy names
/// </summary>
public static class QAndARateLimitingConstants
{
    /// <summary>
    /// Rate limiting policy for creating new questions
    /// </summary>
    public const string CreateQuestion = "QAndA_CreateQuestion";

    /// <summary>
    /// Rate limiting policy for creating new answers
    /// </summary>
    public const string CreateAnswer = "QAndA_CreateAnswer";

    /// <summary>
    /// Rate limiting policy for updating questions
    /// </summary>
    public const string UpdateQuestion = "QAndA_UpdateQuestion";

    /// <summary>
    /// Rate limiting policy for updating answers
    /// </summary>
    public const string UpdateAnswer = "QAndA_UpdateAnswer";

    /// <summary>
    /// Rate limiting policy for updating question subscribe/unsubscribe
    /// </summary>
    public const string UpdateQuestionSubscription = "QAndA_UpdateQuestionSubscription";

    /// <summary>
    /// Rate limiting policy for toggling answer reactions (upvotes)
    /// </summary>
    public const string UpdateAnswerReaction = "QAndA_UpdateAnswerReaction";

    /// <summary>
    /// Rate limiting policy for toggling question reactions (upvotes)
    /// </summary>
    public const string UpdateQuestionReaction = "QAndA_UpdateQuestionReaction";
}
