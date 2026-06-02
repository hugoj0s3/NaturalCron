namespace NaturalCron.CronConverter.Tests;

public class ConvertSpecialDayTests
{
    // ── ClosestWeekdayTo ──────────────────────────────────────────────────────

    [Fact]
    public void Convert_ClosestWeekdayTo_NotSupported_ThrowsCronConversionException()
    {
        var act = () => Convert("every month on ClosestWeekdayTo 15th");
        act.Should().Throw<CronConversionException>()
            .Which.Reasons.Should().ContainSingle(r => r.Contains("ClosestWeekdayTo"));
    }

    [Theory]
    [InlineData("every month on ClosestWeekdayTo 15th",  "* * 15W * *")]
    [InlineData("every month on ClosestWeekdayTo 1st",   "* * 1W * *")]
    [InlineData("every month on ClosestWeekdayTo 30th",  "* * 30W * *")]
    public void Convert_ClosestWeekdayTo_NearestWeekdayW_ReturnsWNotation(string expression, string expected)
        => Convert(expression, new CronConverterOptions { ClosestWeekdaySupport = ClosestWeekdaySupport.NearestWeekdayW })
            .Should().Be(expected);

    // ── NthWeekday ────────────────────────────────────────────────────────────

    [Fact]
    public void Convert_NthWeekday_NotSupported_ThrowsCronConversionException()
    {
        var act = () => Convert("every month on 1stMonday");
        act.Should().Throw<CronConversionException>()
            .Which.Reasons.Should().ContainSingle(r => r.Contains("weekday"));
    }

    // ZeroToSix (default): Sun=0, Mon=1, Tue=2, Wed=3, Thu=4, Fri=5, Sat=6
    [Theory]
    [InlineData("every month on 1stMonday",   "* * * * 1#1")]
    [InlineData("every month on 2ndFriday",   "* * * * 5#2")]
    [InlineData("every month on 3rdWednesday","* * * * 3#3")]
    [InlineData("every month on 4thTuesday",  "* * * * 2#4")]
    [InlineData("every month on 5thSaturday", "* * * * 6#5")]
    public void Convert_NthWeekday_NthHash_ZeroToSix_ReturnsHashNotation(string expression, string expected)
        => Convert(expression, new CronConverterOptions { NthWeekdaySupport = NthWeekdaySupport.NthHash })
            .Should().Be(expected);

    // OneToSeven: Mon=1, Tue=2, Wed=3, Thu=4, Fri=5, Sat=6, Sun=7
    [Theory]
    [InlineData("every month on 1stMonday",   "* * * * 1#1")]
    [InlineData("every month on 2ndFriday",   "* * * * 5#2")]
    [InlineData("every month on 5thSaturday", "* * * * 6#5")]
    [InlineData("every month on 1stSunday",   "* * * * 7#1")]
    public void Convert_NthWeekday_NthHash_OneToSeven_ReturnsHashNotation(string expression, string expected)
        => Convert(expression, new CronConverterOptions { NthWeekdaySupport = NthWeekdaySupport.NthHash, WeekFormat = WeekFormat.OneToSeven })
            .Should().Be(expected);

    [Theory]
    [InlineData("every month on LastMonday",  "* * * * 1L")]
    [InlineData("every month on LastFriday",  "* * * * 5L")]
    [InlineData("every month on LastSunday",  "* * * * 0L")]
    public void Convert_LastNthWeekday_NthHash_ZeroToSix_ReturnsLNotation(string expression, string expected)
        => Convert(expression, new CronConverterOptions { NthWeekdaySupport = NthWeekdaySupport.NthHash })
            .Should().Be(expected);

    // ── Combination: time + Quartz ────────────────────────────────────────────

    [Fact]
    public void Convert_MonthlyAt9_On1stMonday_NthHash_ZeroToSix_ReturnsFullExpression()
        => Convert("every month on 1stMonday at 09:00",
                new CronConverterOptions { NthWeekdaySupport = NthWeekdaySupport.NthHash })
            .Should().Be("0 9 * * 1#1");

    // ── Timezone (always omitted) ─────────────────────────────────────────────

    [Fact]
    public void Convert_WithTimezone_ThrowsCronConversionException()
    {
        var act = () => Convert("every day at 09:00 tz America/New_York");
        act.Should().Throw<CronConversionException>()
            .Which.Reasons.Should().ContainSingle(r => r.Contains("Timezone"));
    }

    [Fact]
    public void Convert_WithTimezone_WithOmit_ReturnsExpressionWithoutTimezone()
    {
        var result = TryConvert("every day at 09:00 tz America/New_York",
            new CronConverterOptions { NonConvertibleBehavior = NonConvertibleBehavior.Omit });
        result.CronExpression.Should().Be("0 9 * * *");
        result.Errors.Should().ContainSingle(e => e.Contains("Timezone"));
    }

    // ── DomDowMutualExclusion ─────────────────────────────────────────────────

    [Fact]
    public void Convert_DomDowMutualExclusion_NeitherDomNorDowSet_DowBecomesQuestionMark()
        => Convert("every day at 18:00",
                new CronConverterOptions { DomDowMutualExclusion = true })
            .Should().Be("0 18 * * ?");

    [Fact]
    public void Convert_DomDowMutualExclusion_DowSet_DomBecomesQuestionMark()
        => Convert("every day between monday and friday at 18:00",
                new CronConverterOptions { DomDowMutualExclusion = true })
            .Should().Be("0 18 ? * 1-5");

    [Fact]
    public void Convert_DomDowMutualExclusion_DomSet_DowBecomesQuestionMark()
        => Convert("every month on 15th at 09:00",
                new CronConverterOptions { DomDowMutualExclusion = true })
            .Should().Be("0 9 15 * ?");

    [Fact]
    public void Convert_DomDowMutualExclusion_WithSeconds_ProducesCorrect6FieldExpression()
        => Convert("every day at 18:00",
                new CronConverterOptions { SupportSeconds = true, DomDowMutualExclusion = true })
            .Should().Be("0 0 18 * * ?");

    [Fact]
    public void Convert_DomDowMutualExclusion_False_NoDomOrDowQuestionMark()
        => Convert("every day at 18:00")
            .Should().Be("0 18 * * *");

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Convert(string expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.Convert(expr, options);

    private static CronConversionResult TryConvert(string expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.TryConvert(expr, options);
}
