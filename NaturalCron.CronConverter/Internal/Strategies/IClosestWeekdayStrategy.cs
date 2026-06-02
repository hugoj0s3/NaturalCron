namespace NaturalCron.CronConverter.Internal.Strategies;

internal interface IClosestWeekdayStrategy
{
    /// <summary>
    /// Returns the cron day-of-month fragment for "closest weekday to day <paramref name="dayOfMonth"/>".
    /// Returns null when this strategy does not support the feature (non-convertible).
    /// </summary>
    string? Format(int dayOfMonth);
}
