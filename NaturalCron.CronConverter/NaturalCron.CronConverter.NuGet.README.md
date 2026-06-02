# NaturalCron.CronConverter
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Optional add-on for **[NaturalCron](https://www.nuget.org/packages/NaturalCron)** that translates natural language expressions into classic cron strings compatible with Unix cron, Cronos, Quartz.NET, and NCrontab.

> NaturalCron is a self-contained scheduling engine — this package is only needed when an external system requires a classic cron string as input.

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

// From the fluent builder
string cron2 = NaturalCronBuilder
    .Every(30).Minutes()
    .ToCronExpression();                            // "*/30 * * * *"

// Non-throwing variant
CronConversionResult result = expr.TryToCronExpression();
if (result.IsSuccess)
    Console.WriteLine(result.CronExpression);
```

## Target-Library Presets

```csharp
string crontab = expr.ToCronExpression(CronConverterOptions.ForCrontab()); // 5-field, Unix cron
string cronos  = expr.ToCronExpression(CronConverterOptions.ForCronos());  // 5-field, Cronos
string quartz  = expr.ToCronExpression(CronConverterOptions.ForQuartz());  // 6-field, Quartz.NET
```

## What Is Not Supported

Some NaturalCron features have no classic cron equivalent and will throw a `CronConversionException` by default:

- Time-window ranges with multiple time units: `between 09:00 and 18:00` (hour + minute)
- Timezone rules
- Every N weeks (N > 1) / yearly rules
- LastDay, LastWeekday, FirstWeekday, day arithmetic

See the [full converter documentation](https://github.com/hugoj0s3/NaturalCron/blob/main/docs/cron-converter.md) for details and workarounds.
