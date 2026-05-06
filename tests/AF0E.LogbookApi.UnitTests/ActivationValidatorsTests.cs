using FluentAssertions;
using Logbook.Api.Requests;
using Logbook.Api.Validators;

namespace AF0E.LogbookApi.UnitTests;

#pragma warning disable CA1707 // Allow underscores in test method names for readability

public class ActivationValidatorsTests
{
    [Fact]
    public void NewActivationValidator_ValidRequest_DoesNotThrow()
    {
        var req = new NewActivationRequest(
            PrevDayActivationId: 1,
            ParkNumber: "US-1234",
            Grid: "EN34",
            County: "Dakota",
            State: "MN",
            Lat: 44.98m,
            Lon: -93.26m,
            StationCallsign: "N0CAL",
            OperatorCallsign: "N0OP",
            StartDate: DateTime.UtcNow);

        var act = () => NewActivationValidator.ValidateAndThrow(req);

        act.Should().NotThrow();
    }

    [Fact]
    public void NewActivationValidator_InvalidParkNumber_ThrowsArgumentException()
    {
        var req = new NewActivationRequest(
            PrevDayActivationId: 1,
            ParkNumber: "1234",
            Grid: "EN34",
            County: "Dakota",
            State: "MN",
            Lat: 44.98m,
            Lon: -93.26m,
            StationCallsign: "N0CAL",
            OperatorCallsign: "N0OP",
            StartDate: DateTime.UtcNow);

        var act = () => NewActivationValidator.ValidateAndThrow(req);

        var ex = act.Should().Throw<ArgumentException>().Which;
        ex.ParamName.Should().Be("req");
        ex.Message.Should().Contain("Park number must be two letters, a dash, and 4-5 digits (e.g., US-1234)");
    }

    [Fact]
    public void UpdateActivationValidator_InvalidState_ThrowsArgumentException()
    {
        var req = new UpdateActivationRequest(
            Id: 12,
            ParkNum: "US-1234",
            Grid: "EN34",
            County: "Dakota",
            State: "MIN",
            Lat: 44.98m,
            Long: -93.26m,
            StationCallsign: "N0CAL",
            OperatorCallsign: "N0OP",
            StartDate: DateTime.UtcNow,
            EndDate: null,
            LogSubmittedDate: null,
            Status: "P",
            SiteComments: null);

        var act = () => UpdateActivationValidator.ValidateAndThrow(req);

        var ex = act.Should().Throw<ArgumentException>().Which;
        ex.ParamName.Should().Be("req");
        ex.Message.Should().Contain("State must be exactly 2 letters");
    }

    [Fact]
    public void UpdateActivationValidator_ValidRequest_DoesNotThrow()
    {
        var req = new UpdateActivationRequest(
            Id: 12,
            ParkNum: "US-1234",
            Grid: "EN34",
            County: "Dakota",
            State: "MN",
            Lat: 44.98m,
            Long: -93.26m,
            StationCallsign: "N0CAL",
            OperatorCallsign: "N0OP",
            StartDate: DateTime.UtcNow,
            EndDate: null,
            LogSubmittedDate: null,
            Status: "P",
            SiteComments: "test");

        var act = () => UpdateActivationValidator.ValidateAndThrow(req);

        act.Should().NotThrow();
    }

    [Fact]
    public void CloneActivationValidator_BlankParkNumber_ThrowsArgumentException()
    {
        var req = new CopyActivationRequest(ActivationId: 7, ParkNumber: " ");

        var act = () => CopyActivationValidator.ValidateAndThrow(req);

        var ex = act.Should().Throw<ArgumentException>().Which;
        ex.ParamName.Should().Be("req");
        ex.Message.Should().Contain("Park number is required");
    }

    [Fact]
    public void CloneActivationValidator_ValidRequest_DoesNotThrow()
    {
        var req = new CopyActivationRequest(ActivationId: 7, ParkNumber: "US-2345");

        var act = () => CopyActivationValidator.ValidateAndThrow(req);

        act.Should().NotThrow();
    }
}
