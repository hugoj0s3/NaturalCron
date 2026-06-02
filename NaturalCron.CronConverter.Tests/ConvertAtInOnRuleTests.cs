namespace NaturalCron.CronConverter.Tests;

public class ConvertAtInOnRuleTests
{
    // ── Time ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every day at 09:00",  "0 9 * * *")]
    [InlineData("every day at 18:00",  "0 18 * * *")]
    [InlineData("every day at 00:00",  "0 0 * * *")]
    [InlineData("every day at 6:00pm", "0 18 * * *")]
    [InlineData("every day at 9:00am", "0 9 * * *")]
    public void Convert_EveryDayAtTime_ReturnsCronWithMinuteAndHour(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    [Fact]
    public void Convert_MultipleTimesInBrackets_ReturnsCommaHourField()
        => Convert("every day at [10:00, 14:00, 18:00]").Should().Be("0 10,14,18 * * *");

    // ── Day of month ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every month on 1st",   "* * 1 * *")]
    [InlineData("every month on 15th",  "* * 15 * *")]
    [InlineData("every month on 31st",  "* * 31 * *")]
    public void Convert_OnSpecificDayOfMonth_SetsDomField(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    [Fact]
    public void Convert_OnFirstDay_SetsDomToOne()
        => Convert("every month on FirstDay").Should().Be("* * 1 * *");

    [Fact]
    public void Convert_OnMultipleDays_ReturnsCommaSeparatedDom()
        => Convert("every month on [1st, 15th, 20th]").Should().Be("* * 1,15,20 * *");

    // ── Day of week — ZeroToSix (default) ────────────────────────────────────

    [Theory]
    [InlineData("every week on sunday",    "* * * * 0")]
    [InlineData("every week on monday",    "* * * * 1")]
    [InlineData("every week on tuesday",   "* * * * 2")]
    [InlineData("every week on wednesday", "* * * * 3")]
    [InlineData("every week on thursday",  "* * * * 4")]
    [InlineData("every week on friday",    "* * * * 5")]
    [InlineData("every week on saturday",  "* * * * 6")]
    public void Convert_OnWeekday_ZeroToSix_ReturnsCorrectDow(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    // ── Day of week — OneToSeven ──────────────────────────────────────────────

    [Theory]
    [InlineData("every week on monday",    "* * * * 1")]
    [InlineData("every week on saturday",  "* * * * 6")]
    [InlineData("every week on sunday",    "* * * * 7")]
    public void Convert_OnWeekday_OneToSeven_ReturnsCorrectDow(string expression, string expected)
        => Convert(expression, new CronConverterOptions { WeekFormat = WeekFormat.OneToSeven })
            .Should().Be(expected);

    // ── Day of week — ThreeLetterName ─────────────────────────────────────────

    [Theory]
    [InlineData("every week on sunday",    "* * * * SUN")]
    [InlineData("every week on monday",    "* * * * MON")]
    [InlineData("every week on friday",    "* * * * FRI")]
    [InlineData("every week on saturday",  "* * * * SAT")]
    public void Convert_OnWeekday_ThreeLetterName_ReturnsNamedDow(string expression, string expected)
        => Convert(expression, new CronConverterOptions { WeekFormat = WeekFormat.ThreeLetterName })
            .Should().Be(expected);

    [Fact]
    public void Convert_OnMultipleWeekdays_ReturnsCommaSeparatedDow()
        => Convert("every day at 10:00 on [monday, wednesday, friday]")
            .Should().Be("0 10 * * 1,3,5");

    [Fact]
    public void Convert_OnMultipleWeekdays_ThreeLetterNames_ReturnsNamedDow()
        => Convert("every day at 10:00 on [monday, wednesday, friday]",
                new CronConverterOptions { WeekFormat = WeekFormat.ThreeLetterName })
            .Should().Be("0 10 * * MON,WED,FRI");

    // ── Month ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every day in january",  "* * * 1 *")]
    [InlineData("every day in june",     "* * * 6 *")]
    [InlineData("every day in december", "* * * 12 *")]
    public void Convert_InMonth_SetsMonthField(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    [Theory]
    [InlineData("every day in january",  "* * * JAN *")]
    [InlineData("every day in june",     "* * * JUN *")]
    [InlineData("every day in december", "* * * DEC *")]
    public void Convert_InMonth_ThreeLetterName_ReturnsNamedMonth(string expression, string expected)
        => Convert(expression, new CronConverterOptions { MonthFormat = MonthFormat.ThreeLetterName })
            .Should().Be(expected);

    [Fact]
    public void Convert_InMultipleMonths_ReturnsCommaSeparatedMonthField()
        => Convert("every day in [jan, jun]").Should().Be("* * * 1,6 *");

    [Fact]
    public void Convert_InMultipleMonths_ThreeLetterNames_ReturnsNamedMonths()
        => Convert("every day in [jan, jun]",
                new CronConverterOptions { MonthFormat = MonthFormat.ThreeLetterName })
            .Should().Be("* * * JAN,JUN *");

    // ── Non-convertible day expressions ──────────────────────────────────────

    [Fact]
    public void Convert_OnLastDay_ThrowsCronConversionException()
    {
        var act = () => Convert("every month on LastDay");
        act.Should().Throw<CronConversionException>()
            .Which.Reasons.Should().ContainSingle(r => r.Contains("LastDay"));
    }

    [Fact]
    public void Convert_OnLastWeekday_ThrowsCronConversionException()
    {
        var act = () => Convert("every month on LastWeekday");
        act.Should().Throw<CronConversionException>()
            .Which.Reasons.Should().ContainSingle(r => r.Contains("LastWeekday"));
    }

    [Fact]
    public void Convert_OnLastDayArithmetic_ThrowsCronConversionException()
    {
        var act = () => Convert("every month on LastDay - 1");
        act.Should().Throw<CronConversionException>();
    }

    [Fact]
    public void Convert_YearRule_ThrowsCronConversionException()
    {
        var act = () => Convert("every year");
        act.Should().Throw<CronConversionException>()
            .Which.Reasons.Should().ContainSingle(r => r.Contains("year"));
    }

    // ── Seconds field ─────────────────────────────────────────────────────────

    [Fact]
    public void Convert_AtSecond_WithSupportSeconds_Returns6FieldCron()
        => Convert("every minute at second 30", new CronConverterOptions { SupportSeconds = true })
            .Should().Be("30 * * * * *");

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Convert(string expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.Convert(expr, options);
}
