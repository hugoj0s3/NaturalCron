namespace NaturalCron.CronConverter.Tests;

public class ConvertExtensionMethodTests
{
    [Fact]
    public void ToCron_EveryDayAt9_ReturnsCronString()
    {
        var expr = NaturalCronExpr.Parse("every day at 09:00");
        expr.ToCronExpression().Should().Be("0 9 * * *");
    }

    [Fact]
    public void ToCron_WithOptions_AppliesWeekFormatOption()
    {
        var expr = NaturalCronExpr.Parse("every day at 09:00 on [monday, friday]");
        expr.ToCronExpression(new CronConverterOptions { WeekFormat = WeekFormat.ThreeLetterName })
            .Should().Be("0 9 * * MON,FRI");
    }

    [Fact]
    public void TryToCron_NonConvertibleFeature_ReturnsResultWithErrors()
    {
        var expr = NaturalCronExpr.Parse("every day at 09:00 tz America/New_York");
        var result = expr.TryToCronExpression();
        result.Errors.Should().ContainSingle(e => e.Contains("Timezone"));
    }

    [Fact]
    public void TryToCron_ValidExpression_IsSuccessTrue()
    {
        var expr = NaturalCronExpr.Parse("every 30 minutes");
        var result = expr.TryToCronExpression();
        result.IsSuccess.Should().BeTrue();
        result.CronExpression.Should().Be("*/30 * * * *");
    }
}
