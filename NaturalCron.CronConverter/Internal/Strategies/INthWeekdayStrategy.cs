using NaturalCron.Builder;

namespace NaturalCron.CronConverter.Internal.Strategies;

internal interface INthWeekdayStrategy
{
    /// <summary>
    /// Returns the cron day-of-week fragment for "Nth weekday of the month".
    /// Returns null when this strategy does not support the feature (non-convertible).
    /// </summary>
    string? Format(NaturalCronNthWeekDay nth, NaturalCronDayOfWeek dayOfWeek, WeekFormat weekFormat);
}
