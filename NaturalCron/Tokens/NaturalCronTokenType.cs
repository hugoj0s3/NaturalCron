namespace NaturalCron.Tokens;

public enum NaturalCronTokenType
{
    // every, between, at, on, in, tz, timezone
    RuleSpec = 1,
    
    Anchored,

    // Whole number
    WholeNumber,

    Comma,
    Colon,
    DashOrMinus,
    Plus,
    
    OpenBrackets,
    CloseBrackets,

    // Iana Timezone
    IanaTimeZoneId,

    // Sec, Second, Seconds
    // Min, Minute, Minutes
    // Hr, Hour, Hours
    // Day, Days
    // Wk, Week, Weeks
    // Mth, Month, Months
    // Yr, Year, Years
    TimeUnit,

    // Mon, Tue, Wed, Thu, Fri, Sat, Sun
    // Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday
    WeekdayWord,

    // Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec
    // January, February, March, April, May, June, July, August, September, October, November, December
    MonthWord,
    
    FirstDayOfTheMonth,

    // Last day of the month or last day of the week
    LastDayOfTheMonth,
    
    RelativeWeekdays,
    
    
    // FirstMon ...
    NthWeekDays,

    // And, &&
    And,

    // Last
    Last,

    // First
    First,
    
    WhiteSpace,
    
    AmPm,
    
    EndOfExpression,

    // User-defined keyword configured via NaturalCronCustomKeywords
    CustomKeyword,

    // ; . , will be ignored
    IgnoredToken = 98,

    // any Unknown token
    Unknown = 99,
}