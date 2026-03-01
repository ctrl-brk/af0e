using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AF0E.LogbookApi.UnitTests;

#pragma warning disable CA1707 // Allow underscores in test method names for readability

[SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings")]
public class GridEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("45.0", "-93.0")]
    [InlineData("40.7128", "-74.0060")]
    [InlineData("51.5074", "-0.1278")]
    [InlineData("-33.8688", "151.2093")]
    public async Task GridEndpoint_ValidCoordinates_ReturnsOkWithGridSquare(string latitude, string longitude)
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/tools/grid?latitude={latitude}&longitude={longitude}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        var gridSquare = JsonSerializer.Deserialize<string>(content);

        Assert.NotNull(gridSquare);
        Assert.Equal(6, gridSquare!.Length);
        Assert.Matches(@"^[A-R]{2}[0-9]{2}[a-x]{2}$", gridSquare); // Valid grid square format
    }

    [Theory]
    [InlineData("xyz", "-93.0", "Invalid latitude value: xyz")]
    [InlineData("45.0", "abc", "Invalid longitude value: abc")]
    [InlineData("invalid", "invalid", "Invalid latitude value: invalid")]
    public async Task GridEndpoint_InvalidNumericInput_ReturnsBadRequestWithMessage(string latitude, string longitude, string expectedError)
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/tools/grid?latitude={latitude}&longitude={longitude}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorMessage = JsonSerializer.Deserialize<string>(content);
        
        Assert.Equal(expectedError, errorMessage);
    }

    [Theory]
    [InlineData("", "-93.0")]
    [InlineData("45.0", "")]
    [InlineData("", "")]
    public async Task GridEndpoint_EmptyParameters_ReturnsBadRequest(string latitude, string longitude)
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/tools/grid?latitude={latitude}&longitude={longitude}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorMessage = JsonSerializer.Deserialize<string>(content);
        
        Assert.Contains("required", errorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GridEndpoint_MissingLatitude_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/tools/grid?longitude=-93.0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorMessage = JsonSerializer.Deserialize<string>(content);
        
        Assert.NotNull(errorMessage);
        Assert.Equal("Both latitude and longitude parameters are required", errorMessage);
    }

    [Fact]
    public async Task GridEndpoint_MissingLongitude_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/tools/grid?latitude=45.0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorMessage = JsonSerializer.Deserialize<string>(content);
        
        Assert.NotNull(errorMessage);
        Assert.Equal("Both latitude and longitude parameters are required", errorMessage);
    }

    [Fact]
    public async Task GridEndpoint_MissingBothParameters_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/tools/grid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorMessage = JsonSerializer.Deserialize<string>(content);
        
        Assert.NotNull(errorMessage);
        Assert.Equal("Both latitude and longitude parameters are required", errorMessage);
    }

    [Theory]
    [InlineData("91.0", "0.0", "Coordinates out of range")]
    [InlineData("-91.0", "0.0", "Coordinates out of range")]
    [InlineData("0.0", "181.0", "Coordinates out of range")]
    [InlineData("0.0", "-181.0", "Coordinates out of range")]
    [InlineData("100.0", "200.0", "Coordinates out of range")]
    public async Task GridEndpoint_OutOfRangeCoordinates_ReturnsBadRequestWithMessage(string latitude, string longitude, string expectedError)
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/tools/grid?latitude={latitude}&longitude={longitude}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorMessage = JsonSerializer.Deserialize<string>(content);

        Assert.Equal(expectedError, errorMessage);
    }

    [Theory]
    [InlineData("0.0", "0.0")]
    [InlineData("90.0", "180.0")]
    [InlineData("-90.0", "-180.0")]
    public async Task GridEndpoint_BoundaryCoordinates_ReturnsOkWithValidGridSquare(string latitude, string longitude)
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/tools/grid?latitude={latitude}&longitude={longitude}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        var gridSquare = JsonSerializer.Deserialize<string>(content);
        
        Assert.NotNull(gridSquare);
        Assert.Equal(6, gridSquare!.Length);
    }
}

