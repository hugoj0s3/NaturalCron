using NaturalCron.CustomRules;

namespace NaturalCron.CronConverter.Tests;

/// <summary>
/// Custom keyword, mixed-list and except rules have no cron equivalent.
/// </summary>
public class ConvertUnsupportedRuleTests
{
    private static readonly CronConverterOptions OmitOptions =
        new() { NonConvertibleBehavior = NonConvertibleBehavior.Omit };

    static ConvertUnsupportedRuleTests()
    {
        // Custom keywords can be configured only once per process.
        if (!NaturalCronCustomKeywords.IsConfigured)
        {
            NaturalCronCustomKeywords.Configure(cfg =>
                cfg.AddMatchable("holiday", dateTime => dateTime.Month == 12 && dateTime.Day == 25));
        }
    }

    [Theory]
    [InlineData("every day on holiday at 09:00", "Custom keyword")]
    [InlineData("every holiday at 09:00", "Custom keyword")]
    [InlineData("every day on [holiday, 15th] at 09:00", "mixes custom keywords")]
    [InlineData("every day at 09:00 except holiday", "Except rule")]
    [InlineData("every day at 09:00 except [saturday, sunday]", "Except rule")]
    [InlineData("every hour except [12:00, 13:00]", "Except rule")]
    public void Convert_UnsupportedRule_ThrowsCronConversionException(string expression, string expectedError)
    {
        var act = () => Convert(expression);
        act.Should().Throw<CronConversionException>()
            .WithMessage($"*{expectedError}*not supported*");
    }

    [Theory]
    [InlineData("every day on holiday at 09:00", "Custom keyword")]
    [InlineData("every day on [holiday, 15th] at 09:00", "mixes custom keywords")]
    [InlineData("every day at 09:00 except holiday", "Except rule")]
    [InlineData("every day at 09:00 except [saturday, sunday]", "Except rule")]
    public void TryConvert_UnsupportedRule_ReturnsError(string expression, string expectedError)
    {
        var result = TryConvert(expression);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains(expectedError) && e.Contains("not supported"));
    }

    [Theory]
    [InlineData("every day on holiday at 09:00", "0 9 * * *")]
    [InlineData("every holiday at 09:00", "0 9 * * *")]
    [InlineData("every day on [holiday, 15th] at 09:00", "0 9 * * *")]
    [InlineData("every day at 09:00 except holiday", "0 9 * * *")]
    [InlineData("every day at 09:00 except [saturday, sunday]", "0 9 * * *")]
    [InlineData("every hour except [12:00, 13:00]", "0 * * * *")]
    public void TryConvert_UnsupportedRuleWithOmit_DropsTheRule(string expression, string expected)
    {
        var result = TryConvert(expression, OmitOptions);
        result.CronExpression.Should().Be(expected);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("not supported"));
    }

    [Theory]
    [InlineData("every monday at 09:00", "0 9 * * 1")]
    [InlineData("every week on monday at 09:00", "0 9 * * 1")]
    [InlineData("every [monday, friday] at 09:00", "0 9 * * 1,5")]
    [InlineData("every 15th at 18:00", "0 18 15 * *")]
    [InlineData("every 09:00", "0 9 * * *")]
    public void Convert_EveryValueShorthand_ReturnsExpectedCron(string expression, string expected)
        => Convert(expression).Should().Be(expected);

    [Theory]
    [InlineData("every monday at 09:00", "every week on monday at 09:00")]
    [InlineData("every 15th at 18:00", "every month on 15th at 18:00")]
    [InlineData("every 9am", "every day at 9am")]
    [InlineData("every 09:30", "every day at 09:30")]
    public void Convert_EveryValueShorthand_ConvertsLikeTheExplicitForm(string shorthand, string explicitForm)
        => Convert(shorthand).Should().Be(Convert(explicitForm));

    [Fact]
    public void Convert_EveryMonthShorthand_ThrowsLikeEveryYear()
    {
        // every january is every year in january, and cron has no year field.
        var act = () => Convert("every january at 09:00");
        act.Should().Throw<CronConversionException>().WithMessage("*year*");
    }

    private static string Convert(string expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.Convert(expr, options);

    private static CronConversionResult TryConvert(string expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.TryConvert(expr, options);
}
