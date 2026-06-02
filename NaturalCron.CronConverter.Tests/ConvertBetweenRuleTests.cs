namespace NaturalCron.CronConverter.Tests;

public class ConvertBetweenRuleTests
{
    // ── Day-of-week range ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("every day between monday and friday",   "* * * * 1-5")]
    [InlineData("every day between sunday and saturday", "* * * * 0-6")]
    [InlineData("every day between monday and wednesday","* * * * 1-3")]
    public void Convert_BetweenWeekdays_ZeroToSix_ReturnsDowRange(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    [Theory]
    [InlineData("every day between monday and friday",   "* * * * 1-5")]
    [InlineData("every day between monday and saturday", "* * * * 1-6")]
    public void Convert_BetweenWeekdays_OneToSeven_ReturnsDowRange(string expression, string expected)
        => Convert(expression, new CronConverterOptions { WeekFormat = WeekFormat.OneToSeven })
            .Should().Be(expected);

    [Theory]
    [InlineData("every day between monday and friday",   "* * * * MON-FRI")]
    [InlineData("every day between sunday and saturday", "* * * * SUN-SAT")]
    public void Convert_BetweenWeekdays_ThreeLetterName_ReturnsNamedDowRange(string expression, string expected)
        => Convert(expression, new CronConverterOptions { WeekFormat = WeekFormat.ThreeLetterName })
            .Should().Be(expected);

    // ── Month range ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every day between jan and jun",  "* * * 1-6 *")]
    [InlineData("every day between mar and nov",  "* * * 3-11 *")]
    [InlineData("every day between jan and dec",  "* * * 1-12 *")]
    public void Convert_BetweenMonths_ReturnsMonthRange(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    [Theory]
    [InlineData("every day between jan and jun", "* * * JAN-JUN *")]
    [InlineData("every day between mar and nov", "* * * MAR-NOV *")]
    public void Convert_BetweenMonths_ThreeLetterName_ReturnsNamedMonthRange(string expression, string expected)
        => Convert(expression, new CronConverterOptions { MonthFormat = MonthFormat.ThreeLetterName })
            .Should().Be(expected);

    // ── upto / from (converted to ranges) ────────────────────────────────────

    [Fact]
    public void Convert_UptoDay_ReturnsStartBoundedDomRange()
        => Convert("every day upto 15th").Should().Be("* * 1-15 * *");

    [Fact]
    public void Convert_FromDay_ReturnsEndBoundedDomRange()
        => Convert("every day from 10th").Should().Be("* * 10-31 * *");

    [Fact]
    public void Convert_UptoMonth_ReturnsStartBoundedMonthRange()
        => Convert("every day upto june").Should().Be("* * * 1-6 *");

    [Fact]
    public void Convert_FromMonth_ReturnsEndBoundedMonthRange()
        => Convert("every day from june").Should().Be("* * * 6-12 *");

    // ── Day-of-month range ────────────────────────────────────────────────────

    [Fact]
    public void Convert_BetweenDays_ReturnsDomRange()
        => Convert("every hour between 10th and 15th").Should().Be("0 * 10-15 * *");

    // ── Hour-only range (single unit — maps directly to the cron hour field) ────

    [Theory]
    [InlineData("every 15 minutes between 8am and 12pm",  "*/15 8-12 * * *")]
    [InlineData("every 30 minutes between 9am and 5pm",   "*/30 9-17 * * *")]
    [InlineData("every hour between 8am and 6pm",         "0 8-18 * * *")]
    [InlineData("every 15 minutes between 8hr and 12hr",  "*/15 8-12 * * *")]
    [InlineData("every 30 minutes between 9hr and 17hr",  "*/30 9-17 * * *")]
    public void Convert_BetweenHourOnly_ReturnsHourRangeInCronField(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    // ── Non-convertible: multiple time units in the same range ────────────────
    // HH:MM produces Hour+Minute, HH:MM:SS produces Hour+Minute+Second — both rejected.

    [Theory]
    [InlineData("every 30 minutes between 09:00 and 18:00")]
    [InlineData("every 30 minutes between 09:00:00 and 18:00:00")]
    public void Convert_BetweenMultipleTimeUnits_ThrowsCronConversionException(string expression)
    {
        var act = () => Convert(expression);
        act.Should().Throw<CronConversionException>()
            .Which.Reasons.Should().ContainSingle(r => r.Contains("multiple time units"));
    }

    [Fact]
    public void Convert_MultipleBetweenRanges_ThrowsCronConversionException()
    {
        var act = () => Convert("every 25 secs between [1:00pm and 03:00pm, 6:00pm and 8:00pm]");
        act.Should().Throw<CronConversionException>();
    }

    [Fact]
    public void Convert_BetweenTimes_WithOmit_ReturnsPartialWithError()
    {
        var result = TryConvert("every 30 minutes between 09:00 and 18:00",
            new CronConverterOptions { NonConvertibleBehavior = NonConvertibleBehavior.Omit });
        result.IsSuccess.Should().BeFalse();
        result.CronExpression.Should().Be("*/30 * * * *");
        result.Errors.Should().ContainSingle(e => e.Contains("multiple time units"));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Convert(string expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.Convert(expr, options);

    private static CronConversionResult TryConvert(string expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.TryConvert(expr, options);
}
