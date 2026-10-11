using NaturalCron.CustomRules;
using TimeZoneConverter;

namespace NaturalCron.UnitTests;

/// <summary>
/// The custom keywords used by GetNextOcurrencesCustomKeywordsTestCases.json.
/// Every keyword is a plain condition on the wall-clock time, so the expected dates of a test case can be checked by hand.
/// </summary>
internal static class CustomKeywordsForJsonTests
{
    public static readonly HashSet<DateTime> HolidayDates = new()
    {
        new(2024, 12, 25), new(2025, 1, 1), new(2025, 4, 18), new(2025, 4, 21), new(2025, 5, 1), new(2025, 12, 25),
        new(2025, 12, 26), new(2026, 1, 1), new(2026, 4, 3), new(2026, 5, 1), new(2026, 12, 25), new(2027, 1, 1),
    };

    private static readonly int[] FibonacciDays = { 1, 2, 3, 5, 8, 13, 21 };

    public static bool IsPrime(int number)
    {
        if (number < 2)
        {
            return false;
        }

        for (var divisor = 2; divisor * divisor <= number; divisor++)
        {
            if (number % divisor == 0)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsHoliday(DateTime d) => HolidayDates.Contains(d.Date);
    public static bool IsWeekend(DateTime d) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public static bool IsBusinessDay(DateTime d) => !IsWeekend(d) && !IsHoliday(d);
    public static bool IsLastDayOfMonth(DateTime d) => d.Day == DateTime.DaysInMonth(d.Year, d.Month);

    public static bool IsLastBusinessDay(DateTime d)
    {
        if (!IsBusinessDay(d))
        {
            return false;
        }

        for (var day = d.Day + 1; day <= DateTime.DaysInMonth(d.Year, d.Month); day++)
        {
            if (IsBusinessDay(new DateTime(d.Year, d.Month, day)))
            {
                return false;
            }
        }

        return true;
    }

    // Open on weekdays from 10:00 to 15:59 New York time, whatever zone the expression is evaluated in.
    public static bool IsNewYorkMarketOpen(DateTime dateTimeInTz, TimeZoneInfo timeZone)
    {
        var newYork = TimeZoneInfo.ConvertTime(dateTimeInTz, timeZone, TZConvert.GetTimeZoneInfo("America/New_York"));
        return !IsWeekend(newYork) && newYork.Hour is >= 10 and < 16;
    }

    /// <summary>
    /// Keyword name => (time unit, condition). The time zone aware keyword nyMarketOpen is registered separately.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (NaturalCronTimeUnit TimeUnit, Func<DateTime, bool> Matches)> Keywords =
        new Dictionary<string, (NaturalCronTimeUnit, Func<DateTime, bool>)>(StringComparer.OrdinalIgnoreCase)
        {
            // Seconds
            ["primeSecond"] = (NaturalCronTimeUnit.Second, d => IsPrime(d.Second)),
            ["evenSecond"] = (NaturalCronTimeUnit.Second, d => d.Second % 2 == 0),
            ["oddSecond"] = (NaturalCronTimeUnit.Second, d => d.Second % 2 == 1),

            // Minutes
            ["primeMinute"] = (NaturalCronTimeUnit.Minute, d => IsPrime(d.Minute)),
            ["evenMinute"] = (NaturalCronTimeUnit.Minute, d => d.Minute % 2 == 0),
            ["quarterHour"] = (NaturalCronTimeUnit.Minute, d => d.Minute % 15 == 0),

            // Hours
            ["primeHour"] = (NaturalCronTimeUnit.Hour, d => IsPrime(d.Hour)),
            ["evenHour"] = (NaturalCronTimeUnit.Hour, d => d.Hour % 2 == 0),
            ["officeHours"] = (NaturalCronTimeUnit.Hour, d => d.Hour is >= 9 and <= 17),
            ["nightShift"] = (NaturalCronTimeUnit.Hour, d => d.Hour is >= 22 or < 6),
            ["lunchtime"] = (NaturalCronTimeUnit.Hour, d => d.Hour == 12),

            // Days
            ["primeDay"] = (NaturalCronTimeUnit.Day, d => IsPrime(d.Day)),
            ["evenDay"] = (NaturalCronTimeUnit.Day, d => d.Day % 2 == 0),
            ["oddDay"] = (NaturalCronTimeUnit.Day, d => d.Day % 2 == 1),
            ["fibonacciDay"] = (NaturalCronTimeUnit.Day, d => FibonacciDays.Contains(d.Day)),
            ["twinDay"] = (NaturalCronTimeUnit.Day, d => d.Day is 11 or 22),
            ["holiday"] = (NaturalCronTimeUnit.Day, IsHoliday),
            ["weekend"] = (NaturalCronTimeUnit.Day, IsWeekend),
            ["businessDay"] = (NaturalCronTimeUnit.Day, IsBusinessDay),
            ["lastBusinessDay"] = (NaturalCronTimeUnit.Day, IsLastBusinessDay),
            ["fridayThe13th"] = (NaturalCronTimeUnit.Day, d => d.Day == 13 && d.DayOfWeek == DayOfWeek.Friday),
            ["firstDayOfQuarter"] = (NaturalCronTimeUnit.Day, d => d.Day == 1 && d.Month is 1 or 4 or 7 or 10),
            ["lastDayOfQuarter"] = (NaturalCronTimeUnit.Day, d => IsLastDayOfMonth(d) && d.Month is 3 or 6 or 9 or 12),
            ["payday"] = (NaturalCronTimeUnit.Day, d => d.Day == 15 || IsLastDayOfMonth(d)),
            ["leapDay"] = (NaturalCronTimeUnit.Day, d => d.Month == 2 && d.Day == 29),

            // Days of the week
            ["midweek"] = (NaturalCronTimeUnit.Week, d => d.DayOfWeek is DayOfWeek.Tuesday or DayOfWeek.Wednesday or DayOfWeek.Thursday),

            // Months
            ["quarter"] = (NaturalCronTimeUnit.Month, d => d.Month is 1 or 4 or 7 or 10),
            ["summer"] = (NaturalCronTimeUnit.Month, d => d.Month is 6 or 7 or 8),
            ["primeMonth"] = (NaturalCronTimeUnit.Month, d => IsPrime(d.Month)),
            ["q1"] = (NaturalCronTimeUnit.Month, d => d.Month is >= 1 and <= 3),
            ["q2"] = (NaturalCronTimeUnit.Month, d => d.Month is >= 4 and <= 6),
            ["q3"] = (NaturalCronTimeUnit.Month, d => d.Month is >= 7 and <= 9),
            ["q4"] = (NaturalCronTimeUnit.Month, d => d.Month is >= 10 and <= 12),
            ["h2"] = (NaturalCronTimeUnit.Month, d => d.Month >= 7),

            // Years
            ["leapYear"] = (NaturalCronTimeUnit.Year, d => DateTime.IsLeapYear(d.Year)),
        };

    /// <summary>
    /// Registers the keywords, going through every kind of AddMatchable overload.
    /// </summary>
    public static void Configure(NaturalCronCustomKeywordsBuilder cfg)
    {
        foreach (var keyword in Keywords)
        {
            var (timeUnit, matches) = keyword.Value;
            switch (keyword.Key)
            {
                case "holiday":
                    // Two keywords for the same condition.
                    cfg.AddMatchable(new[] { "holiday", "holidays" }, matches, timeUnit);
                    break;
                case "primeDay":
                    cfg.AddMatchable<PrimeDayKeyword>(keyword.Key);
                    break;
                case "fibonacciDay":
                    cfg.AddMatchable(keyword.Key, () => new ConditionKeyword(timeUnit, matches));
                    break;
                default:
                    cfg.AddMatchable(keyword.Key, matches, timeUnit);
                    break;
            }
        }

        cfg.AddMatchable("nyMarketOpen", IsNewYorkMarketOpen, NaturalCronTimeUnit.Hour);
    }

    private sealed class PrimeDayKeyword : INaturalCronMatchableCustomKeyword
    {
        public NaturalCronTimeUnit TimeUnit => NaturalCronTimeUnit.Day;
        public bool Match(DateTime dateTimeInTz, TimeZoneInfo timeZone) => IsPrime(dateTimeInTz.Day);
    }

    private sealed class ConditionKeyword : INaturalCronMatchableCustomKeyword
    {
        private readonly Func<DateTime, bool> matches;

        public ConditionKeyword(NaturalCronTimeUnit timeUnit, Func<DateTime, bool> matches)
        {
            TimeUnit = timeUnit;
            this.matches = matches;
        }

        public NaturalCronTimeUnit TimeUnit { get; }
        public bool Match(DateTime dateTimeInTz, TimeZoneInfo timeZone) => matches(dateTimeInTz);
    }
}
