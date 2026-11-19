using System.Net;
using Kentico.Community.Portal.Web.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Tests.Infrastructure;

/// <summary>
/// Each verification is logged with its score, for tuning the threshold. No token, IP or email.
/// </summary>
public class CaptchaValidatorTests
{
    private const string TOKEN = "client-token-123";
    private const string SECRET = "secret-456";

    private CapturingLogger logger = null!;

    [SetUp]
    public void SetUp() => logger = new CapturingLogger();

    [Test]
    public async Task Logs_A_Passed_Verification()
    {
        var sut = CreateValidator("""{ "success": true, "score": 0.9, "action": "register" }""", threshold: 0.2);

        var result = await sut.ValidateCaptcha(new ClientResponse(), "Registration");

        Assert.That(result.IsSuccess, Is.True);
        var entry = logger.Entries.Single();
        Assert.Multiple(() =>
        {
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Information));
            Assert.That(entry.State, Does.Contain(Field("Source", "Registration")));
            Assert.That(entry.State, Does.Contain(Field("Score", 0.9)));
            Assert.That(entry.State, Does.Contain(Field("Action", "register")));
            Assert.That(entry.State, Does.Contain(Field("Success", true)));
            Assert.That(entry.State, Does.Contain(Field("Threshold", 0.2)));
            Assert.That(entry.State, Does.Contain(Field("Passed", true)));
        });
    }

    [Test]
    public async Task Logs_A_Score_Below_The_Threshold_As_Failed()
    {
        var sut = CreateValidator("""{ "success": true, "score": 0.1, "action": "support" }""", threshold: 0.2);

        var result = await sut.ValidateCaptcha(new ClientResponse(), "Support");

        Assert.That(result.IsSuccess, Is.False);
        var entry = logger.Entries.Single();
        Assert.Multiple(() =>
        {
            Assert.That(entry.State, Does.Contain(Field("Score", 0.1)));
            Assert.That(entry.State, Does.Contain(Field("Success", true)));
            Assert.That(entry.State, Does.Contain(Field("Passed", false)));
        });
    }

    [Test]
    public async Task Logs_A_Google_Failure_As_Failed()
    {
        var sut = CreateValidator("""{ "success": false, "error-codes": ["timeout-or-duplicate"] }""", threshold: 0);

        var result = await sut.ValidateCaptcha(new ClientResponse(), "Support");

        Assert.That(result.IsSuccess, Is.False);
        var entry = logger.Entries.Single();
        Assert.Multiple(() =>
        {
            Assert.That(entry.State, Does.Contain(Field("Success", false)));
            Assert.That(entry.State, Does.Contain(Field("Passed", false)));
        });
    }

    [Test]
    public async Task Does_Not_Log_The_Token_Or_Secret()
    {
        var sut = CreateValidator("""{ "success": true, "score": 0.9, "action": "register" }""", threshold: 0.2);

        _ = await sut.ValidateCaptcha(new ClientResponse(), "Registration");

        var entry = logger.Entries.Single();
        string logged = entry.Message + string.Join(";", entry.State.Select(kv => $"{kv.Key}={kv.Value}"));
        Assert.That(logged, Does.Not.Contain(TOKEN).And.Not.Contain(SECRET));
    }

    [Test]
    public async Task Logs_Nothing_When_Validation_Is_Disabled()
    {
        var sut = CreateValidator("{}", threshold: 0.2, isValidationDisabled: true);

        var result = await sut.ValidateCaptcha(new ClientResponse(), "Registration");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.Entries, Is.Empty);
    }

    private CaptchaValidator CreateValidator(string json, double threshold, bool isValidationDisabled = false)
    {
        var settings = new ReCaptchaSettings
        {
            IsValidationDisabled = isValidationDisabled,
            SecretKey = SECRET,
            ScoreThreshold = threshold,
        };
        var factory = Substitute.For<IHttpClientFactory>();
        _ = factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(new JsonHandler(json)));

        return new CaptchaValidator(Options.Create(settings), factory, logger);
    }

    private static KeyValuePair<string, object?> Field(string key, object? value) => new(key, value);

    private class ClientResponse : ICaptchaClientResponse
    {
        public string CaptchaToken { get; set; } = TOKEN;
    }

    private class JsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
    }

    private record LogEntry(LogLevel Level, string Message, IReadOnlyList<KeyValuePair<string, object?>> State);

    private class CapturingLogger : ILogger<CaptchaValidator>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        // Copied: the logging generator reuses its state object after Log returns
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add(new(
                logLevel,
                formatter(state, exception),
                (state as IEnumerable<KeyValuePair<string, object?>>)?.ToList() ?? []));
    }
}
