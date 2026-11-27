using FluentAssertions;
using TimeZoneConverter;

namespace NaturalCron.UnitTests;

/// <summary>
/// How the UTC and local methods read the Kind of the base time and which Kind they return,
/// the same with or without a tz rule.
/// </summary>
public class OccurrenceKindTests
{
    private static readonly DateTime BaseTimeUtc = new(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static IEnumerable<object[]> Expressions() =>
    [
        ["every day at 10:00"],
        ["every day at 10:00 tz America/New_York"]
    ];

    [Theory]
    [MemberData(nameof(Expressions))]
    public void InUtc_WithUtcBaseTime_ReturnsUtc(string expression)
    {
        var result = NaturalCronExpr.Parse(expression).TryGetNextOccurrenceInUtc(BaseTimeUtc);

        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [MemberData(nameof(Expressions))]
    public void InUtc_WithUnspecifiedBaseTime_IsTreatedAsUtc(string expression)
    {
        var naturalCronExpr = NaturalCronExpr.Parse(expression);
        var unspecified = DateTime.SpecifyKind(BaseTimeUtc, DateTimeKind.Unspecified);

        var result = naturalCronExpr.TryGetNextOccurrenceInUtc(unspecified);

        result.Should().Be(naturalCronExpr.TryGetNextOccurrenceInUtc(BaseTimeUtc));
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [MemberData(nameof(Expressions))]
    public void InUtc_WithLocalBaseTime_Throws(string expression)
    {
        var naturalCronExpr = NaturalCronExpr.Parse(expression);
        var local = BaseTimeUtc.ToLocalTime();

        naturalCronExpr.Invoking(x => x.TryGetNextOccurrenceInUtc(local))
            .Should().Throw<ArgumentException>().WithParameterName("utcBaseTime");
        naturalCronExpr.Invoking(x => x.TryGetNextOccurrenceInUtc(BaseTimeUtc, local.AddDays(10)))
            .Should().Throw<ArgumentException>().WithParameterName("utcMaxLookahead");
    }

    [Theory]
    [MemberData(nameof(Expressions))]
    public void Local_WithUtcBaseTime_ReturnsUtcLikeTheUtcMethod(string expression)
    {
        var naturalCronExpr = NaturalCronExpr.Parse(expression);

        var result = naturalCronExpr.TryGetNextOccurrence(BaseTimeUtc);

        result.Should().Be(naturalCronExpr.TryGetNextOccurrenceInUtc(BaseTimeUtc));
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [MemberData(nameof(Expressions))]
    public void Local_WithLocalBaseTime_ReturnsLocal(string expression)
    {
        var naturalCronExpr = NaturalCronExpr.Parse(expression);

        var result = naturalCronExpr.TryGetNextOccurrence(BaseTimeUtc.ToLocalTime());

        result!.Value.Kind.Should().Be(DateTimeKind.Local);
        result.Value.ToUniversalTime().Should().Be(ExpectedUtc(naturalCronExpr));
    }

    [Theory]
    [MemberData(nameof(Expressions))]
    public void Local_WithUnspecifiedBaseTime_ReturnsUnspecified(string expression)
    {
        var naturalCronExpr = NaturalCronExpr.Parse(expression);
        var unspecified = DateTime.SpecifyKind(BaseTimeUtc.ToLocalTime(), DateTimeKind.Unspecified);

        var result = naturalCronExpr.TryGetNextOccurrence(unspecified);

        result!.Value.Kind.Should().Be(DateTimeKind.Unspecified);
        DateTime.SpecifyKind(result.Value, DateTimeKind.Local).ToUniversalTime().Should().Be(ExpectedUtc(naturalCronExpr));
    }

    [Theory]
    [MemberData(nameof(Expressions))]
    public void InTz_WithUnspecifiedBaseTime_ReturnsUnspecified(string expression)
    {
        var naturalCronExpr = NaturalCronExpr.Parse(expression);
        var unspecified = DateTime.SpecifyKind(BaseTimeUtc, DateTimeKind.Unspecified);

        var result = naturalCronExpr.TryGetNextOccurrenceInTz(unspecified, "Asia/Tokyo");

        result!.Value.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Theory]
    [MemberData(nameof(Expressions))]
    public void InTz_WithUtcBaseTimeAndUtcZone_ReturnsUtcLikeTheUtcMethod(string expression)
    {
        var naturalCronExpr = NaturalCronExpr.Parse(expression);

        foreach (var utcZoneId in new[] { "UTC", "Etc/UTC" })
        {
            var result = naturalCronExpr.TryGetNextOccurrenceInTz(BaseTimeUtc, utcZoneId);

            result.Should().Be(naturalCronExpr.TryGetNextOccurrenceInUtc(BaseTimeUtc), utcZoneId);
            result!.Value.Kind.Should().Be(DateTimeKind.Utc, utcZoneId);
        }
    }

    [Theory]
    [MemberData(nameof(Expressions))]
    public void InTz_WithUtcBaseTimeAndAnotherZone_Throws(string expression)
    {
        var naturalCronExpr = NaturalCronExpr.Parse(expression);
        var unspecified = DateTime.SpecifyKind(BaseTimeUtc, DateTimeKind.Unspecified);

        naturalCronExpr.Invoking(x => x.TryGetNextOccurrenceInTz(BaseTimeUtc, "America/New_York"))
            .Should().Throw<ArgumentException>().WithParameterName("baseTimeInTz");
        naturalCronExpr.Invoking(x => x.TryGetNextOccurrenceInTz(unspecified, "America/New_York", BaseTimeUtc.AddDays(10)))
            .Should().Throw<ArgumentException>().WithParameterName("maxLookaheadInTz");
    }

    [Theory]
    [MemberData(nameof(Expressions))]
    public void InTz_WithLocalBaseTimeAndMachineZone_ReturnsLocalLikeTheLocalMethod(string expression)
    {
        var naturalCronExpr = NaturalCronExpr.Parse(expression);
        var local = BaseTimeUtc.ToLocalTime();

        var result = naturalCronExpr.TryGetNextOccurrenceInTz(local, MachineIanaZoneId());

        result.Should().Be(naturalCronExpr.TryGetNextOccurrence(local));
        result!.Value.Kind.Should().Be(DateTimeKind.Local);
    }

    [Theory]
    [MemberData(nameof(Expressions))]
    public void InTz_WithLocalBaseTimeAndAnotherZone_Throws(string expression)
    {
        var naturalCronExpr = NaturalCronExpr.Parse(expression);
        var otherZoneId = TimeZoneInfo.Local.HasSameRules(TZConvert.GetTimeZoneInfo("Asia/Tokyo")) ? "Europe/Lisbon" : "Asia/Tokyo";

        naturalCronExpr.Invoking(x => x.TryGetNextOccurrenceInTz(BaseTimeUtc.ToLocalTime(), otherZoneId))
            .Should().Throw<ArgumentException>().WithParameterName("baseTimeInTz");
    }

    [Theory]
    [MemberData(nameof(Expressions))]
    public void PluralMethods_WithZeroCountAndInvalidInput_StillThrow(string expression)
    {
        var naturalCronExpr = NaturalCronExpr.Parse(expression);
        var local = BaseTimeUtc.ToLocalTime();

        naturalCronExpr.Invoking(x => x.TryGetNextOccurrencesInUtc(local, 0))
            .Should().Throw<ArgumentException>().WithParameterName("utcBaseTime");
        naturalCronExpr.Invoking(x => x.TryGetNextOccurrencesInUtc(BaseTimeUtc, 0, local.AddDays(10)))
            .Should().Throw<ArgumentException>().WithParameterName("utcMaxLookahead");

        naturalCronExpr.Invoking(x => x.TryGetNextOccurrencesInTz(BaseTimeUtc, 0, "America/New_York"))
            .Should().Throw<ArgumentException>().WithParameterName("baseTimeInTz");
        naturalCronExpr.Invoking(x => x.TryGetNextOccurrencesInTz(DateTime.SpecifyKind(BaseTimeUtc, DateTimeKind.Unspecified), 0, "Invalid/Zone"))
            .Should().Throw<ArgumentException>();
    }

    private static string MachineIanaZoneId() =>
        TZConvert.TryWindowsToIana(TimeZoneInfo.Local.Id, out var ianaId) ? ianaId : TimeZoneInfo.Local.Id;

    // With a tz rule: the same instant as the UTC method.
    // Without: the first 10:00 on the machine's clock after the base time.
    private static DateTime ExpectedUtc(NaturalCronExpr naturalCronExpr)
    {
        if (naturalCronExpr.TimeZoneRule() != null)
        {
            return naturalCronExpr.TryGetNextOccurrenceInUtc(BaseTimeUtc)!.Value;
        }

        var baseTimeLocal = BaseTimeUtc.ToLocalTime();
        var tenOClock = baseTimeLocal.Date.AddHours(10);
        var nextTenOClock = tenOClock > baseTimeLocal ? tenOClock : tenOClock.AddDays(1);
        return DateTime.SpecifyKind(nextTenOClock, DateTimeKind.Local).ToUniversalTime();
    }
}
