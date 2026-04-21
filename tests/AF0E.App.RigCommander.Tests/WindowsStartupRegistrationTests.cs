using FluentAssertions;
using RigCommander.Services;
using RigCommander.Tests.TestDoubles;
using Xunit;

namespace RigCommander.Tests;

#pragma warning disable CA1707 // Identifiers should not contain underscores

public sealed class WindowsStartupRegistrationTests
{
    [Fact]
    public void IsEnabled_ReturnsTrue_WhenStringValueExists()
    {
        using var runKey = new FakeRunKeySession { Values = { ["RigCommander"] = "\"C:\\Apps\\RigCommander.exe\"" } };
        var startupRegistration = CreateStartupRegistration(runKey);

        var isEnabled = startupRegistration.IsEnabled();

        isEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenValueMissingOrRunKeyUnavailable()
    {
        using var runKey = new FakeRunKeySession();
        var missingValue = CreateStartupRegistration(runKey);
        var missingRunKey = CreateStartupRegistration(runKey: null);

        missingValue.IsEnabled().Should().BeFalse();
        missingRunKey.IsEnabled().Should().BeFalse();
    }

    [Fact]
    public void SetEnabled_True_WritesQuotedExecutablePath()
    {
        using var runKey = new FakeRunKeySession();
        var startupRegistration = CreateStartupRegistration(runKey);

        startupRegistration.SetEnabled(enabled: true);

        runKey.Values["RigCommander"].Should().Be("\"C:\\Apps\\RigCommander.exe\"");
    }

    [Fact]
    public void SetEnabled_False_DeletesStartupValue()
    {
        using var runKey = new FakeRunKeySession { Values = { ["RigCommander"] = "any" } };
        var startupRegistration = CreateStartupRegistration(runKey);

        startupRegistration.SetEnabled(enabled: false);

        runKey.Values.ContainsKey("RigCommander").Should().BeFalse();
        runKey.DeleteCalls.Should().ContainSingle(call => call.Name == "RigCommander" && !call.ThrowOnMissingValue);
    }

    [Fact]
    public void SetEnabled_DoesNothing_WhenRunKeyUnavailable()
    {
        var startupRegistration = CreateStartupRegistration(runKey: null);

        var act = () => startupRegistration.SetEnabled(enabled: true);

        act.Should().NotThrow();
    }

    private static WindowsStartupRegistration CreateStartupRegistration(
        FakeRunKeySession? runKey,
        string executablePath = "C:\\Apps\\RigCommander.exe")
    {
        return new WindowsStartupRegistration(
            "RigCommander",
            new FakeRunKeyProvider(runKey),
            new FakeExecutablePathProvider(executablePath));
    }
}
