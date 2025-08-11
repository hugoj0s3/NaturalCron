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
- [Timezone Behavior](#timezone-behavior)
- [Usage Examples](#usage-examples)
- [Rule Filter Methods](#rule-filter-methods)


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


## Timezone Behavior
Understanding how timezones work in NaturalCron is crucial for correct usage:

### Expression Timezone vs Return Timezone

1. **Expression timezone (`tz Asia/Tokyo`)**: Specifies the timezone for **calculation** - when the recurrence rules are evaluated
2. **Return value timezone**: Always matches the method used:
   - UTC methods (`GetNextOccurrenceInUtc`) → return UTC time
   - Local methods (`GetNextOccurrence`) → return system/local time

### Example

For expression `"daily at 09:00 tz Asia/Tokyo"`:
- Calculates when 9:00 AM Tokyo time occurs
- If using `GetNextOccurrence()`, calculates using Tokyo time zone and returns in the current thread timezone.
- If using `GetNextOccurrenceInUtc()`, calculates using Tokyo time zone and returns in UTC.
- If no timezone in the expression, returns in the current thread timezone or in UTC if UTC methods are used.
- The input parameter time is on current thread time zone when using non-UTC methods and UTC when using UTC methods

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

✅ [Back to Main README](../README.md)