namespace NaturalCron.CronConverter.Tests;

public class ConvertEveryRuleTests
{
    // ── Minutes ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every minute")]
    [InlineData("minutely")]
    public void Convert_EveryMinute_ReturnsWildcardMinute(string expression)
        => Convert(expression).Should().Be("* * * * *");

    [Theory]
    [InlineData("every 5 minutes",  "*/5 * * * *")]
    [InlineData("every 30 minutes", "*/30 * * * *")]
    [InlineData("every 15 min",     "*/15 * * * *")]
    public void Convert_EveryNMinutes_ReturnsStepMinuteField(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    // ── Hours ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every hour")]
    [InlineData("hourly")]
    public void Convert_EveryHour_ReturnsZeroMinuteWildcardHour(string expression)
        => Convert(expression).Should().Be("0 * * * *");

    [Theory]
    [InlineData("every 2 hours",  "0 */2 * * *")]
    [InlineData("every 6 hours",  "0 */6 * * *")]
    [InlineData("every 12 hours", "0 */12 * * *")]
    public void Convert_EveryNHours_ReturnsStepHourWithZeroMinute(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    [Fact]
    public void Convert_EveryNHoursWithExplicitMinute_UsesExplicitMinuteNotZero()
        => Convert("every 2 hours at minute 30").Should().Be("30 */2 * * *");

    // ── Days ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every day")]
    [InlineData("daily")]
    public void Convert_EveryDay_ReturnsAllWildcards(string expression)
        => Convert(expression).Should().Be("* * * * *");

    [Theory]
    [InlineData("every 2 days", "* * */2 * *")]
    [InlineData("every 3 days", "* * */3 * *")]
    public void Convert_EveryNDays_ReturnsStepDomField(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    // ── Months ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every month")]
    [InlineData("monthly")]
    public void Convert_EveryMonth_ReturnsAllWildcards(string expression)
        => Convert(expression).Should().Be("* * * * *");

    [Theory]
    [InlineData("every 2 months",  "* * * */2 *")]
    [InlineData("every 3 months",  "* * * */3 *")]
    [InlineData("every 6 months",  "* * * */6 *")]
    [InlineData("every 12 months", "* * * */12 *")]
    public void Convert_EveryNMonths_ReturnsStepMonthField(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    // ── Seconds ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every second",    "* * * * * *")]
    [InlineData("every 10 seconds", "*/10 * * * * *")]
    [InlineData("every 30 secs",   "*/30 * * * * *")]
    public void Convert_EveryNSeconds_WithSupportSeconds_Returns6FieldCron(string expression, string expected)
        => Convert(expression, new CronConverterOptions { SupportSeconds = true }).Should().Be(expected);

    [Fact]
    public void Convert_EverySeconds_WithoutSupportSeconds_ThrowsCronConversionException()
    {
        var act = () => Convert("every 10 seconds");
        act.Should().Throw<CronConversionException>()
            .Which.Reasons.Should().ContainSingle(r => r.Contains("SupportSeconds"));
    }

    // ── Non-convertible ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("every week on monday", "* * * * 1")]
    [InlineData("every week on friday", "* * * * 5")]
    public void Convert_EveryWeekOnWeekday_ReturnsCorrectDow(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    [Theory]
    [InlineData("every 2 weeks")]
    [InlineData("every 3 weeks")]
    public void Convert_EveryNWeeksGreaterThanOne_ThrowsCronConversionException(string expression)
    {
        var act = () => Convert(expression);
        act.Should().Throw<CronConversionException>()
            .Which.Reasons.Should().ContainSingle(r => r.Contains("week"));
    }

    [Theory]
    [InlineData("every year")]
    [InlineData("yearly")]
    public void Convert_EveryYear_ThrowsCronConversionException(string expression)
    {
        var act = () => Convert(expression);
        act.Should().Throw<CronConversionException>();
    }

    [Fact]
    public void Convert_AnchoredOn_AddsErrorAboutAnchoredOn()
    {
        var result = TryConvert("every 2 months AnchoredOn Nov on 1st");
        result.Errors.Should().Contain(e => e.Contains("AnchoredOn"));
    }

    [Fact]
    public void Convert_Every2WeeksWithOmit_ReturnsPartialResultWithError()
    {
        var result = TryConvert("every 2 weeks",
            new CronConverterOptions { NonConvertibleBehavior = NonConvertibleBehavior.Omit });
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("week"));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Convert(string expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.Convert(expr, options);

    private static CronConversionResult TryConvert(string expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.TryConvert(expr, options);
}
