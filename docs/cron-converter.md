# Cron Converter

`NaturalCron.CronConverter` is an optional add-on that translates a NaturalCron expression into a classic cron string compatible with standard schedulers (Unix cron, Cronos, Quartz.NET, NCrontab, etc.).

> **Important:** NaturalCron is not a cron-string tool at its core — it is a self-contained scheduling engine. The converter exists as a bridge for systems that already require a cron string as input.

## Installation

```bash
dotnet add package NaturalCron.CronConverter
```

## Quick Usage

```csharp
using NaturalCron;
using NaturalCron.CronConverter;

// From a parsed expression
var expr = NaturalCronExpr.Parse("every day at 18:00");
string cron = expr.ToCronExpression();              // "0 18 * * *"

// From the fluent builder (requires NaturalCron.CronConverter)
string cron2 = NaturalCronBuilder
    .Every(30).Minutes()
    .ToCronExpression();                            // "*/30 * * * *"

// Non-throwing variant
CronConversionResult result = expr.TryToCronExpression();
if (result.IsSuccess)
    Console.WriteLine(result.CronExpression);
else
    Console.WriteLine(string.Join(", ", result.Errors));
```

## Presets

`CronConverterOptions` ships with three ready-made presets that configure all the right field formats and extensions for each scheduler. You don't need to set individual options unless you want to customise beyond what the preset provides.

```csharp
// Standard Unix/Linux crontab — 5 fields, Sun=0…Sat=6
expr.ToCronExpression(CronConverterOptions.ForCrontab());

// Cronos .NET library — 5 fields, W and # extensions enabled by default
expr.ToCronExpression(CronConverterOptions.ForCronos());

// Quartz.NET — 6 fields (with seconds), W and # enabled, DOM/DOW mutual exclusion
expr.ToCronExpression(CronConverterOptions.ForQuartz());
```

| Preset | Fields | DOW format | W / # support |
|--------|--------|------------|---------------|
| `ForCrontab()` | 5 | `0`–`6` (Sun=0) | ✗ |
| `ForCronos()` | 5 | `0`–`6` (Sun=0) | ✓ (on by default) |
| `ForQuartz()` | 6 (seconds) | `1`–`7` or `MON`–`SUN` | ✓ (on by default) |

---

## Options

If a preset doesn't cover your exact setup you can build `CronConverterOptions` manually:

```csharp
var options = new CronConverterOptions
{
    SupportSeconds          = false,                              // true = 6-field cron (prepend seconds)
    WeekFormat              = WeekFormat.ZeroToSix,               // ZeroToSix | OneToSeven | ThreeLetterName
    MonthFormat             = MonthFormat.Number,                 // Number | ThreeLetterName
    ClosestWeekdaySupport   = ClosestWeekdaySupport.NotSupported, // NotSupported | NearestWeekdayW
    NthWeekdaySupport       = NthWeekdaySupport.NotSupported,     // NotSupported | NthHash
    DomDowMutualExclusion   = false,                              // true = Quartz-style ? placeholder
    NonConvertibleBehavior  = NonConvertibleBehavior.ThrowException // ThrowException | Omit
};
```

| Option | Default | Description |
|--------|---------|-------------|
| `SupportSeconds` | `false` | Prepend a seconds field (6-field output). Required for Quartz.NET. |
| `WeekFormat` | `ZeroToSix` | How day-of-week values are rendered. `ZeroToSix` (Sun=0), `OneToSeven` (Mon=1), or `ThreeLetterName` (MON–SUN). |
| `MonthFormat` | `Number` | Month values as numbers (`1`–`12`) or three-letter names (`JAN`–`DEC`). |
| `ClosestWeekdaySupport` | `NotSupported` | Enables `W` notation for `ClosestWeekdayTo` expressions. Supported by Cronos and Quartz. |
| `NthWeekdaySupport` | `NotSupported` | Enables `#` notation for Nth-weekday expressions (e.g. `1stMonday`). Supported by Cronos and Quartz. |
| `DomDowMutualExclusion` | `false` | Quartz requires exactly one of DOM / DOW to be `?`. Enable this when targeting Quartz.NET. |
| `NonConvertibleBehavior` | `ThrowException` | What to do when a feature cannot be converted. `ThrowException` raises a `CronConversionException`; `Omit` silently skips the unsupported part and records an error in `CronConversionResult.Errors`. |

You can also start from a preset and override specific fields using a `with` expression:

```csharp
var options = CronConverterOptions.ForCronos() with
{
    NonConvertibleBehavior = NonConvertibleBehavior.Omit
};
```

---

## What Is Not Supported

Classic cron has a fixed, limited field set. Several NaturalCron features have no equivalent in any cron dialect and will produce an error (or be silently omitted when `NonConvertibleBehavior.Ignore` is set).

### Between ranges with multiple time units

A `between` expression that specifies **more than one time unit** (e.g. `09:00` = hour + minute) cannot be expressed in classic cron:

```
every 30 minutes between 09:15 and 18:45      ❌  (hour + minute)
every 30 minutes between 09:00:00 and 18:00   ❌  (hour + minute + second)
```

A **single-unit** hour range (e.g. `8am` = hour only) maps directly to the cron hour field and is supported:

```
every 15 minutes between 8am and 12pm         ✅  → */15 8-12 * * *
every 30 minutes between 9hr and 17hr         ✅  → */30 9-17 * * *
```

### Timezone

```
every day at 09:00 in America/New_York     ❌
```

Classic cron has no timezone field. The scheduler process timezone is the only control point.

### Every N weeks (N > 1)

```
every 2 weeks on monday                    ❌
every 3 weeks                              ❌
```

Cron has no week field. `every week` (once a week) is supported via a DOW constraint, but intervals greater than one week cannot be expressed.

### Every N years / Year rules

```
every year at 01:00                        ❌
every 2 years                              ❌
```

Classic cron has no year field.

### LastDay / LastWeekday / FirstWeekday

```
every month on LastDay at 09:00            ❌
every month on LastWeekday at 09:00        ❌
every month on FirstWeekday at 09:00       ❌
```

These are month-length-dependent and cannot be expressed in static cron syntax. (Quartz `L` notation for last-day-of-month is **not yet emitted** by the converter.)

### Day arithmetic

```
every month on LastDay-1 at 09:00          ❌
every month on FirstDay+2 at 09:00         ❌
```

Arithmetic on dynamic day anchors (FirstDay, LastDay) is month-dependent and has no cron equivalent.

### Multiple time ranges

NaturalCron supports multiple time windows using bracket syntax:

```
every 25 secs between [1:00pm and 03:00pm, 6:00pm and 8:00pm]   ✅ (NaturalCron)
                                                                  ❌ (cron conversion)
```

Classic cron cannot express multiple active windows in a single expression, so this feature cannot be converted.

### AnchoredOn / AnchoredAt (on EveryX rules)

```
every 2 hours anchored at 01:00            ❌
```

The `AnchoredAt`/`AnchoredOn` modifier shifts interval alignment to a fixed reference point. Classic cron intervals always start from `0` (e.g. `*/2` fires at 0, 2, 4 … not 1, 3, 5), so the alignment offset is lost.

---

### ClosestWeekdayTo — requires explicit opt-in

```
every month on ClosestWeekdayTo 15th at 09:00
```

By default this is **not supported** and raises an error. Enable it with `ClosestWeekdaySupport.NearestWeekdayW` (e.g Quartz and Cronos uses `W` notation):

```csharp
var options = CronConverterOptions.ForCronos();   // W support is on by default
// or:
var options = new CronConverterOptions { ClosestWeekdaySupport = ClosestWeekdaySupport.NearestWeekdayW };
```

### Nth weekday of month — requires explicit opt-in

```
every month on 1stMonday at 09:00
every month on LastFriday at 09:00
```

By default **not supported**. Enable it with `NthWeekdaySupport.NthHash` (e.g Quartz and Cronos uses `#` notation):

```csharp
var options = CronConverterOptions.ForCronos();   // # support is on by default
// or:
var options = new CronConverterOptions { NthWeekdaySupport = NthWeekdaySupport.NthHash };
```

---

## Error Handling

By default the converter **throws** a `CronConversionException` when it encounters an unsupported feature. Use `TryToCronExpression()` or set `NonConvertibleBehavior.Ignore` to suppress throwing:

```csharp
// Option A — non-throwing
var result = expr.TryToCronExpression();
foreach (var error in result.Errors)
    Console.WriteLine(error);

// Option B — ignore unsupported features silently
var options = CronConverterOptions.ForCronos() with
{
    NonConvertibleBehavior = NonConvertibleBehavior.Ignore
};
string partial = expr.ToCronExpression(options);
```

When features are ignored the output is a best-effort partial conversion — verify it produces the schedule you expect.
