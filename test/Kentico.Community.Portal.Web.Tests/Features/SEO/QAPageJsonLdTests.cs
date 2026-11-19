using System.Text.Json;
using CMS.Websites;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Web.Features.QAndA;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Features.SEO.StructuredData;
using Kentico.Community.Portal.Web.Rendering;

namespace Kentico.Community.Portal.Web.Tests.Features.SEO;

public class QAPageJsonLdTests
{
    private const string PAGE_URL = "https://example.com/q-and-a/q/question";

    private static string Profile(int id) => $"https://example.com/member/{id}";

    private static QAndAPostQuestionViewModel Question() =>
        new(
            new QAndAQuestionPage
            {
                SystemFields = new WebPageFields(),
                BasicItemTitle = "Title",
                QAndAQuestionPageContent = "Some **body**",
                QAndAQuestionPageDateCreated = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                CoreTaxonomyDXTopics = [],
            },
            QAndAQuestionPermissions.NoPermissions,
            null,
            new QAndATaxonomiesQueryResponse([], [], []),
            new QAndAAuthorViewModel(1, "Asker", "asker", null),
            new MarkdownRenderer(),
            false);

    private static QAndAPostAnswerViewModel Answer(Guid guid, bool accepted, int authorID = 2) => new()
    {
        GUID = guid,
        Author = new QAndAAuthorViewModel(authorID, "Answerer", "answerer", null),
        ContentPlainText = "Answer",
        DateCreated = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
        IsAcceptedAnswer = accepted,
    };

    [Test]
    public void Accepted_Answer_Is_Separate_From_Suggested_Answers()
    {
        var accepted = Guid.NewGuid();
        var other = Guid.NewGuid();
        var vm = new QAndAQuestionPageViewModel(Question(), [Answer(other, false), Answer(accepted, true)]);

        var page = QAndAQuestionPageJsonLd.Map(vm, PAGE_URL, Profile);

        Assert.Multiple(() =>
        {
            Assert.That(page.MainEntity.Name, Is.EqualTo("Title"));
            Assert.That(page.MainEntity.Text, Is.EqualTo("Some body"));
            Assert.That(page.MainEntity.AnswerCount, Is.EqualTo(2));
            Assert.That(page.MainEntity.AcceptedAnswer!.URL, Is.EqualTo($"{PAGE_URL}#answer_{accepted:N}"));
            Assert.That(page.MainEntity.SuggestedAnswer!.Select(a => a.URL), Is.EqualTo(new[] { $"{PAGE_URL}#answer_{other:N}" }));
            Assert.That(page.MainEntity.DateCreated, Is.EqualTo("2026-01-02T03:04:05Z"));
            Assert.That(page.MainEntity.Author, Is.EqualTo(new PersonJsonLd("Asker", "https://example.com/member/1")));
        });
    }

    [Test]
    public void Unanswered_Question_Has_Zero_Answers_And_No_Answer_Properties()
    {
        var vm = new QAndAQuestionPageViewModel(Question(), []);

        using var json = JsonDocument.Parse(JsonLdSerializer.Serialize(QAndAQuestionPageJsonLd.Map(vm, PAGE_URL, Profile)));
        var question = json.RootElement.GetProperty("mainEntity");

        Assert.Multiple(() =>
        {
            Assert.That(json.RootElement.GetProperty("@type").GetString(), Is.EqualTo("QAPage"));
            Assert.That(question.GetProperty("@type").GetString(), Is.EqualTo("Question"));
            Assert.That(question.GetProperty("answerCount").GetInt32(), Is.Zero);
            Assert.That(question.TryGetProperty("acceptedAnswer", out _), Is.False);
            Assert.That(question.TryGetProperty("suggestedAnswer", out _), Is.False);
        });
    }

    [Test]
    public void Unlinked_Author_Has_No_URL()
    {
        var vm = new QAndAQuestionPageViewModel(Question(), [Answer(Guid.NewGuid(), false, authorID: 0)]);

        var page = QAndAQuestionPageJsonLd.Map(vm, PAGE_URL, Profile);

        Assert.That(page.MainEntity.SuggestedAnswer![0].Author.URL, Is.Null);
    }
}
