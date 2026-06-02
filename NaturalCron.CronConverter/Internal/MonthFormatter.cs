using NaturalCron.Builder;

namespace NaturalCron.CronConverter.Internal;

internal static class MonthFormatter
{
    private static readonly string[] ThreeLetterNames =
        { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

    /// <summary>
    /// Converts a <see cref="NaturalCronMonth"/> (Jan=1..Dec=12) to a cron month string.
    /// </summary>
    internal static string Format(NaturalCronMonth month, MonthFormat format)
    {
        return format switch
        {
            MonthFormat.ThreeLetterName => ThreeLetterNames[(int)month - 1],
            _ => ((int)month).ToString(),
        };
    }
}
