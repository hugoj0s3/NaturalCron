using FluentAssertions;

namespace NaturalCron.UnitTests;

/// <summary>
/// Edge-case tests for spring-forward daylight saving transitions that require maxLookahead or invalid inputs.
/// Standard spring-forward cases are in GetNextOcurrencesTestCases.json (section 42).
/// </summary>
public class NaturalCronDaylightSavingTests
{
    private static DateTime Utc(int year, int month, int day, int hour, int minute, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    [Fact]
    public void SpringForward_MovedOccurrenceBeyondMaxLookahead_ReturnsNull()
    {
        // 02:30 moves to 03:00 EDT (07:00 UTC), which equals maxLookahead (exclusive) -> null.
        var expr = NaturalCronExpr.Parse("every day at 02:30 tz America/New_York");
        var baseTime = Utc(2025, 3, 9, 6, 0);     // 01:00 EST
        var maxLookahead = Utc(2025, 3, 9, 7, 0); // 03:00 EDT

        var next = expr.TryGetNextOccurrenceInUtc(baseTime, maxLookahead);

        next.Should().BeNull();
    }

    [Fact]
    public void SpringForward_InvalidBaseTimePassedInTz_DoesNotThrow()
    {
        var expr = NaturalCronExpr.Parse("every day at 09:00 tz Europe/Lisbon");
        var invalidBaseInNy = new DateTime(2025, 3, 9, 2, 30, 0, DateTimeKind.Unspecified);

        var next = expr.TryGetNextOccurrenceInTz(invalidBaseInNy, "America/New_York");

        // 02:30 invalid in NY moves to 03:00 EDT (07:00 UTC); next 09:00 WET in Lisbon is 09:00 UTC = 05:00 EDT.
        next.Should().Be(new DateTime(2025, 3, 9, 5, 0, 0, DateTimeKind.Unspecified));
    }
}
