using FluentAssertions;
using NaturalCron.Rules;

namespace NaturalCron.UnitTests;

public class NaturalCronExceptRulesTests
{
    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    private static IList<DateTime> Next(string expression, DateTime baseTime, int count) =>
        NaturalCronExpr.Parse(expression).GetNextOccurrencesInUtc(baseTime, count);

    // 2025-01-02 is a Thursday.

    [Theory]
    [InlineData("every day at 09:00 except [saturday, sunday]")]
    [InlineData("@every day @at 09:00 @except [saturday, sunday]")]
    public void Except_Weekdays_SkipsThoseDays(string expression)
    {
        Next(expression, Utc(2025, 1, 2, 10), 3)
            .Should().Equal(Utc(2025, 1, 3, 9), Utc(2025, 1, 6, 9), Utc(2025, 1, 7, 9));
    }

    [Theory]
    [InlineData("every day at 09:00 except 2025-01-03")]
    [InlineData("every day at 09:00 except 3rd")]
    [InlineData("every day at 09:00 except 3rd jan")]
    public void Except_Date_SkipsThatDay(string expression)
    {
        Next(expression, Utc(2025, 1, 2, 10), 2)
            .Should().Equal(Utc(2025, 1, 4, 9), Utc(2025, 1, 5, 9));
    }

    [Fact]
    public void Except_Months_SkipsThoseMonths()
    {
        Next("every month on 1st at 09:00 except [feb, mar]", Utc(2025, 1, 2), 2)
            .Should().Equal(Utc(2025, 4, 1, 9), Utc(2025, 5, 1, 9));
    }

    [Fact]
    public void Except_DayRule_AllowedWithEveryDay()
    {
        // An "on" rule at the every unit is rejected, but an exclusion at that unit is fine.
        NaturalCronExpr.TryParse("every day at 09:00 except 3rd").errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("every hour except 22:00")]
    [InlineData("every hour except [22:00, 23:00]")]
    public void Except_Time_KeepsTheEveryCadence(string expression)
    {
        var occurrences = Next(expression, Utc(2025, 1, 2, 21), 3);

        occurrences.Should().OnlyContain(x => x.Minute == 0 && x.Second == 0);
        occurrences.Should().NotContain(x => x.Hour == 22);
        occurrences.First().Should().BeAfter(Utc(2025, 1, 2, 21));
    }

    [Fact]
    public void Except_TimeList_ContinuesAtNextSlot()
    {
        Next("every hour except [22:00, 23:00]", Utc(2025, 1, 2, 21), 2)
            .Should().Equal(Utc(2025, 1, 3, 0), Utc(2025, 1, 3, 1));
    }

    [Fact]
    public void Except_Weekday_WithEveryXHours_KeepsCadence()
    {
        Next("every 2 hours except friday", Utc(2025, 1, 2, 23), 2)
            .Should().Equal(Utc(2025, 1, 4, 1), Utc(2025, 1, 4, 3));
    }

    [Fact]
    public void Except_Hour_WithEveryXMinutes()
    {
        Next("every 15 min except 22hr", Utc(2025, 1, 2, 21, 30), 3)
            .Should().Equal(Utc(2025, 1, 2, 21, 45), Utc(2025, 1, 2, 23), Utc(2025, 1, 2, 23, 15));
    }

    [Fact]
    public void Except_CombinedWithOnRule()
    {
        // Mondays at 09:00, except 2025-01-13.
        Next("every week on monday at 09:00 except 2025-01-13", Utc(2025, 1, 7), 2)
            .Should().Equal(Utc(2025, 1, 20, 9), Utc(2025, 1, 27, 9));
    }

    [Fact]
    public void Except_WithTimeZone_EvaluatedInExpressionZone()
    {
        // 09:00 Tokyo = 00:00 UTC same day; 2025-01-04 is a Saturday in Tokyo.
        Next("every day at 09:00 except [saturday, sunday] tz Asia/Tokyo", Utc(2025, 1, 2, 12), 2)
            .Should().Equal(Utc(2025, 1, 3, 0), Utc(2025, 1, 6, 0));
    }

    [Fact]
    public void Except_ExcludesEverything_ReturnsNullWithinLookahead()
    {
        var expr = NaturalCronExpr.Parse("every day at 09:00 except 09:00");

        expr.TryGetNextOccurrenceInUtc(Utc(2025, 1, 1), Utc(2025, 3, 1)).Should().BeNull();
    }

    [Fact]
    public void Parse_ExceptRule_ExposesInnerRules()
    {
        var expr = NaturalCronExpr.Parse("every day at 09:00 except [saturday, sunday]");

        var exceptRule = expr.Rules.OfType<NaturalCronExceptRule>().Should().ContainSingle().Subject;
        exceptRule.Spec.Should().Be(NaturalCronRuleSpec.Except);
        exceptRule.Rules.Should().ContainSingle().Which.Should().BeOfType<NaturalCronAtInOnMultiplesRule>();
    }

    [Theory]
    [InlineData("every day at 09:00 except 12")]
    [InlineData("every day at 09:00 except")]
    public void Except_InvalidValue_ReturnsError(string expression)
    {
        var (expr, errors) = NaturalCronExpr.TryParse(expression);

        expr.Should().BeNull();
        errors.Should().NotBeEmpty();
    }
}
