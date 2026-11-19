using Kentico.Community.Portal.Web.Features.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Kentico.Community.Portal.Web.Tests.Features.Testing;

public class E2ETestingGateTests
{
    [TestCase("Development")]
    [TestCase("CI")]
    public void Flag_On_In_Development_Or_CI_Enables(string environment) =>
        Assert.That(E2ETestingGate.IsEnabled(Config(true), Env(environment)), Is.True);

    [TestCase("Production")]
    [TestCase("qa")]
    [TestCase("uat")]
    public void Flag_On_In_Any_Other_Environment_Throws(string environment)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => E2ETestingGate.IsEnabled(Config(true), Env(environment)));

        Assert.That(ex!.Message, Does.Contain(environment));
    }

    [TestCase("Development")]
    [TestCase("Production")]
    public void Flag_Off_Disables(string environment) =>
        Assert.That(E2ETestingGate.IsEnabled(Config(false), Env(environment)), Is.False);

    [Test]
    public void Flag_Missing_Disables() =>
        Assert.That(E2ETestingGate.IsEnabled(new ConfigurationBuilder().Build(), Env("Development")), Is.False);

    private static IConfiguration Config(bool enabled) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [E2ETestingGate.ENABLED_KEY] = enabled.ToString() })
            .Build();

    private static IHostEnvironment Env(string name)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(name);

        return env;
    }
}
