using BusTracking.Contracts;

namespace BusTracking.Core.Tests;

public class LocationReportValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

    private static LocationReport ValidReport() =>
        new("bus-01", "route-01", 4.8133, -75.6961, 25, 90, Now);

    [Fact]
    public void Validate_ValidReport_HasNoErrors()
    {
        Assert.Empty(LocationReportValidator.Validate(ValidReport(), Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingBusId_Fails(string busId)
    {
        var errors = LocationReportValidator.Validate(ValidReport() with { BusId = busId }, Now);

        Assert.Equal("busId", Assert.Single(errors).Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingRouteId_Fails(string routeId)
    {
        var errors = LocationReportValidator.Validate(ValidReport() with { RouteId = routeId }, Now);

        Assert.Equal("routeId", Assert.Single(errors).Key);
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    [InlineData(double.NaN)]
    public void Validate_LatitudeOutOfRange_Fails(double latitude)
    {
        var errors = LocationReportValidator.Validate(ValidReport() with { Latitude = latitude }, Now);

        Assert.Equal("latitude", Assert.Single(errors).Key);
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    public void Validate_LongitudeOutOfRange_Fails(double longitude)
    {
        var errors = LocationReportValidator.Validate(ValidReport() with { Longitude = longitude }, Now);

        Assert.Equal("longitude", Assert.Single(errors).Key);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(150.1)]
    public void Validate_SpeedOutOfRange_Fails(double speedKmh)
    {
        var errors = LocationReportValidator.Validate(ValidReport() with { SpeedKmh = speedKmh }, Now);

        Assert.Equal("speedKmh", Assert.Single(errors).Key);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(360.1)]
    public void Validate_HeadingOutOfRange_Fails(double heading)
    {
        var errors = LocationReportValidator.Validate(ValidReport() with { Heading = heading }, Now);

        Assert.Equal("heading", Assert.Single(errors).Key);
    }

    [Fact]
    public void Validate_RecordedInTheFuture_Fails()
    {
        var report = ValidReport() with { RecordedAt = Now.AddMinutes(2) };

        Assert.Equal("recordedAt", Assert.Single(LocationReportValidator.Validate(report, Now)).Key);
    }

    [Fact]
    public void Validate_RecordedMoreThanOneHourAgo_Fails()
    {
        var report = ValidReport() with { RecordedAt = Now.AddHours(-2) };

        Assert.Equal("recordedAt", Assert.Single(LocationReportValidator.Validate(report, Now)).Key);
    }

    [Fact]
    public void Validate_SmallClockDifference_IsAccepted()
    {
        var report = ValidReport() with { RecordedAt = Now.AddSeconds(30) };

        Assert.Empty(LocationReportValidator.Validate(report, Now));
    }
}
