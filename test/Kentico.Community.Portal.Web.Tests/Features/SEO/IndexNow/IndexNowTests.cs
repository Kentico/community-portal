using System.Net;
using System.Text.Json;
using CMS.Websites;
using Kentico.Community.Portal.Web.Features.SEO.IndexNow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Tests.Features.SEO.IndexNow;

public class IndexNowOptionsTests
{
    [TestCase("abcd1234", true)]
    [TestCase("e2e-indexnow-test-key", true)]
    [TestCase("short", false)]
    [TestCase("has space1", false)]
    [TestCase("bad/char12", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void Validates_Key(string? key, bool expected) =>
        Assert.That(IndexNowOptions.IsValidKey(key), Is.EqualTo(expected));

    [Test]
    public void Max_Length_Key_Is_Valid() =>
        Assert.Multiple(() =>
        {
            Assert.That(IndexNowOptions.IsValidKey(new string('a', 128)), Is.True);
            Assert.That(IndexNowOptions.IsValidKey(new string('a', 129)), Is.False);
        });

    [TestCase(IndexNowOptions.DEFAULT_ENDPOINT, true)]
    [TestCase("", false)]
    [TestCase("http://api.indexnow.org/indexnow", false)]
    [TestCase("not a url", false)]
    public void Submission_Needs_Https_Endpoint(string endpoint, bool expected) =>
        Assert.That(new IndexNowOptions { Key = "abcd1234", Endpoint = endpoint }.IsSubmissionEnabled, Is.EqualTo(expected));

    [Test]
    public void Submission_Needs_Key() =>
        Assert.That(new IndexNowOptions().IsSubmissionEnabled, Is.False);
}

public class IndexNowEligibilityTests
{
    [TestCase(false, null, false, true)]
    [TestCase(false, "", false, true)]
    [TestCase(false, "index, follow", false, true)]
    [TestCase(true, null, false, false)]
    [TestCase(false, "noindex, follow", false, false)]
    [TestCase(false, "NOINDEX", false, false)]
    [TestCase(false, null, true, false)]
    public void Matches_Sitemap_And_Robots_Rules(bool excludeFromSitemap, string? robots, bool isBlogDiscussion, bool expected) =>
        Assert.That(IndexNowEligibility.IsIndexable(excludeFromSitemap, robots, isBlogDiscussion), Is.EqualTo(expected));
}

public class IndexNowRequestTests
{
    private const string KEY = "abcd1234";

    [Test]
    public void Groups_By_Host_With_Key_Location()
    {
        var requests = IndexNowRequest.Create(KEY,
        [
            "https://community.kentico.com/blog/a",
            "https://community.kentico.com/q-and-a/b",
            "https://other.example/c",
        ]).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(requests, Has.Count.EqualTo(2));
            Assert.That(requests[0].Host, Is.EqualTo("community.kentico.com"));
            Assert.That(requests[0].Key, Is.EqualTo(KEY));
            Assert.That(requests[0].KeyLocation, Is.EqualTo($"https://community.kentico.com/{KEY}.txt"));
            Assert.That(requests[0].UrlList, Is.EqualTo(new[] { "https://community.kentico.com/blog/a", "https://community.kentico.com/q-and-a/b" }));
            Assert.That(requests[1].Host, Is.EqualTo("other.example"));
        });
    }

    [Test]
    public void Drops_Duplicates_And_Relative_URLs()
    {
        var requests = IndexNowRequest.Create(KEY, ["https://a.example/x", "https://a.example/x", "/relative"]).ToList();

        Assert.That(requests.Single().UrlList, Is.EqualTo(new[] { "https://a.example/x" }));
    }

    [Test]
    public void Splits_At_Protocol_Limit()
    {
        var urls = Enumerable.Range(0, IndexNowOptions.MAX_URLS_PER_REQUEST + 1).Select(i => $"https://a.example/{i}");

        var requests = IndexNowRequest.Create(KEY, urls).ToList();

        Assert.That(requests.Select(r => r.UrlList.Count), Is.EqualTo(new[] { IndexNowOptions.MAX_URLS_PER_REQUEST, 1 }));
    }

    [Test]
    public void Serializes_Protocol_Property_Names()
    {
        string json = JsonSerializer.Serialize(new IndexNowRequest("a.example", KEY, $"https://a.example/{KEY}.txt", ["https://a.example/x"]));

        Assert.That(json, Is.EqualTo($$"""{"host":"a.example","key":"{{KEY}}","keyLocation":"https://a.example/{{KEY}}.txt","urlList":["https://a.example/x"]}"""));
    }
}

public class IndexNowClientTests
{
    private static IndexNowClient CreateClient(HttpMessageHandler handler, IndexNowOptions? options = null) =>
        new(
            new HttpClient(handler),
            Monitor(options ?? new IndexNowOptions { Key = "abcd1234", RequestTimeout = TimeSpan.FromMilliseconds(200) }),
            NullLogger<IndexNowClient>.Instance);

    [TestCase(HttpStatusCode.OK)]
    [TestCase(HttpStatusCode.Accepted)]
    public async Task Success_Status_Succeeds(HttpStatusCode status)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status));

        var result = await CreateClient(handler).Submit(["https://a.example/x"], default);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(handler.Requests.Single().RequestUri, Is.EqualTo(new Uri(IndexNowOptions.DEFAULT_ENDPOINT)));
            Assert.That(handler.Bodies.Single(), Does.Contain("\"urlList\":[\"https://a.example/x\"]"));
        });
    }

    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase((HttpStatusCode)422)]
    [TestCase(HttpStatusCode.TooManyRequests)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public async Task Rejection_Fails_Without_Throwing(HttpStatusCode status)
    {
        var result = await CreateClient(new StubHandler(_ => new HttpResponseMessage(status))).Submit(["https://a.example/x"], default);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task Unavailable_Endpoint_Fails_Without_Throwing()
    {
        var result = await CreateClient(new StubHandler((HttpRequestMessage _) => throw new HttpRequestException("connection refused"))).Submit(["https://a.example/x"], default);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task Slow_Endpoint_Times_Out_Without_Throwing()
    {
        var handler = new StubHandler(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var result = await CreateClient(handler).Submit(["https://a.example/x"], default);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task Disabled_Submits_Nothing()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var result = await CreateClient(handler, new IndexNowOptions { Key = "abcd1234", Endpoint = "" }).Submit(["https://a.example/x"], default);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(handler.Requests, Is.Empty);
        });
    }

    internal static IOptionsMonitor<IndexNowOptions> Monitor(IndexNowOptions options)
    {
        var monitor = Substitute.For<IOptionsMonitor<IndexNowOptions>>();
        _ = monitor.CurrentValue.Returns(options);
        return monitor;
    }

    private class StubHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : this(_ => Task.FromResult(respond(null!))) { }

        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            return await respond(cancellationToken);
        }
    }
}

public class IndexNowQueueTests
{
    [Test]
    public async Task Batch_Is_Deduplicated()
    {
        var queue = new IndexNowQueue();
        var page = new IndexNowPage(1, "devnet", "en-US");
        _ = queue.Enqueue(page);
        _ = queue.Enqueue(page);
        _ = queue.Enqueue(page with { WebPageItemID = 2 });

        var batch = await queue.ReadBatch(TimeSpan.Zero, TimeProvider.System, default);

        Assert.That(batch, Has.Count.EqualTo(2));
    }

    [Test]
    public void Enqueue_Never_Blocks_When_Full()
    {
        var queue = new IndexNowQueue();

        bool all = Enumerable.Range(0, IndexNowQueue.CAPACITY + 10)
            .All(i => queue.Enqueue(new IndexNowPage(i, "devnet", "en-US")));

        Assert.That(all, Is.True);
    }
}

public class IndexNowPublishHandlerTests
{
    private static PublishWebPageEventData Published() =>
        new() { ID = 42, WebsiteChannelName = "devnet", ContentLanguageName = "en-US" };

    [Test]
    public void Queues_Published_Page_When_Enabled()
    {
        var queue = new IndexNowQueue();
        var handler = new IndexNowPublishHandler(queue, IndexNowClientTests.Monitor(new() { Key = "abcd1234" }), NullLogger<IndexNowPublishHandler>.Instance);

        _ = handler.Handle(Published());

        Assert.That(queue.Reader.TryRead(out var page) ? page : null, Is.EqualTo(new IndexNowPage(42, "devnet", "en-US")));
    }

    [Test]
    public void Queues_Nothing_When_Submission_Disabled()
    {
        var queue = new IndexNowQueue();
        var handler = new IndexNowPublishHandler(queue, IndexNowClientTests.Monitor(new() { Key = "abcd1234", Endpoint = "" }), NullLogger<IndexNowPublishHandler>.Instance);

        _ = handler.Handle(Published());

        Assert.That(queue.Reader.TryRead(out _), Is.False);
    }
}

public class IndexNowKeyControllerTests
{
    [Test]
    public void Serves_Configured_Key()
    {
        var controller = new IndexNowKeyController(IndexNowClientTests.Monitor(new() { Key = "abcd1234" }));

        var result = controller.KeyFile("abcd1234");

        Assert.That(result, Is.TypeOf<ContentResult>());
        var content = (ContentResult)result;
        Assert.Multiple(() =>
        {
            Assert.That(content.Content, Is.EqualTo("abcd1234"));
            Assert.That(content.ContentType, Does.StartWith("text/plain"));
        });
    }

    [TestCase("abcd1234", "other-key-1")]
    [TestCase("abcd1234", "ABCD1234")]
    [TestCase("", "")]
    public void Other_Names_Are_Not_Found(string configured, string requested) =>
        Assert.That(
            new IndexNowKeyController(IndexNowClientTests.Monitor(new() { Key = configured })).KeyFile(requested),
            Is.TypeOf<NotFoundResult>());
}
