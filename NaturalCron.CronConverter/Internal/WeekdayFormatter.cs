using NaturalCron.Builder;

namespace NaturalCron.CronConverter.Internal;

internal static class WeekdayFormatter
{
    private static readonly string[] ThreeLetterNames = { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" };

    /// <summary>
    /// Converts a <see cref="NaturalCronDayOfWeek"/> (Sun=1..Sat=7) to a cron day-of-week string.
    /// </summary>
    internal static string Format(NaturalCronDayOfWeek dayOfWeek, WeekFormat format)
    {
        return format switch
        {
            WeekFormat.ZeroToSix => ((int)dayOfWeek - 1).ToString(),
            WeekFormat.OneToSeven => dayOfWeek == NaturalCronDayOfWeek.Sun ? "7" : ((int)dayOfWeek - 1).ToString(),
            WeekFormat.ThreeLetterName => ThreeLetterNames[(int)dayOfWeek - 1],
            _ => ((int)dayOfWeek - 1).ToString(),
        };
    }
}
