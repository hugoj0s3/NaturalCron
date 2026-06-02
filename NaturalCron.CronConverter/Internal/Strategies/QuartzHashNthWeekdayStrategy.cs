using NaturalCron.Builder;

namespace NaturalCron.CronConverter.Internal.Strategies;

/// <summary>
/// Quartz # notation: {dow}#{nth} e.g. "1#1" = first Sunday in ZeroToSix, "2#1" = first Sunday in Quartz/NaturalCron numbering.
/// The day-of-week number is formatted according to the configured <see cref="WeekFormat"/>.
/// For Last occurrence uses {dow}L notation e.g. "2L" = last Monday in Quartz numbering.
/// </summary>
internal class QuartzHashNthWeekdayStrategy : INthWeekdayStrategy
{
    public string? Format(NaturalCronNthWeekDay nth, NaturalCronDayOfWeek dayOfWeek, WeekFormat weekFormat)
    {
        var dow = WeekdayFormatter.Format(dayOfWeek, weekFormat);
        return nth == NaturalCronNthWeekDay.Last ? $"{dow}L" : $"{dow}#{(int)nth}";
    }
}
