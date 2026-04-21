using FluentAssertions;
using RigCommander.Radios.Icom;
using RigCommander.Radios.Yaesu;
using Xunit;

namespace RigCommander.Tests;

#pragma warning disable CA1707 // Identifiers should not contain underscores

public sealed class RadioFactoryTests
{
    [Fact]
    public void Create_ReturnsIcomRadio()
    {
        var profile = new RadioProfileSettings
        {
            Name = "IC-9100",
            Kind = RadioProfileKind.Icom,
            Icom = new IcomSettings
            {
                PortName = "COM20",
                BaudRate = 115200,
                RadioAddress = 0x7C,
                ControllerAddress = 0xE0
            }
        };

        using var radio = RadioFactory.Create(profile);

        radio.Should().BeOfType<IC_9100>();
    }

    [Fact]
    public void Create_ReturnsYaesuRadio()
    {
        var profile = new RadioProfileSettings
        {
            Name = "field",
            Kind = RadioProfileKind.Yaesu,
            Yaesu = new YaesuSettings
            {
                PortName = "COM21",
                BaudRate = 38400
            }
        };

        using var radio = RadioFactory.Create(profile);

        radio.Should().BeOfType<FT_897>();
    }
}
