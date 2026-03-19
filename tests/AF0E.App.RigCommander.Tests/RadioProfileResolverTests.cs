using FluentAssertions;
using Xunit;

namespace RigCommander.Tests;

#pragma warning disable CA1707 // Identifiers should not contain underscores

public sealed class RadioProfileResolverTests
{
    [Fact]
    public void Resolve_UsesActiveProfileWhenSet()
    {
        var settings = BuildSettings();
        settings.ActiveProfile = "field";

        var profile = RadioProfileResolver.Resolve(settings);

        profile.Name.Should().Be("field");
    }

    [Fact]
    public void Resolve_DefaultsToFirstProfile_WhenActiveProfileMissing()
    {
        var settings = BuildSettings();
        settings.ActiveProfile = null;

        var profile = RadioProfileResolver.Resolve(settings);

        profile.Name.Should().Be("lab");
    }

    [Fact]
    public void Resolve_DefaultsToFirstProfile_WhenActiveProfileNotFound()
    {
        var settings = BuildSettings();
        settings.ActiveProfile = "unknown";

        var profile = RadioProfileResolver.Resolve(settings);

        profile.Name.Should().Be("lab");
    }

    [Fact]
    public void Resolve_Throws_WhenNoProfilesConfigured()
    {
        var settings = new RigCommanderSettings();

        var act = () => RadioProfileResolver.Resolve(settings);

        act.Should().Throw<InvalidOperationException>();
    }

    private static RigCommanderSettings BuildSettings() => new()
    {
        Profiles =
        [
            new RadioProfileSettings { Name = "lab", Kind = RadioProfileKind.Icom, Icom = new IcomSettings { PortName = "COM1", BaudRate = 2400 }},
            new RadioProfileSettings { Name = "field", Kind = RadioProfileKind.Yaesu, Yaesu = new YaesuSettings { PortName = "COM2", BaudRate = 4800 }}
        ]
    };
}
