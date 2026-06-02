namespace NaturalCron.CronConverter;

public enum NonConvertibleBehavior
{
    /// <summary>
    /// Throw a <see cref="CronConversionException"/> when a NaturalCron feature
    /// cannot be represented in classic cron syntax.
    /// </summary>
    ThrowException,

    /// <summary>
    /// Omit the non-convertible feature and continue building the expression.
    /// Use <see cref="NaturalCronToCronConverter.TryConvert"/> to retrieve the
    /// list of features that were omitted.
    /// </summary>
    Omit,
}
