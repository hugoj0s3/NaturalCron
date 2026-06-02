namespace NaturalCron.CronConverter.Tests;

/// <summary>
/// Integration-style tests for common real-world expression combinations.
/// </summary>
public class ConvertCombinedExpressionTests
{
    [Theory]
    [InlineData("every day between monday and friday at 18:00", "0 18 * * 1-5")]
    [InlineData("every day between mon and fri at 9:00",        "0 9 * * 1-5")]
    public void Convert_WeekdayRangeWithTime_ReturnsExpectedCron(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    [Theory]
    [InlineData("every 30 minutes in [jan, jun]", "*/30 * * 1,6 *")]
    [InlineData("every 15 minutes in [mar, jun, sep, dec]", "*/15 * * 3,6,9,12 *")]
    public void Convert_StepMinutesInSpecificMonths_ReturnsExpectedCron(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    [Fact]
    public void Convert_EveryDayAtTimeOnWeekdays_ReturnsExpectedCron()
        => Convert("every day at 10:00 on [monday, wednesday, friday]")
            .Should().Be("0 10 * * 1,3,5");

    [Fact]
    public void Convert_Every2HoursOnWeekdaysInSummer_ReturnsExpectedCron()
        => Convert("every 2 hours on [monday, friday] between jun and aug")
            .Should().Be("0 */2 * 6-8 1,5");

    [Fact]
    public void Convert_MonthlyOn15thAt9_ReturnsExpectedCron()
        => Convert("every month on 15th at 09:00").Should().Be("0 9 15 * *");

    [Fact]
    public void Convert_Every6MonthsAt9OnFirst_ReturnsExpectedCron()
        => Convert("every 6 months on 1st at 09:00").Should().Be("0 9 1 */6 *");

    [Fact]
    public void Convert_EveryWeekOnMondayAt9_ReturnsExpectedCron()
        => Convert("every week on monday at 09:00").Should().Be("0 9 * * 1");

    [Fact]
    public void Convert_TimeWindowOmitted_ReturnsPartialCronWithoutTimeConstraint()
        => TryConvert("every 30 minutes in [jan, jun] between 09:00 and 18:00",
                new CronConverterOptions { NonConvertibleBehavior = NonConvertibleBehavior.Omit })
            .CronExpression.Should().Be("*/30 * * 1,6 *");

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Convert(string expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.Convert(expr, options);

    private static CronConversionResult TryConvert(string expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.TryConvert(expr, options);
}
