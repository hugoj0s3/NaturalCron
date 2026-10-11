# API Reference

This document provides a comprehensive reference for the NaturalCron library's public API.

```csharp
// Parse a NaturalCron expression
var compiledExpr = NaturalCronExpr.Parse("daily at 14:30");
```

## Quick Navigation
- [Parse Methods](#parse-methods)
- [Properties](#properties)
- [Local Time Occurrence Methods](#local-time-occurrence-methods)
- [UTC Occurrence Methods](#utc-occurrence-methods)
- [IANA Timezone Occurrence Methods](#iana-timezone-occurrence-methods)
- [Timezone Behavior](#timezone-behavior)
- [Usage Examples](#usage-examples)
- [Rule Filter Methods](#rule-filter-methods)
- [Custom Keywords](#custom-keywords)


## Parse Methods

### Parse()
```csharp
public static NaturalCronExpr Parse(string expression)
```
Parses a NaturalCron expression string into a `NaturalCronExpr` object.

**Parameters:**
- `expression`: The NaturalCron expression string to parse

**Returns:** A `NaturalCronExpr` object representing the parsed expression

**Throws:** `ArgumentException` if the expression is invalid

### TryParse()
```csharp
public static (NaturalCronExpr?, IList<string> errors) TryParse(string expression)
```
Attempts to parse a NaturalCron expression string. Returns the parsed expression and any errors encountered.

**Parameters:**
- `expression`: The NaturalCron expression string to parse

**Returns:** A tuple containing:
- `NaturalCronExpr?`: The parsed expression object, or null if parsing failed
- `IList<string> errors`: A list of error messages (empty if parsing succeeded)


## Properties

### Expression
```csharp
public string Expression { get; }
```
The original expression string used to create this object.

### Rules
```csharp
public IReadOnlyCollection<NaturalCronRule> Rules { get; }
```
The parsed rules that define the recurrence.


## Local Time Occurrence Methods
These methods work with the current thread's timezone and return results in the local timezone.

### TryGetNextOccurrence()
```csharp
public DateTime? TryGetNextOccurrence(DateTime baseTime, DateTime? maxLookahead = null)
```
Tries to get the next occurrence using the current thread's timezone. Returns null if no occurrence is found within the specified lookahead period.

**Parameters:**
- `baseTime`: The base time in the current thread's timezone from which to find the next occurrence
- `maxLookahead`: Maximum lookahead time in the current thread's timezone. If null, defaults to DateTime.MaxValue

**Returns:** The next occurrence in the current thread's timezone, or null if no occurrence is found

### GetNextOccurrence()
```csharp
public DateTime GetNextOccurrence(DateTime baseTime, DateTime? maxLookahead = null)
```
Gets the next occurrence using the current thread's timezone. Throws an exception if no occurrence is found within the specified lookahead period.

**Parameters:**
- `baseTime`: The base time in the current thread's timezone from which to find the next occurrence
- `maxLookahead`: Maximum lookahead time in the current thread's timezone. If null, defaults to DateTime.MaxValue

**Returns:** The next occurrence in the current thread's timezone

**Throws:** `InvalidOperationException` when no next occurrence is found

### TryGetNextOccurrences()
```csharp
public IList<DateTime> TryGetNextOccurrences(DateTime baseTime, int count, DateTime? maxLookahead = null)
```
Tries to get the next occurrences using the current thread's timezone. Returns as many occurrences as found, up to the specified count. Stops when no more occurrences are found or the count is reached.

**Parameters:**
- `baseTime`: The base time in the current thread's timezone from which to find the next occurrences
- `count`: The number of occurrences to retrieve
- `maxLookahead`: Maximum lookahead time in the current thread's timezone. If null, defaults to DateTime.MaxValue

**Returns:** A list of occurrences in the current thread's timezone (may contain fewer than the requested count)

### GetNextOccurrences()
```csharp
public IList<DateTime> GetNextOccurrences(DateTime baseTime, int count, DateTime? maxLookahead = null)
```
Gets the next occurrences using the current thread's timezone. Throws an exception if fewer occurrences than requested are found. Use `TryGetNextOccurrences` if you are not sure there are enough occurrences available.

**Parameters:**
- `baseTime`: The base time in the current thread's timezone from which to find the next occurrences
- `count`: The number of occurrences to retrieve
- `maxLookahead`: Maximum lookahead time in the current thread's timezone. If null, defaults to DateTime.MaxValue

**Returns:** A list of occurrences in the current thread's timezone

**Throws:** `InvalidOperationException` when fewer than the requested number of occurrences are found


## UTC Occurrence Methods
These methods work with UTC times and return UTC results.

### TryGetNextOccurrenceInUtc()
```csharp
public DateTime? TryGetNextOccurrenceInUtc(DateTime utcBaseTime, DateTime? utcMaxLookahead = null)
```
Tries to get the next occurrence in UTC. Returns null if no occurrence is found within the specified lookahead period.

**Parameters:**
- `utcBaseTime`: The base time in UTC from which to find the next occurrence
- `utcMaxLookahead`: Maximum lookahead time in UTC. If null, defaults to DateTime.MaxValue

**Returns:** The next occurrence in UTC, or null if no occurrence is found

### GetNextOccurrenceInUtc()
```csharp
public DateTime GetNextOccurrenceInUtc(DateTime utcBaseTime, DateTime? utcMaxLookahead = null)
```
Gets the next occurrence in UTC. Throws an exception if no occurrence is found within the specified lookahead period.

**Parameters:**
- `utcBaseTime`: The base time in UTC from which to find the next occurrence
- `utcMaxLookahead`: Maximum lookahead time in UTC. If null, defaults to DateTime.MaxValue

**Returns:** The next occurrence in UTC

**Throws:** `InvalidOperationException` when no next occurrence is found

### TryGetNextOccurrencesInUtc()
```csharp
public IList<DateTime> TryGetNextOccurrencesInUtc(DateTime utcBaseTime, int count, DateTime? utcMaxLookahead = null)
```
Tries to get the next occurrences in UTC. Returns as many occurrences as found, up to the specified count. Stops when no more occurrences are found or the count is reached.

**Parameters:**
- `utcBaseTime`: The base time in UTC from which to find the next occurrences
- `count`: The number of occurrences to retrieve
- `utcMaxLookahead`: Maximum lookahead time in UTC. If null, defaults to DateTime.MaxValue

**Returns:** A list of occurrences in UTC (may contain fewer than the requested count)

### GetNextOccurrencesInUtc()
```csharp
public IList<DateTime> GetNextOccurrencesInUtc(DateTime utcBaseTime, int count, DateTime? utcMaxLookahead = null)
```
Gets the next occurrences in UTC. Throws an exception if fewer occurrences than requested are found. Use `TryGetNextOccurrencesInUtc` if you are not sure there are enough occurrences available.

**Parameters:**
- `utcBaseTime`: The base time in UTC from which to find the next occurrences
- `count`: The number of occurrences to retrieve
- `utcMaxLookahead`: Maximum lookahead time in UTC. If null, defaults to DateTime.MaxValue

**Returns:** A list of occurrences in UTC

**Throws:** `InvalidOperationException` when fewer than the requested number of occurrences are found


## IANA Timezone Occurrence Methods
These methods work with a specified IANA timezone ID and return results in that timezone. The timezone ID is case-insensitive (e.g., "America/New_York" or "america/new_york").

### TryGetNextOccurrenceInTz()
```csharp
public DateTime? TryGetNextOccurrenceInTz(DateTime baseTimeInTz, string ianaTzId, DateTime? maxLookaheadInTz = null)
```
Tries to get the next occurrence in a specified IANA timezone. Returns null if no occurrence is found within the specified lookahead period.

**Parameters:**
- `baseTimeInTz`: The base time interpreted in the specified IANA timezone from which to find the next occurrence
- `ianaTzId`: IANA timezone ID (case-insensitive, e.g., "America/New_York" or "europe/lisbon")
- `maxLookaheadInTz`: Maximum lookahead time in the specified timezone. If null, defaults to DateTime.MaxValue

**Returns:** The next occurrence in the specified timezone, or null if no occurrence is found

**Throws:** `ArgumentException` when the IANA timezone ID is invalid or not recognized

### GetNextOccurrenceInTz()
```csharp
public DateTime GetNextOccurrenceInTz(DateTime baseTimeInTz, string ianaTzId, DateTime? maxLookaheadInTz = null)
```
Gets the next occurrence in a specified IANA timezone. Throws an exception if no occurrence is found within the specified lookahead period.

**Parameters:**
- `baseTimeInTz`: The base time interpreted in the specified IANA timezone from which to find the next occurrence
- `ianaTzId`: IANA timezone ID (case-insensitive, e.g., "America/New_York" or "europe/lisbon")
- `maxLookaheadInTz`: Maximum lookahead time in the specified timezone. If null, defaults to DateTime.MaxValue

**Returns:** The next occurrence in the specified timezone

**Throws:** 
- `ArgumentException` when the IANA timezone ID is invalid or not recognized
- `InvalidOperationException` when no next occurrence is found

### TryGetNextOccurrencesInTz()
```csharp
public IList<DateTime> TryGetNextOccurrencesInTz(DateTime baseTimeInTz, int count, string ianaTzId, DateTime? maxLookaheadInTz = null)
```
Tries to get the next occurrences in a specified IANA timezone. Returns as many occurrences as found, up to the specified count. Stops when no more occurrences are found or the count is reached.

**Parameters:**
- `baseTimeInTz`: The base time interpreted in the specified IANA timezone from which to find the next occurrences
- `count`: The number of occurrences to retrieve
- `ianaTzId`: IANA timezone ID (case-insensitive, e.g., "America/New_York" or "europe/lisbon")
- `maxLookaheadInTz`: Maximum lookahead time in the specified timezone. If null, defaults to DateTime.MaxValue

**Returns:** A list of occurrences in the specified timezone (may contain fewer than the requested count)

**Throws:** `ArgumentException` when the IANA timezone ID is invalid or not recognized

### GetNextOccurrencesInTz()
```csharp
public IList<DateTime> GetNextOccurrencesInTz(DateTime baseTimeInTz, int count, string ianaTzId, DateTime? maxLookaheadInTz = null)
```
Gets the next occurrences in a specified IANA timezone. Throws an exception if fewer occurrences than requested are found. Use `TryGetNextOccurrencesInTz` if you are not sure there are enough occurrences available.

**Parameters:**
- `baseTimeInTz`: The base time interpreted in the specified IANA timezone from which to find the next occurrences
- `count`: The number of occurrences to retrieve
- `ianaTzId`: IANA timezone ID (case-insensitive, e.g., "America/New_York" or "europe/lisbon")
- `maxLookaheadInTz`: Maximum lookahead time in the specified timezone. If null, defaults to DateTime.MaxValue

**Returns:** A list of occurrences in the specified timezone

**Throws:** 
- `ArgumentException` when the IANA timezone ID is invalid or not recognized
- `InvalidOperationException` when fewer than the requested number of occurrences are found


## Timezone Behavior
Understanding how timezones work in NaturalCron is crucial for correct usage:

### Expression Timezone vs Return Timezone

1. **Expression timezone (`tz Asia/Tokyo`)**: Specifies the timezone for **calculation** - when the recurrence rules are evaluated
2. **Return value timezone**: Always matches the method used:
   - UTC methods (`GetNextOccurrenceInUtc`) → return UTC time
   - Local methods (`GetNextOccurrence`) → return system/local time
   - IANA timezone methods (`GetNextOccurrenceInTz`) → return in the specified IANA timezone

### Example

For expression `"daily at 09:00 tz Asia/Tokyo"`:
- Calculates when 9:00 AM Tokyo time occurs
- If using `GetNextOccurrence()`, calculates using Tokyo timezone and returns in the current thread timezone
- If using `GetNextOccurrenceInUtc()`, calculates using Tokyo timezone and returns in UTC
- If using `GetNextOccurrenceInTz(baseTime, "America/New_York")`, calculates using Tokyo timezone and returns in New York timezone
- If no timezone in the expression, the calculation uses the input time's timezone context
- The input parameter time is interpreted in:
  - Current thread timezone for local methods
  - UTC for UTC methods
  - The specified IANA timezone for IANA timezone methods

The `tz` keyword affects the **calculation** timezone, not the **return** timezone.


## Usage Examples

### Basic Usage
```csharp
// Parse an expression
var expr = NaturalCronExpr.Parse("daily at 14:30");

// Get next occurrence in local time
var next = expr.GetNextOccurrence(DateTime.Now);

// Get next occurrence in UTC
var nextUtc = expr.GetNextOccurrenceInUtc(DateTime.UtcNow);
```

### Safe Parsing
```csharp
var (expr, errors) = NaturalCronExpr.TryParse("daily at 14:30");
if (expr != null)
{
    var next = expr.GetNextOccurrence(DateTime.Now);
}
else
{
    Console.WriteLine($"Parse errors: {string.Join(", ", errors)}");
}
```

### Multiple Occurrences
```csharp
var expr = NaturalCronExpr.Parse("every monday at 09:00");

// Get next 5 occurrences
var occurrences = expr.GetNextOccurrences(DateTime.Now, 5);

// Try to get next 10 occurrences (may return fewer)
var moreOccurrences = expr.TryGetNextOccurrences(DateTime.Now, 10);
```

### Working with Timezones
```csharp
var expr = NaturalCronExpr.Parse("daily at 09:00 tz America/New_York");

// This calculates 9 AM New York time and returns it in local system time
var nextLocal = expr.GetNextOccurrence(DateTime.Now);

// This calculates 9 AM New York time and returns it in UTC
var nextUtc = expr.GetNextOccurrenceInUtc(DateTime.UtcNow);
```

### Working with Specific IANA Timezones
```csharp
var expr = NaturalCronExpr.Parse("daily at 14:30");

// Get next occurrence in Tokyo time (input and output in Tokyo timezone)
var tokyoTime = new DateTime(2024, 1, 1, 10, 0, 0);
var nextInTokyo = expr.GetNextOccurrenceInTz(tokyoTime, "Asia/Tokyo");

// Get next 5 occurrences in London time
var londonTime = new DateTime(2024, 1, 1, 10, 0, 0);
var occurrencesInLondon = expr.GetNextOccurrencesInTz(londonTime, 5, "Europe/London");

// Case-insensitive timezone ID
var nextInNY = expr.GetNextOccurrenceInTz(DateTime.Now, "america/new_york");

// Safe usage with Try methods
var result = expr.TryGetNextOccurrenceInTz(DateTime.Now, "America/Los_Angeles");
if (result.HasValue)
{
    Console.WriteLine($"Next occurrence: {result.Value}");
}
```

### Expression Timezone + Different Return Timezone
```csharp
// Expression calculates in Tokyo time, but returns in New York time
var expr = NaturalCronExpr.Parse("daily at 09:00 tz Asia/Tokyo");
var nyTime = new DateTime(2024, 1, 1, 20, 0, 0); // 8 PM in New York
var nextInNY = expr.GetNextOccurrenceInTz(nyTime, "America/New_York");
// Returns when 9 AM Tokyo occurs, but expressed in New York timezone
```

## Rule Filter Methods
These methods return subsets of rules filtered by time unit:

### SecondRules()
```csharp
public IReadOnlyCollection<NaturalCronRule> SecondRules()
```
Returns all rules that operate on seconds.

### MinuteRules()
```csharp
public IReadOnlyCollection<NaturalCronRule> MinuteRules()
```
Returns all rules that operate on minutes.

### HourRules()
```csharp
public IReadOnlyCollection<NaturalCronRule> HourRules()
```
Returns all rules that operate on hours.

### DayRules()
```csharp
public IReadOnlyCollection<NaturalCronRule> DayRules()
```
Returns all rules that operate on days.

### WeekRules()
```csharp
public IReadOnlyCollection<NaturalCronRule> WeekRules()
```
Returns all rules that operate on weeks.

### MonthRules()
```csharp
public IReadOnlyCollection<NaturalCronRule> MonthRules()
```
Returns all rules that operate on months.

### YearRules()
```csharp
public IReadOnlyCollection<NaturalCronRule> YearRules()
```
Returns all rules that operate on years.

### TimeZoneRule()
```csharp
public NaturalCronIanaTimeZoneRule? TimeZoneRule()
```
Returns the timezone rule if one is defined in the expression, otherwise null.

## Custom Keywords
Custom keywords let expressions reference your own rules, e.g. `every day at 09:00 except holiday`.
See [Custom Keywords in the Expression Syntax](expression-syntax.md#custom-keywords) for the syntax.

### Configure()
```csharp
public static void NaturalCronCustomKeywords.Configure(Action<NaturalCronCustomKeywordsBuilder> configure)
```
Configures all custom keywords at once, typically at application startup and **before** any expression using them is parsed.
- It can only be called **once**; a second call throws `InvalidOperationException`. There is no unregister.
- All keywords are validated together (format, duplicates, collisions with built-in words). If any is invalid an
  `ArgumentException` lists the problems and nothing is applied.

```csharp
NaturalCronCustomKeywords.Configure(cfg =>
{
    // Inline function on the wall-clock time. Time unit defaults to Day.
    cfg.AddMatchable("payday", dt => dt.Day == 15);

    // Inline function that also needs the time zone.
    cfg.AddMatchable("payday2", (dt, tz) => dt.Day == 15);

    // Class implementing INaturalCronMatchableCustomKeyword, created with its parameterless constructor.
    cfg.AddMatchable<MaintenanceWindowKeyword>("maintenanceWindow");

    // Factory, e.g. to resolve the keyword from your IoC container.
    cfg.AddMatchable("holiday", () => serviceProvider.GetRequiredService<HolidayKeyword>());

    // Several keywords for the same implementation. Every overload accepts a list.
    cfg.AddMatchable(new[] { "weekend", "weekends" }, dt => dt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
});
```
A keyword can only be registered once: registering the same keyword twice (ignoring case), or a built-in word, makes
`Configure` throw and nothing is applied.

### INaturalCronMatchableCustomKeyword
```csharp
public interface INaturalCronMatchableCustomKeyword
{
    NaturalCronTimeUnit TimeUnit { get; }
    bool Match(DateTime dateTimeInTz, TimeZoneInfo timeZone);
}
```
A matchable keyword says whether a time matches. It is registered with `AddMatchable` and can be used with `on`, `at`, `in`
and `except`. It cannot be used as a boundary of `between`, `upto` or `from`.

- `TimeUnit` is the keyword granularity (e.g. `Day` for holidays, `Hour` for an hourly window). It controls how far the engine
  safely advances when the keyword doesn't match. `TimeZone` is not allowed.
- `Match` receives the wall-clock time (`Kind = Unspecified`) and the time zone it is in:

  | Method | Expression without `tz` | Expression with `tz X` |
  |---|---|---|
  | `GetNextOccurrence(local)` | `TimeZoneInfo.Local` (`TimeZoneInfo.Utc` if `Kind == Utc`) | X |
  | `GetNextOccurrenceInUtc(utc)` | `TimeZoneInfo.Utc` | X |
  | `GetNextOccurrenceInTz(dt, id)` | `id` | X (the expression's tz always wins) |

- A keyword instance is created **once per parsed expression** (at parse time) and reused for all its matches.
  A factory must work wherever expressions are parsed (including Quartz triggers).

> ⚠️ **Performance:** `Match` is called on every step of the occurrence search, potentially many times per
> `GetNextOccurrence` call. Keep it fast and non-blocking: load or cache your data up front (e.g. a `HashSet` of holiday
> dates refreshed periodically, or an injected singleton cache) instead of querying a database or HTTP endpoint inside `Match`.

```csharp
public class HolidayKeyword : INaturalCronMatchableCustomKeyword
{
    private readonly IHolidayCache cache; // e.g. a singleton holding a HashSet<DateTime> refreshed periodically

    public HolidayKeyword(IHolidayCache cache) => this.cache = cache;

    public NaturalCronTimeUnit TimeUnit => NaturalCronTimeUnit.Day;

    public bool Match(DateTime dateTimeInTz, TimeZoneInfo timeZone) => cache.IsHoliday(dateTimeInTz.Date);
}
```

### NaturalCronCustomKeywordRule, NaturalCronAnyOfRule and NaturalCronExceptRule
Parsed custom keyword rules appear in `Rules` as `NaturalCronCustomKeywordRule` (`Spec == NaturalCronRuleSpec.Custom`),
exposing `Keywords`. A list mixing keywords and built-in values (`on [holiday, 15th]`) appears as `NaturalCronAnyOfRule`
(`Spec == NaturalCronRuleSpec.AtInOn`), whose `Rules` holds the keyword rule and the built-in rule.
Exclusions (`except`) appear as `NaturalCronExceptRule` (`Spec == NaturalCronRuleSpec.Except`),
whose `Rules` holds the excluded rules, e.g. a `NaturalCronCustomKeywordRule` or `NaturalCronAtInOnRule`.

✅ [Back to Main README](../README.md)