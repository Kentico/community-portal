using System.Text.Json.Serialization;

namespace Kentico.Community.Portal.Web.Features.SEO.StructuredData;

/// <summary>
/// https://schema.org/QAPage - one question per page, answers submitted by members.
/// </summary>
/// <param name="MainEntity">The page's question</param>
public record QAPageJsonLd(
    [property: JsonPropertyName("mainEntity")] QuestionJsonLd MainEntity) : JsonLdNode("QAPage");

/// <summary>
/// https://schema.org/Question, nested in <see cref="QAPageJsonLd"/>.
/// Only emit values that are visibly rendered on the page.
/// </summary>
/// <param name="Name">Question title</param>
/// <param name="Text">Plain text of the rendered question body</param>
/// <param name="DateCreated">ISO 8601 UTC, same value as the page's <c>&lt;time datetime&gt;</c></param>
/// <param name="Author">Question author</param>
/// <param name="AnswerCount">Number of answers rendered</param>
/// <param name="UpvoteCount">Upvote count shown on the question</param>
/// <param name="AcceptedAnswer">Answer the asker accepted, if any</param>
/// <param name="SuggestedAnswer">All other answers, when there are any</param>
public record QuestionJsonLd(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("dateCreated")] string DateCreated,
    [property: JsonPropertyName("author")] PersonJsonLd Author,
    [property: JsonPropertyName("answerCount")] int AnswerCount,
    [property: JsonPropertyName("upvoteCount")] int UpvoteCount,
    [property: JsonPropertyName("acceptedAnswer")] AnswerJsonLd? AcceptedAnswer,
    [property: JsonPropertyName("suggestedAnswer")] IReadOnlyList<AnswerJsonLd>? SuggestedAnswer)
{
    [JsonPropertyName("@type")]
    [JsonPropertyOrder(-1)]
    public string Type => "Question";
}

/// <summary>
/// https://schema.org/Answer, nested in <see cref="QuestionJsonLd"/>.
/// Only emit values that are visibly rendered on the page.
/// </summary>
/// <param name="Text">Plain text of the rendered answer body</param>
/// <param name="DateCreated">ISO 8601 UTC, same value as the answer's <c>&lt;time datetime&gt;</c></param>
/// <param name="URL">Absolute page URL with the answer's anchor fragment</param>
/// <param name="Author">Answer author</param>
/// <param name="UpvoteCount">Upvote count shown on the answer</param>
public record AnswerJsonLd(
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("dateCreated")] string DateCreated,
    [property: JsonPropertyName("url")] string URL,
    [property: JsonPropertyName("author")] PersonJsonLd Author,
    [property: JsonPropertyName("upvoteCount")] int UpvoteCount)
{
    [JsonPropertyName("@type")]
    [JsonPropertyOrder(-1)]
    public string Type => "Answer";
}
