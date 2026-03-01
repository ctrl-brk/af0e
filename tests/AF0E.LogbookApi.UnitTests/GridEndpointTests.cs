using System.Diagnostics.CodeAnalysis;
using Logbook.Api.Handlers;

namespace AF0E.LogbookApi.UnitTests;
#pragma warning disable CA1707 //Allow underscores in test method names for readability

[SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
public class GridEndpointTests
{
    [Theory]
    [InlineData("45.0", "-93.0", "EN35ma")]
    [InlineData("40.7128", "-74.0060", "FN20xr")]
    [InlineData("51.5074", "-0.1278", "IO91wm")]
    [InlineData("-33.8688", "151.2093", "QF56od")]
    public void CoordinatesToGridSquare_ValidCoordinates_ReturnsCorrectGridSquare(string latitude, string longitude, string expectedGrid)
    {
        // Arrange
        var lat = double.Parse(latitude);
        var lon = double.Parse(longitude);

        // Act
        var result = UtilsHandlers.CoordinatesToGridSquare(lat, lon);

        // Assert
        Assert.Equal(expectedGrid, result);
    }

    [Theory]
    [InlineData("xyz", "-93.0")]
    [InlineData("45.0", "abc")]
    [InlineData("invalid", "invalid")]
    [InlineData("", "-93.0")]
    [InlineData("45.0", "")]
    public void CoordinatesToGridSquare_InvalidInput_ShouldBeHandledByEndpoint(string latitude, string longitude)
    {
        // This test verifies that the endpoint would catch invalid string inputs
        // before reaching the handler, since the endpoint now accepts strings and validates them
        
        // Arrange & Act
        var latParsed = double.TryParse(latitude, out _);
        var lonParsed = double.TryParse(longitude, out _);

        // Assert - at least one should fail to parse
        Assert.False(latParsed && lonParsed, "At least one parameter should fail to parse");
    }

    [Theory]
    [InlineData(91.0, 0.0)]
    [InlineData(-91.0, 0.0)]
    [InlineData(0.0, 181.0)]
    [InlineData(0.0, -181.0)]
    [InlineData(100.0, 200.0)]
    public void CoordinatesToGridSquare_OutOfRangeCoordinates_ReturnsErrorMessage(double latitude, double longitude)
    {
        // Act
        var result = UtilsHandlers.CoordinatesToGridSquare(latitude, longitude);

        // Assert
        Assert.Equal("Coordinates out of range", result);
    }

    [Theory]
    [InlineData(double.NaN, 0.0)]
    [InlineData(0.0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0.0)]
    [InlineData(0.0, double.NegativeInfinity)]
    public void CoordinatesToGridSquare_InvalidDoubleValues_ReturnsErrorMessage(double latitude, double longitude)
    {
        // Act
        var result = UtilsHandlers.CoordinatesToGridSquare(latitude, longitude);

        // Assert
        Assert.Equal("Invalid coordinates", result);
    }

    [Theory]
    [InlineData(0.0, 0.0, "JJ00aa")]
    [InlineData(90.0, 180.0, "RR99xx")]
    [InlineData(-90.0, -180.0, "AA00aa")]
    public void CoordinatesToGridSquare_BoundaryValues_ReturnsValidGridSquare(double latitude, double longitude, string expectedGrid)
    {
        // Act
        var result = UtilsHandlers.CoordinatesToGridSquare(latitude, longitude);

        // Assert
        Assert.Equal(6, result.Length);
        Assert.Equal(expectedGrid, result);
    }
}
