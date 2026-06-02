namespace NaturalCron.CronConverter;

/// <summary>
/// Controls how "closest weekday to Nth" day expressions are handled during conversion.
/// The W modifier was introduced by Quartz Scheduler and is not part of POSIX cron.
/// </summary>
public enum ClosestWeekdaySupport
{
    /// <summary>
    /// Not supported. Behavior is determined by <see cref="NonConvertibleBehavior"/>.
    /// </summary>
    NotSupported,

    /// <summary>
    /// Use W notation: e.g. "15W" means the weekday nearest to the 15th.
    /// Supported by Quartz.NET, Cronos, and other schedulers that recognise the W extension.
    /// </summary>
    NearestWeekdayW,
}
