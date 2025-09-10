namespace Kentico.Community.Portal.Web.Features.QAndA;

/// <summary>
/// Constants for Q&A rate limiting policy names
/// </summary>
public static class QAndARateLimitingConstants
{
    /// <summary>
    /// Rate limiting policy for creating new questions - 3 per 10 minutes per user
    /// </summary>
    public const string CreateQuestion = "QAndA_CreateQuestion";

    /// <summary>
    /// Rate limiting policy for creating new answers - 10 per 5 minutes per user
    /// </summary>
    public const string CreateAnswer = "QAndA_CreateAnswer";

    /// <summary>
    /// Rate limiting policy for updating questions - 5 per 5 minutes per user
    /// </summary>
    public const string UpdateQuestion = "QAndA_UpdateQuestion";

    /// <summary>
    /// Rate limiting policy for updating answers - 10 per 5 minutes per user
    /// </summary>
    public const string UpdateAnswer = "QAndA_UpdateAnswer";
}
