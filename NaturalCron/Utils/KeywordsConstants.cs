using TimeZoneConverter;

namespace NaturalCron.Utils;

internal static class KeywordsConstants
{
    // Keep the support for rules with ampersat.
    internal static readonly string[] RuleSpecWithAmpersat =
    {
        "@AT", "@ON", "@IN", 
        "@EVERY", "@YEARLY", "@MONTHLY", "@WEEKLY", "@DAILY", "@HOURLY", "@MINUTELY", "@SECONDLY", 
        "@BETWEEN", "@UPTO", "@FROM",
        "@TimeZone", "@TZ",
        "@EXCEPT"
    };
    
    internal static readonly string[] RuleSpec = RuleSpecWithAmpersat
        .Select(x => x.Replace("@", ""))
        .Concat(RuleSpecWithAmpersat)
        .ToArray();

    internal static readonly string[] Anchored =
    [
        "BasedOn", 
        "BasedAt", 
        "BasedIn",
        "AnchoredOn",
        "AnchoredAt",
        "AnchoredIn",
    ];

    internal static readonly string[] SecondsTimeUnitWords =
    [
        "Sec", "Second", "Seconds", "Secs"
    ];
    
    internal static readonly string[] MinutesTimeUnitWords =
    [
        "Min", "Minute", "Minutes", "Mins"
    ];
    
    internal static readonly string[] HoursTimeUnitWords =
    [
        "HR", "HOUR", "HOURS", "Hrs"
    ];

    internal static readonly string[] DayOrdinals =
    [
        "ST", "ND", "RD", "TH",
    ];
    
    internal static readonly string[] DaysTimeUnitWords = new List<string>(DayOrdinals).Concat(["DAY", "DAYS", "DY", "Dys"]).ToArray();
    
    internal static readonly string[] WeeksTimeUnitWords =
    [
        "WK", "WEEK", "WEEKS", "Wks"
    ];

    internal static readonly string[] MonthTimeUnitWords =
    [
        "MTH", "MONTH", "MONTHS", "Mths"
    ];
    internal static readonly string[] YearsTimeUnitWords =
    [
        "YR", "YEAR", "YEARS", "Yrs"
    ];

    internal static readonly string[] TimeUnitWords = SecondsTimeUnitWords
        .Concat(MinutesTimeUnitWords)
        .Concat(HoursTimeUnitWords)
        .Concat(DaysTimeUnitWords)
        .Concat(WeeksTimeUnitWords)
        .Concat(MonthTimeUnitWords)
        .Concat(YearsTimeUnitWords)
        .ToArray();

    internal static readonly string[] WeekdayWords =
    [
        "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT", 
        "SUNDAY", "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY"
    ];

    internal static readonly string[] MonthWords =
    [
        "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC", 
        "JANUARY", "FEBRUARY", "MARCH", "APRIL", "JUNE", "JULY", "AUGUST", "SEPTEMBER", "OCTOBER", "NOVEMBER", "DECEMBER"
    ];

    internal static readonly string[] LastDayOfTheMonth =
    [
        "LastDayOfTheMonth", "LastDayOfMonth", "LastDay"
    ];
    
    internal static readonly string[] FirstDayOfTheMonth =
    [
        "FirstOfTheMonth", "FirstDayOfMonth", "FirstDay"
    ];

    internal static readonly string[] LastNthWeekDays =
    [
        "LastMon", "LastTue", "LastWed", "LastThu", "LastFri", "LastSat", "LastSun", "LastMonday", "LastTuesday", "LastWednesday",
        "LastThursday", "LastFriday", "LastSaturday", "LastSunday"
    ];
    
    internal static readonly string[] NthWeekOrdinals = new[]
    {
        "First", "1st",
        "Second", "2nd",
        "Third", "3rd",
        "Fourth", "4th",
        "Fifth", "5th",
        "Last"
    };
    
    internal static readonly string[] NthWeekDays = NthWeekOrdinals
            .SelectMany(ordinal => WeekdayWords, (ordinal, weekday) => ordinal + weekday)
            .ToArray();
    
    internal static readonly string[] RelativeWeekdays =
    [
        "LastWeekday",
        "FirstWeekday",
        "ClosestWeekdayTo",
    ];


    internal static readonly string[] IanaTimeZones = TZConvert.KnownIanaTimeZoneNames.Select(x => x.ToUpper()).ToArray();

    internal static readonly char[] Breaks = new[]
    {
        ' ', ';', ',', ':', '-', '.', '\n', '\r', '\t', '[', ']', '(', ')', '{', '}', '%', '$', '#', '^', '=', '!', '<', '>',
        '?', '|', '\\', '"', '\'', '+', '\0'
    };

    internal static readonly char[] IgnoredChars = new[]
    {
        ';', '\t', '\n', '\r'
    };
    
    internal static readonly string[] Last = ["Last"];
    internal static readonly string[] First = ["First"];
    
    internal static readonly string[] PmOrAm = ["PM", "AM"];
    
    internal static readonly string[] And = ["AND"];
}