# FluentBuilder

The **NaturalCron Builder** provides a **fluent, strongly-typed API** for constructing NaturalCron expressions programmatically in C#.  
Its primary goals are:

- **Type Safety** – Prevent invalid combinations with compile-time checks
- **IntelliSense Support** – Full IDE support with hints and completion
- **Dynamic Generation** – Perfect for scenarios where schedules are created at runtime
- **Validation** – Ensures valid structure before building
- **Readability** – Outputs the same human-friendly syntax as raw expressions

> **Note**: The Builder complements **raw strings**. Raw syntax is great for static cases (e.g., configuration files).  
> The Builder shines in dynamic, scenarios where **type safety** matter.

---

## Quick Start

```csharp
using NaturalCron.Builder;

// Simple daily schedule at 09:30
var expr = NaturalCronBuilder.Start()
    .Daily()
    .AtTime(9, 30)
    .Build(); // => Every Day At 09:30

// Every 2 weeks on Monday
var expr = NaturalCronBuilder.Start()
    .Every(2)
    .Weeks()
    .AnchoredOn(DayOfWeek.Monday)
    .Build(); // => Every 2 Weeks AnchoredOn Monday

```

## Core Concepts

### Build() and ToRawExpression()

The builder provides two output methods with **important differences**:

```csharp
var builder = NaturalCronBuilder.Start()
    .Every(2)
    .Weeks();

// Build() - Validates and returns NaturalCronExpr
var expr = builder.Build(); // Parses and validates the expression
var nextRun = expr.GetNextOccurrence(DateTime.Now);

// ToRawExpression() - Returns raw string without validation  
var rawString = builder.ToRawExpression(); // "Every 2 Weeks" - No validation
Console.WriteLine($"Expression: {rawString}");
```

**Key Differences:**

| Method | Validation | Return Type | Use Case           |
|--------|------------|-------------|--------------------|
| `Build()` | Validates expression and throws exception if parsing fails | `NaturalCronExpr` | Runtime use |
| `ToRawExpression()` | No validation - returns raw string | `string` | Debugging, logging |

> Important: The builder is **not a silver bullet** - it can construct **invalid expressions**.

**Example of invalid expression:**
```csharp
// This will generate invalid syntax but ToRawExpression() won't catch it
var invalid = NaturalCronBuilder.Start()
    .At("WRONG-RAW-STRING")  // Invalid time
    .ToRawExpression(); // Returns: "At WRONG-RAW-STRING" (invalid!)

// Build() will catch the error
try 
{
    var expr = NaturalCronBuilder.Start()
        .At("WRONG-RAW-STRING")
        .Build(); // Throws parsing exception
}
catch (Exception ex)
{
    Console.WriteLine($"Invalid expression: {ex.Message}");
}
```

### Base Expressions
All builder chains must start with a base expression that defines the fundamental schedule:

- **Time Units**: `Daily()`, `Weekly()`, `Monthly()`, `Yearly()`, `Hourly()`, `Minutely()`, `Secondly()`
- **Intervals**: `Every().Day()`, `Every(2).Weeks()`, `Every(3).Months()`
- **Specific Times**: `At("12:00")`, `AtTime(14, 30)`
- **Specific Days**: `On(DayOfWeek.Monday)`, `On("Friday")`

### Modifiers
After establishing a base expression, you can add modifiers:

- **Time Ranges**: `Between()`, `From()`, `Upto()`
- **Anchoring**: `AnchoredOn()`, `AnchoredIn()`, `AnchoredAt()`
- **Timezone**: `WithTimeZone()`

## API Reference

### Static Entry Points

```csharp
// Direct time units
NaturalCronBuilder.Daily()
NaturalCronBuilder.Weekly()
NaturalCronBuilder.Monthly()
NaturalCronBuilder.Yearly()
NaturalCronBuilder.Hourly()
NaturalCronBuilder.Minutely()
NaturalCronBuilder.Secondly()

// Interval-based
NaturalCronBuilder.Every()           // Every single unit
NaturalCronBuilder.Every(5)         // Every 5 units

// Specific targeting
NaturalCronBuilder.On("Monday")      // Specific day
NaturalCronBuilder.In("January")     // Specific month
NaturalCronBuilder.At("12:00")       // Specific time
NaturalCronBuilder.AtDay(15)         // Specific day of month
NaturalCronBuilder.AtTime(14, 30)    // Specific time (hour, minute)
```

### Configuration Options

```csharp
// Formatting options
NaturalCronBuilder.UseAmpersat()     // Prefix with @
NaturalCronBuilder.UseWeekFullName() // "Monday" instead of "Mon"
NaturalCronBuilder.UseMonthFullName() // "January" instead of "Jan"

// Combine configurations
var expr = NaturalCronBuilder.Start()
    .UseAmpersatPrefix()
    .UseWeekFullName()
    .UseMonthFullName()
    .Daily()
    .Build();
// Result: "@Daily"
```

### Time Methods

```csharp
// Basic time
.AtTime(14, 30)                      // At 14:30
.AtTime(2, 15, 30)                   // At 02:15:30
.AtTime(9, 0, amOrPm: NaturalCronAmOrPm.Am) // At 09:00am

// Time ranges
.BetweenTime(9, 0, 17, 30)           // Between 09:00 and 17:30
.FromTime(8, 0)                      // From 08:00
.UptoTime(18, 0)                     // Until 18:00

// TimeOnly support (.NET 6+)
.At(new TimeOnly(14, 30))
.Between(new TimeOnly(9, 0), new TimeOnly(17, 30))
```

### Day and Date Methods

```csharp
// Day positions
.AtDayPosition(NaturalCronDayPosition.FirstDay)
.AtDayPosition(NaturalCronDayPosition.LastWeekday)

// Nth weekday
.OnNthWeekday(NaturalCronNthWeekDay.First, NaturalCronDayOfWeek.Mon)  // 1st Monday
.OnNthWeekday(NaturalCronNthWeekDay.Last, NaturalCronDayOfWeek.Fri)   // Last Friday

// Closest weekday
.OnClosestWeekdayTo(15)              // Closest weekday to 15th

// Month and day combinations
.At(NaturalCronMonth.Dec, 25)       // December 25th
.From(NaturalCronMonth.Jan, 1)      // From January 1st
.Between(NaturalCronMonth.Jun, 1, NaturalCronMonth.Aug, 31) // June 1st to August 31st
```

### Range Methods

```csharp
// Day of week ranges
.Between(DayOfWeek.Monday, DayOfWeek.Friday)
.From(DayOfWeek.Monday)
.Upto(DayOfWeek.Friday)

// Month ranges
.Between(NaturalCronMonth.Mar, NaturalCronMonth.Nov)
.From(NaturalCronMonth.Jun)
.Upto(NaturalCronMonth.Sep)

// Day ranges
.FromDay(5)                          // From 5th
.UptoDay(25)                         // Until 25th

// Multiple ranges
.Between(("Jan", "Mar"), ("Jun", "Aug"), ("Nov", "Dec"))
```

### Anchoring

```csharp
// Anchor intervals to specific points
.Every(2).Weeks().AnchoredOn(DayOfWeek.Monday)
.Every(3).Months().AnchoredIn(NaturalCronMonth.Feb)
.Every(4).Days().AnchoredAt("12:00")

// Integer-based anchoring
.Every(2).Days().AnchoredOn(15)      // Anchored on 15th day
.Every(3).Weeks().AnchoredIn(5)      // Anchored in 5th week
```

### Timezone Support

```csharp
.WithTimeZone("America/New_York")
.WithTimeZone("Asia/Tokyo")
```

## Output Methods

The builder provides two ways to get results:

```csharp
// Get parsed expression (recommended)
NaturalCronExpr expr = builder.Build();
var nextRun = expr.GetNextOccurrence(DateTime.Now);

// Get raw string (useful for debugging/logging)
string rawExpression = builder.ToRawExpression();
Console.WriteLine($"Generated expression: {rawExpression}");
```

## Use Cases and Examples

### Basic Schedules

```csharp
// Every day at 9:30 AM
var daily = NaturalCronBuilder.Start()
    .Daily()
    .AtTime(9, 30)
    .Build();

// Every Monday
var weekly = NaturalCronBuilder.Start()
    .Weekly()
    .On(DayOfWeek.Monday)
    .Build();

// First day of every month
var monthly = NaturalCronBuilder.Start()
    .Monthly()
    .AtDayPosition(NaturalCronDayPosition.FirstDay)
    .Build();
```

### Interval Schedules

```csharp
// Every 2 hours
var everyTwoHours = NaturalCronBuilder.Start()
    .Every(2)
    .Hours()
    .Build();

// Every 3 weeks on Friday
var everyThreeWeeks = NaturalCronBuilder.Start()
    .Every(3)
    .Weeks()
    .AnchoredOn(DayOfWeek.Friday)
    .Build();

// Every 6 months starting in January
var semiAnnual = NaturalCronBuilder.Start()
    .Every(6)
    .Months()
    .AnchoredIn(NaturalCronMonth.Jan)
    .Build();
```

### Time-Constrained Schedules

```csharp
// Hourly during business hours
var businessHours = NaturalCronBuilder.Start()
    .Hourly()
    .BetweenTime(9, 0, 17, 0)
    .Between(DayOfWeek.Monday, DayOfWeek.Friday)
    .Build();

// Daily from March to November
var seasonal = NaturalCronBuilder.Start()
    .Daily()
    .Between(NaturalCronMonth.Mar, NaturalCronMonth.Nov)
    .Build();

// Weekly on weekdays only
var weekdaysOnly = NaturalCronBuilder.Start()
    .Weekly()
    .Between(DayOfWeek.Monday, DayOfWeek.Friday)
    .Build();
```

### Maintenance Windows
```csharp
// Every Sunday at 2 AM
var maintenance = NaturalCronBuilder.Start()
    .Weekly()
    .On(DayOfWeek.Sunday)
    .AtTime(2, 0)
    .Build();
```

### Business Hours
```csharp
// Every hour during business hours, weekdays only
var businessHours = NaturalCronBuilder.Start()
    .Hourly()
    .BetweenTime(9, 0, 17, 0)
    .Between(DayOfWeek.Monday, DayOfWeek.Friday)
    .Build();
```

### Seasonal Schedules
```csharp
// Daily during summer months
var summer = NaturalCronBuilder.Start()
    .Daily()
    .Between(NaturalCronMonth.Jun, NaturalCronMonth.Aug)
    .Build();
```

### Backup Schedules
```csharp
// Daily backups at midnight, except weekends
var backups = NaturalCronBuilder.Start()
    .Daily()
    .AtTime(0, 0)
    .Between(DayOfWeek.Monday, DayOfWeek.Friday)
    .Build();
```

### Dynamic Expression from config object.
```csharp
// Building expressions dynamically
public NaturalCronExpr CreateSchedule(AnyObject config)
{
    var builder = NaturalCronBuilder.Start();
    
    if (config.UseFullNames)
    {
        builder.UseWeekFullName().UseMonthFullName();
    }
    
    if (config.Interval > 1)
    {
        builder.Every(config.Interval);
    }
    
    switch (config.Unit)
    {
        case TimeUnit.Days:
            builder.Days();
            break;
        case TimeUnit.Weeks:
            builder.Weeks();
            if (config.AnchorDay.HasValue)
                builder.AnchoredOn(config.AnchorDay.Value);
            break;
        case TimeUnit.Months:
            builder.Months();
            break;
    }
    
    if (config.StartTime.HasValue)
    {
        builder.AtTime(config.StartTime.Value.Hour, config.StartTime.Value.Minute);
    }
    
    if (!string.IsNullOrEmpty(config.TimeZone))
    {
        builder.WithTimeZone(config.TimeZone);
    }
    
    return builder.Build();
}
```

### Store and Retrieve Expression from Database
```csharp
// Storing and retrieving expressions
var expr = NaturalCronBuilder.Start()
    .Every(2)
    .Weeks()
    .Build();

// Storing
var schedule = new Schedule
{
    Id = Guid.NewGuid(),
    Expression = expr.Expression,
    CreatedAt = DateTime.UtcNow,
    Job = "BackupJob",
    CreatedBy = "John Doe"
};
scheduleRepository.Add(schedule);

// Retrieving
var schedule = scheduleRepository.Get(id);
var expr = NaturalCronExpr.FromExpression(schedule.Expression);
```

### ToRawExpression() for Debugging
```csharp
var builder = NaturalCronBuilder.Start()
    .Every(2)
    .Weeks()
    .AnchoredOn(DayOfWeek.Monday);

// Debug the expression before building
Console.WriteLine($"Expression: {builder.ToRawExpression()}");
var expr = builder.Build();
```

### Complex Schedules

```csharp
// Every 2 weeks on Monday, during business hours, March to November, Eastern Time
var complex = NaturalCronBuilder.Start()
    .UseWeekFullName()
    .Every(2)
    .Weeks()
    .AnchoredOn(DayOfWeek.Monday)
    .AtTime(10, 0)
    .Between(NaturalCronMonth.Mar, NaturalCronMonth.Nov)
    .WithTimeZone("America/New_York")
    .Build();

// Last Friday of every month at 5 PM
var monthlyReport = NaturalCronBuilder.Start()
    .OnNthWeekday(NaturalCronNthWeekDay.Last, NaturalCronDayOfWeek.Fri)
    .AtTime(17, 0)
    .Build();

// Multiple time windows throughout the day
var multipleWindows = NaturalCronBuilder.Start()
    .Minutely()
    .Between(("09:00", "12:00"), ("14:00", "17:00"))
    .Build();
```
✅ [Back to Main README](../README.md)