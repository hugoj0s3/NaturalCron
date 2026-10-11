using FluentAssertions;
using NaturalCron.Rules;

namespace NaturalCron.UnitTests;

/// <summary>
/// "every &lt;value&gt;" is a shorthand for "every &lt;period&gt; on &lt;value&gt;".
/// </summary>
public class NaturalCronEveryValueTests
{
    private static readonly DateTime[] BaseTimes =
    {
        new(2024, 12, 20, 0, 0, 0, DateTimeKind.Utc),
        new(2025, 2, 27, 13, 37, 11, DateTimeKind.Utc),
        new(2025, 10, 31, 23, 59, 59, DateTimeKind.Utc),
    };

    [Theory]
    // Times => every day
    [InlineData("every 9am", "every day at 9am")]
    [InlineData("every 09:30", "every day at 09:30")]
    [InlineData("every 6:15pm", "every day at 6:15pm")]
    [InlineData("every [09:00, 18:00]", "every day at [09:00, 18:00]")]
    // Days of the month => every month
    [InlineData("every 15th", "every month on 15th")]
    [InlineData("every 15th at 18:00", "every month on 15th at 18:00")]
    [InlineData("every 1st at 09:00", "every month on 1st at 09:00")]
    [InlineData("every [1st, 15th] at 18:00", "every month on [1st, 15th] at 18:00")]
    [InlineData("every 1stMonday at 9am", "every month on 1stMonday at 9am")]
    [InlineData("every LastFriday at 5pm", "every month on LastFriday at 5pm")]
    [InlineData("every LastDay at 23:59", "every month on LastDay at 23:59")]
    [InlineData("every last day at 23:59", "every month on last day at 23:59")]
    [InlineData("every FirstWeekday at 08:00", "every month on FirstWeekday at 08:00")]
    [InlineData("every ClosestWeekdayTo 15 at 09:00", "every month on ClosestWeekdayTo 15 at 09:00")]
    // Weekdays => every week
    [InlineData("every monday", "every week on monday")]
    [InlineData("every monday at 08:00", "every week on monday at 08:00")]
    [InlineData("every friday at 17:00 in [jun, jul]", "every week on friday at 17:00 in [jun, jul]")]
    [InlineData("every [monday, friday] at 09:00", "every week on [monday, friday] at 09:00")]
    [InlineData("every monday 9am", "every week on monday 9am")]
    // Months => every year
    [InlineData("every january at 09:00", "every year on january at 09:00")]
    [InlineData("every [jan, jul] on 1st at 09:00", "every year on [jan, jul] on 1st at 09:00")]
    [InlineData("every 1st jan at 00:00", "every year on 1st jan at 00:00")]
    [InlineData("every jan-15 at 12:00", "every year on jan-15 at 12:00")]
    // With the other rules
    [InlineData("@every 15th @at 18:00 @tz America/New_York", "@every month @on 15th @at 18:00 @tz America/New_York")]
    [InlineData("every 9am between monday and friday", "every day at 9am between monday and friday")]
    public void EveryValue_IsTheSameAsEveryPeriodOnValue(string shorthand, string explicitExpression)
    {
        var shorthandExpr = NaturalCronExpr.Parse(shorthand);
        var explicitExpr = NaturalCronExpr.Parse(explicitExpression);

        foreach (var baseTime in BaseTimes)
        {
            var expected = explicitExpr.TryGetNextOccurrencesInUtc(baseTime, 6);

            expected.Should().HaveCount(6);
            shorthandExpr.TryGetNextOccurrencesInUtc(baseTime, 6).Should().Equal(expected);
        }
    }

    [Fact]
    public void EveryOrdinalDay_IsADayOfTheMonth_NotAnIntervalInDays()
    {
        // "every 15th" used to be parsed as "every 15 days".
        var occurrences = NaturalCronExpr.Parse("every 15th at 18:00")
            .GetNextOccurrencesInUtc(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), 3);

        occurrences.Should().Equal(
            new DateTime(2025, 1, 15, 18, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 2, 15, 18, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 15, 18, 0, 0, DateTimeKind.Utc));
    }

    // 2025-04-13 is a Sunday, 2025-04-14 a Monday.
    [Theory]
    // "every monday at 08:00" used to return the next day at 08:00 when the base time was later than 08:00.
    [InlineData("every monday at 08:00", "2025-04-13 09:00:00", "2025-04-14 08:00:00", "2025-04-21 08:00:00")]
    [InlineData("every monday at 08:00", "2025-04-14 09:00:00", "2025-04-21 08:00:00", "2025-04-28 08:00:00")]
    [InlineData("every monday at 08:00", "2025-04-14 07:00:00", "2025-04-14 08:00:00", "2025-04-21 08:00:00")]
    [InlineData("every monday at 08:00", "2025-04-14 08:00:00", "2025-04-21 08:00:00", "2025-04-28 08:00:00")]
    [InlineData("every monday at 08:00", "2025-04-15 23:59:00", "2025-04-21 08:00:00", "2025-04-28 08:00:00")]
    // It used to return a day that is not a monday when the other rules skipped some weeks.
    [InlineData("every monday at 10:00 in [jan, feb, mar]", "2025-03-25 00:00:00", "2025-03-31 10:00:00", "2026-01-05 10:00:00")]
    public void EveryWeekday_OnlyReturnsThatWeekday(string expression, string baseTime, string first, string second)
    {
        var occurrences = NaturalCronExpr.Parse(expression)
            .GetNextOccurrencesInUtc(DateTime.SpecifyKind(DateTime.Parse(baseTime), DateTimeKind.Utc), 2);

        occurrences.Should().Equal(
            DateTime.SpecifyKind(DateTime.Parse(first), DateTimeKind.Utc),
            DateTime.SpecifyKind(DateTime.Parse(second), DateTimeKind.Utc));
    }

    [Fact]
    public void EveryNthWeekday_WhenTheDayOfTheMonthGoesDown()
    {
        var occurrences = NaturalCronExpr.Parse("every 1stMonday at 09:00")
            .GetNextOccurrencesInUtc(new DateTime(2022, 10, 1, 0, 0, 0, DateTimeKind.Utc), 4);

        occurrences.Should().Equal(
            new DateTime(2022, 10, 3, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2022, 11, 7, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2022, 12, 5, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2023, 1, 2, 9, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData("every 9am", NaturalCronTimeUnit.Day)]
    [InlineData("every 15th", NaturalCronTimeUnit.Month)]
    [InlineData("every monday", NaturalCronTimeUnit.Week)]
    [InlineData("every [monday, friday]", NaturalCronTimeUnit.Week)]
    [InlineData("every january", NaturalCronTimeUnit.Year)]
    public void EveryValue_ProducesAnEveryRuleAndTheValueRules(string expression, NaturalCronTimeUnit expectedEveryTimeUnit)
    {
        var expr = NaturalCronExpr.Parse(expression);

        var everyRule = expr.Rules.OfType<NaturalCronEveryXRule>().Should().ContainSingle().Subject;
        everyRule.TimeUnit.Should().Be(expectedEveryTimeUnit);
        everyRule.Value.Should().Be(1);
        expr.Rules.Should().HaveCountGreaterThan(1);
    }

    [Theory]
    // Intervals and single weekdays keep their meaning.
    [InlineData("every day", NaturalCronTimeUnit.Day, 1, null)]
    [InlineData("every 2 days", NaturalCronTimeUnit.Day, 2, null)]
    [InlineData("every 25 min", NaturalCronTimeUnit.Minute, 25, null)]
    [InlineData("every 15 seconds", NaturalCronTimeUnit.Second, 15, null)]
    [InlineData("every week AnchoredOn Friday", NaturalCronTimeUnit.Week, 1, "FRIDAY")]
    [InlineData("every 2 months AnchoredOn Nov", NaturalCronTimeUnit.Month, 2, "NOV")]
    public void Intervals_AreUnchanged(string expression, NaturalCronTimeUnit timeUnit, int value, string? anchoredValue)
    {
        var expr = NaturalCronExpr.Parse(expression);

        var everyRule = expr.Rules.Should().ContainSingle().Which.Should().BeOfType<NaturalCronEveryXRule>().Subject;
        everyRule.TimeUnit.Should().Be(timeUnit);
        everyRule.Value.Should().Be(value);
        (everyRule.AnchoredValue?.ToUpper()).Should().Be(anchoredValue);
    }

    [Theory]
    [InlineData("every 2025-12-25", "A date with a year occurs only once")]
    [InlineData("every 25th dec 2025 at 09:00", "A date with a year occurs only once")]
    [InlineData("every 15", "invalid time unit")]
    [InlineData("every foo", "Invalid token")]
    [InlineData("every 15th on 20th", "Duplicate At/On/In rules")]
    public void InvalidEveryValue_ReturnsError(string expression, string expectedError)
    {
        var (expr, errors) = NaturalCronExpr.TryParse(expression);

        expr.Should().BeNull();
        errors.Should().Contain(x => x.Contains(expectedError));
    }
}
