using Kentico.Community.Portal.Web.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kentico.Community.Portal.Web.Tests.Infrastructure;

public class RateLimitingOptionsTests
{
    [Test]
    public void Partial_Config_Overrides_Only_Given_Values()
    {
        var (options, errors) = Resolve(new()
        {
            ["RateLimiting:Registration:PermitLimit"] = "1000",
            ["RateLimiting:CreateAnswer:Window"] = "00:02:00",
        });

        Assert.Multiple(() =>
        {
            Assert.That(options.Registration.PermitLimit, Is.EqualTo(1000));
            Assert.That(options.Registration.Window, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(options.Registration.SegmentsPerWindow, Is.Null);

            Assert.That(options.CreateAnswer.PermitLimit, Is.EqualTo(2));
            Assert.That(options.CreateAnswer.Window, Is.EqualTo(TimeSpan.FromMinutes(2)));
            Assert.That(options.CreateAnswer.SegmentsPerWindow, Is.EqualTo(2));

            Assert.That(options.Login.PermitLimit, Is.EqualTo(50));
            Assert.That(errors, Is.Empty);
        });
    }

    [Test]
    public void Defaults_Log_No_Errors() =>
        Assert.That(Resolve([]).Errors, Is.Empty);

    [TestCase("RateLimiting:Login:PermitLimit", "0")]
    [TestCase("RateLimiting:Login:PermitLimit", "-1")]
    [TestCase("RateLimiting:Login:Window", "00:00:00")]
    [TestCase("RateLimiting:Login:Window", "-00:01:00")]
    [TestCase("RateLimiting:Login:Window", "5m")]
    [TestCase("RateLimiting:Login:SegmentsPerWindow", "0")]
    public void Invalid_Value_Logs_Error_And_Keeps_Default_Policy(string key, string value)
    {
        var (options, errors) = Resolve(new()
        {
            [key] = value,
            // Valid change in the same policy is dropped too: the policy is used whole or not at all
            ["RateLimiting:Login:SegmentsPerWindow"] = key.EndsWith("SegmentsPerWindow") ? value : "5",
        });

        Assert.Multiple(() =>
        {
            Assert.That(options.Login.PermitLimit, Is.EqualTo(50));
            Assert.That(options.Login.Window, Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(options.Login.SegmentsPerWindow, Is.Null);
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain("RateLimiting:Login"));
        });
    }

    [Test]
    public void Invalid_Policy_Does_Not_Affect_Others()
    {
        var (options, _) = Resolve(new()
        {
            ["RateLimiting:Login:PermitLimit"] = "0",
            ["RateLimiting:Registration:PermitLimit"] = "1000",
        });

        Assert.That(options.Registration.PermitLimit, Is.EqualTo(1000));
    }

    [TestCase("RateLimiting:Login:PermitLimt", "RateLimiting:Login:PermitLimt")]
    [TestCase("RateLimiting:Logon:PermitLimit", "RateLimiting:Logon")]
    public void Unknown_Key_Logs_Error(string key, string loggedKey)
    {
        var (options, errors) = Resolve(new() { [key] = "10" });

        Assert.Multiple(() =>
        {
            Assert.That(options.Login.PermitLimit, Is.EqualTo(50));
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain(loggedKey));
        });
    }

    [Test]
    public void Keys_Are_Case_Insensitive()
    {
        var (options, errors) = Resolve(new() { ["ratelimiting:login:permitlimit"] = "7" });

        Assert.Multiple(() =>
        {
            Assert.That(options.Login.PermitLimit, Is.EqualTo(7));
            Assert.That(errors, Is.Empty);
        });
    }

    [Test]
    public async Task Startup_Check_Reads_Options()
    {
        var logger = new CapturingLogger();
        using var provider = Provider(new() { ["RateLimiting:Login:PermitLimit"] = "0" }, logger);

        foreach (var hostedService in provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        Assert.That(logger.Errors, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// The Web project's appsettings files are copied to this project's output by the project reference.
    /// </summary>
    [TestCase("Development")]
    [TestCase("CI")]
    [TestCase("Production")]
    [TestCase("qa")]
    [TestCase("uat")]
    public void Real_Appsettings_Log_No_Errors(string environment)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: false)
            .Build();

        var logger = new CapturingLogger();
        using var provider = new ServiceCollection()
            .AddSingleton<ILogger<RateLimitingOptionsSetup>>(logger)
            .AddRateLimitingOptions(config)
            .BuildServiceProvider();

        _ = provider.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        Assert.That(logger.Errors, Is.Empty);
    }

    private static (RateLimitingOptions Options, List<string> Errors) Resolve(Dictionary<string, string?> values)
    {
        var logger = new CapturingLogger();
        using var provider = Provider(values, logger);

        return (provider.GetRequiredService<IOptions<RateLimitingOptions>>().Value, logger.Errors);
    }

    private static ServiceProvider Provider(Dictionary<string, string?> values, CapturingLogger logger) =>
        new ServiceCollection()
            .AddSingleton<ILogger<RateLimitingOptionsSetup>>(logger)
            .AddRateLimitingOptions(new ConfigurationBuilder().AddInMemoryCollection(values).Build())
            .BuildServiceProvider();

    private sealed class CapturingLogger : ILogger<RateLimitingOptionsSetup>
    {
        public List<string> Errors { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error && eventId.Name == RateLimitingOptionsSetup.InvalidConfigEvent.Name)
            {
                Errors.Add(formatter(state, exception));
            }
        }
    }
}
