using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace RigCommander.Tests;

#pragma warning disable CA1707 // Identifiers should not contain underscores

public sealed class RigCommanderSettingsValidatorTests
{
    private readonly RigCommanderSettingsValidator _validator = new();

    [Fact]
    public void Validate_Fails_WhenProfilesMissing()
    {
        var settings = new RigCommanderSettings { Profiles = [] };

        var result = _validator.Validate(Options.DefaultName, settings);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Profiles"));
    }

    [Fact]
    public void Validate_Fails_WhenDuplicateNamesDetected()
    {
        var settings = new RigCommanderSettings
        {
            Profiles =
            [
                new RadioProfileSettings { Name = "lab", Kind = RadioProfileKind.Icom, Icom = new IcomSettings { PortName = "COM1", BaudRate = 2400 } },
                new RadioProfileSettings { Name = "lab", Kind = RadioProfileKind.Yaesu, Yaesu = new YaesuSettings { PortName = "COM2", BaudRate = 4800 } }
            ]
        };

        var result = _validator.Validate(Options.DefaultName, settings);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Fails_WhenNegativeDelay()
    {
        var settings = new RigCommanderSettings
        {
            StatusDelayMs = -1,
            Profiles =
            [
                new RadioProfileSettings { Name = "lab", Kind = RadioProfileKind.Icom, Icom = new IcomSettings { PortName = "COM1", BaudRate = 2400 } },
                new RadioProfileSettings { Name = "lab", Kind = RadioProfileKind.Yaesu, Yaesu = new YaesuSettings { PortName = "COM2", BaudRate = 4800 } }
            ]
        };

        var result = _validator.Validate(Options.DefaultName, settings);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("StatusDelayMs must be zero or greater.", StringComparison.OrdinalIgnoreCase));
    }
    
    [Fact]
    public void Validate_Succeeds_ForWellFormedProfiles()
    {
        var settings = new RigCommanderSettings
        {
            ListenPort = "5050",
            StatusDelayMs = 1000,
            ActiveProfile = "lab",
            Profiles =
            [
                new RadioProfileSettings { Name = "lab", Kind = RadioProfileKind.Icom, Icom = new IcomSettings { PortName = "COM7", BaudRate = 19200, RadioAddress = 0x7C, ControllerAddress = 0xE0 } },
                new RadioProfileSettings { Name = "field", Kind = RadioProfileKind.Yaesu, Yaesu = new YaesuSettings { PortName = "COM8", BaudRate = 38400 } }
            ]
        };

        var result = _validator.Validate(Options.DefaultName, settings);

        result.Succeeded.Should().BeTrue();
    }
}
