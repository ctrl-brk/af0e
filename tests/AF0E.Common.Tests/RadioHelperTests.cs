using AF0E.Common.Radio;
using FluentAssertions;
using Xunit;

namespace AF0E.Common.Tests;

public sealed class RadioHelperTests
{
    [Theory]
    [InlineData(14074.0, "20m")]
    [InlineData(7030.0, "40m")]
    [InlineData(18100.0, "17m")]
    [InlineData(432100.0, "70cm")]
    [InlineData(915000.0, "33cm")]
    [InlineData(1296000.0, "23cm")]
    [InlineData(1500.0, null)]
    public void DetectBandReturnsExpectedBand(decimal frequencyKhz, string? expectedBand)
    {
        var band = RadioHelper.DetectBand(frequencyKhz);

        band.Should().Be(expectedBand);
    }

    [Theory]
    [InlineData("20M", "20m")]
    [InlineData(" 40m ", "40m")]
    [InlineData("70CM", "70cm")]
    [InlineData("1.25m", "1.25m")]
    [InlineData("AIR", "AIR")]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    public void NormalizeBandReturnsExpectedValue(string? value, string? expectedBand)
    {
        var band = RadioHelper.NormalizeBand(value);

        band.Should().Be(expectedBand);
    }

    [Theory]
    [InlineData("USB", "SSB")]
    [InlineData("lsb", "SSB")]
    [InlineData("phone", "SSB")]
    [InlineData("digital", "DIGI")]
    [InlineData("data", "DIGI")]
    [InlineData("FT-8", "FT8")]
    [InlineData("JS8Call", "JS8")]
    [InlineData("MFSK16", "MFSK")]
    [InlineData("PSK31", "PSK")]
    [InlineData("FT8", "FT8")]
    [InlineData("cw", "CW")]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    public void NormalizeModeReturnsExpectedValue(string? value, string? expectedMode)
    {
        var mode = RadioHelper.NormalizeMode(value);

        mode.Should().Be(expectedMode);
    }

    [Theory]
    [InlineData("DIGI", "FT8", true)]
    [InlineData("FT8", "DIGI", true)]
    [InlineData("DIGI", "FT4", true)]
    [InlineData("DIGI", "FT2", true)]
    [InlineData("DIGI", "MFSK16", true)]
    [InlineData("DIGI", "CW", false)]
    [InlineData("FT8", "FT4", false)]
    [InlineData(null, "FT8", false)]
    public void ModesMatchReturnsExpectedValue(string? left, string? right, bool expectedMatch)
    {
        var matches = RadioHelper.ModesMatch(left, right);

        matches.Should().Be(expectedMatch);
    }
}


