namespace NaturalCron.CronConverter;

/// <summary>
/// Controls how Nth-weekday-of-month expressions (e.g. "1stMonday", "2ndFriday") are handled.
/// The # modifier was introduced by Quartz Scheduler and is not part of POSIX cron.
/// </summary>
public enum NthWeekdaySupport
{
    /// <summary>
    /// Not supported. Behavior is determined by <see cref="NonConvertibleBehavior"/>.
    /// </summary>
    NotSupported,

    /// <summary>
    /// Use # notation for Nth-weekday-of-month: <c>{dow}#{nth}</c> for a specific occurrence,
    /// or <c>{dow}L</c> for the last occurrence.
    /// Supported by Quartz.NET, Cronos, and other schedulers that recognise the # extension.
    /// The day-of-week number is formatted according to the configured <see cref="WeekFormat"/>.
    /// Example with <see cref="WeekFormat.ZeroToSix"/> (default): "1stMonday" → "1#1" (Mon=1).
    /// Example with <see cref="WeekFormat.ThreeLetterName"/>: "1stMonday" → "MON#1".
    /// </summary>
    NthHash,
}
