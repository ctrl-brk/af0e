using System.Text.Json;
using AF0E.DB.Models;
using Logbook.Api.Models;

namespace AF0E.LogbookApi.UnitTests;

public sealed class HamEventSummaryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ConstructorMapsSummaryFields()
    {
        var hamEvent = CreateHamEvent();

        var result = new HamEventSummary(hamEvent);

        Assert.Equal(42, result.Id);
        Assert.Equal("E", result.EventType);
        Assert.Equal("Minnesota QSO Party", result.Name);
        Assert.Equal(new DateTime(2026, 2, 7, 14, 0, 0, DateTimeKind.Utc), result.StartDate);
        Assert.Equal(new DateTime(2026, 2, 8, 2, 0, 0, DateTimeKind.Utc), result.EndDate);
    }

    [Fact]
    public void SerializationExposesOnlyResponseFields()
    {
        var result = new HamEventSummary(CreateHamEvent());

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result, JsonOptions));
        var json = document.RootElement;

        Assert.Equal(42, json.GetProperty("id").GetInt32());
        Assert.Equal("E", json.GetProperty("eventType").GetString());
        Assert.Equal("Minnesota QSO Party", json.GetProperty("name").GetString());
        Assert.True(json.TryGetProperty("startDate", out _));
        Assert.True(json.TryGetProperty("endDate", out _));
        Assert.False(json.TryGetProperty("description", out _));
        Assert.False(json.TryGetProperty("url", out _));
        Assert.False(json.TryGetProperty("contactCount", out _));
        Assert.False(json.TryGetProperty("hamEvent", out _));
        Assert.False(json.TryGetProperty("hamEventContacts", out _));
        Assert.Equal(5, json.EnumerateObject().Count());
    }

    [Fact]
    public void QsoDetailsMapsSelectedHamEventIds()
    {
        var log = new HrdLog
        {
            ColPrimaryKey = 7,
            ColCall = "AF0E",
            ColTimeOn = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc),
            ColBand = "20m",
            ColMode = "CW",
            HamEventContacts =
            [
                new HamEventContact { HamEventId = 42 },
                new HamEventContact { HamEventId = 3 }
            ]
        };

        var result = new QsoDetails(log, isAdmin: true);

        Assert.Equal([3, 42], result.HamEventIds);
    }

    [Fact]
    public void QsoDetailsDeserializationAcceptsSelectedHamEventIds()
    {
        const string json = """{"hamEventIds":[3,42]}""";

        var result = JsonSerializer.Deserialize<QsoDetails>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.Equal([3, 42], result.HamEventIds);
    }

    private static HamEvent CreateHamEvent() => new()
    {
        HamEventId = 42,
        EventType = "E",
        Name = "Minnesota QSO Party",
        Description = "Annual state QSO party",
        Url = new Uri("https://example.com/mnqp"),
        StartDate = new DateTime(2026, 2, 7, 14, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2026, 2, 8, 2, 0, 0, DateTimeKind.Utc),
        LogDueDate = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
        QslDueDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        QslInfo = "QSL via bureau",
        Comments = "Test event"
    };
}

