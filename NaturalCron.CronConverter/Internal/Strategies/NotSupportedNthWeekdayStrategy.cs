using NaturalCron.Builder;

namespace NaturalCron.CronConverter.Internal.Strategies;

internal class NotSupportedNthWeekdayStrategy : INthWeekdayStrategy
{
    public string? Format(NaturalCronNthWeekDay nth, NaturalCronDayOfWeek dayOfWeek, WeekFormat weekFormat) => null;
}
