using NaturalCron.Builder.Selectors;

namespace NaturalCron.CronConverter;

public static class NaturalCronExprExtensions
{
    /// <summary>
    /// Converts this NaturalCron expression to a classic cron string.
    /// Throws <see cref="CronConversionException"/> for non-convertible features
    /// when <see cref="NonConvertibleBehavior.ThrowException"/> is set (the default).
    /// </summary>
    public static string ToCronExpression(this NaturalCronExpr expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.Convert(expr, options);

    /// <summary>
    /// Tries to convert this NaturalCron expression to a classic cron string.
    /// Never throws — non-convertible features are reported in <see cref="CronConversionResult.Errors"/>.
    /// </summary>
    public static CronConversionResult TryToCronExpression(this NaturalCronExpr expr, CronConverterOptions? options = null)
        => NaturalCronToCronConverter.TryConvert(expr, options);

    /// <summary>
    /// Builds the expression and converts it to a classic cron string.
    /// Throws <see cref="CronConversionException"/> for non-convertible features
    /// when <see cref="NonConvertibleBehavior.ThrowException"/> is set (the default).
    /// </summary>
    public static string ToCronExpression(this INaturalCronBuildSelector builder, CronConverterOptions? options = null)
        => builder.Build().ToCronExpression(options);

    /// <summary>
    /// Builds the expression and tries to convert it to a classic cron string.
    /// Never throws — non-convertible features are reported in <see cref="CronConversionResult.Errors"/>.
    /// </summary>
    public static CronConversionResult TryToCronExpression(this INaturalCronBuildSelector builder, CronConverterOptions? options = null)
        => builder.Build().TryToCronExpression(options);
}
