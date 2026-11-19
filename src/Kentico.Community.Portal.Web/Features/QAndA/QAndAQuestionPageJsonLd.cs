using Kentico.Community.Portal.Web.Components.TagHelpers;
using Kentico.Community.Portal.Web.Features.SEO.StructuredData;

namespace Kentico.Community.Portal.Web.Features.QAndA;

/// <summary>
/// Maps a rendered Q&amp;A question page to <see cref="QAPageJsonLd"/>.
/// Every value comes from the view model the page renders.
/// </summary>
public static class QAndAQuestionPageJsonLd
{
    /// <param name="vm">The page's view model</param>
    /// <param name="pageURL">Absolute canonical URL of the question page</param>
    /// <param name="profileURL">Absolute profile URL for a member ID - only called for authors the page links</param>
    public static QAPageJsonLd Map(QAndAQuestionPageViewModel vm, string pageURL, Func<int, string> profileURL)
    {
        var question = vm.Question;
        var accepted = vm.Answers.FirstOrDefault(a => a.IsAcceptedAnswer);
        var suggested = vm.Answers
            .Where(a => a != accepted)
            .Select(a => MapAnswer(a, pageURL, profileURL))
            .ToList();

        return new(new QuestionJsonLd(
            Name: question.Title,
            Text: NullIfEmpty(question.ContentPlainText),
            DateCreated: question.DateCreated.ToString(DateTimeTagHelper.MACHINE_FORMAT),
            Author: MapAuthor(question.Author, profileURL),
            AnswerCount: vm.Answers.Count,
            UpvoteCount: question.Reactions.ReactionCount,
            AcceptedAnswer: accepted is null ? null : MapAnswer(accepted, pageURL, profileURL),
            SuggestedAnswer: suggested.Count > 0 ? suggested : null));
    }

    private static AnswerJsonLd MapAnswer(QAndAPostAnswerViewModel answer, string pageURL, Func<int, string> profileURL) =>
        new(
            Text: NullIfEmpty(answer.ContentPlainText),
            DateCreated: answer.DateCreated.ToString(DateTimeTagHelper.MACHINE_FORMAT),
            URL: $"{pageURL}#{answer.ElementID}",
            Author: MapAuthor(answer.Author, profileURL),
            UpvoteCount: answer.Reactions.ReactionCount);

    /// <summary>
    /// Matches Components/Author/Author.cshtml: members (ID != 0) link to their profile, the fallback Kentico author does not
    /// </summary>
    private static PersonJsonLd MapAuthor(QAndAAuthorViewModel author, Func<int, string> profileURL) =>
        new(author.FormattedName, author.ID == 0 ? null : profileURL(author.ID));

    private static string? NullIfEmpty(string text) => string.IsNullOrWhiteSpace(text) ? null : text;
}
