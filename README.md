# NaturalCron
[![NuGet](https://img.shields.io/nuget/v/NaturalCron.svg)](https://www.nuget.org/packages/NaturalCron)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Awesome](https://cdn.rawgit.com/sindresorhus/awesome/d7305f38d29fed78fa85652e3a63e154dd8e8829/media/badge.svg)](https://github.com/sindresorhus/awesome)

NaturalCron is a **human-readable scheduling engine for .NET**. It lets you write schedules in a clear and intuitive way instead of memorizing cryptic cron strings.

**Why?** Because memorizing `0 18 * * 1-5` is harder than understanding `every day between monday and friday at 6:00pm`.

**Readable schedules reduce mistakes, write expressions that you can understand at a glance.**

> **Note:** NaturalCron is **not a cron converter**. It’s a new expressive syntax for better readability.

## 💡 Why use NaturalCron?
- **Readable syntax**: `every 30 minutes in [jan, jun] between 09:00 and 18:00`
- **Fluent Builder API**: Strongly typed for .NET developers.
- **No online generators needed**.
- **Features**:
  - Ranges
  - Weekday list
  - Closest weekday, first/last day handling
  - Time zone support with IANA TZ names

## 🚀 Quick Start

### **Using an Expression**
```csharp
using System;
using NaturalCron;

// Define an advanced expression
string expression = "every 30 minutes in [jan, jun] between 09:00 and 18:00";

// Parse and calculate next occurrence (local time)
NaturalCronExpr schedule = NaturalCronExpr.Parse(expression);
DateTime next = schedule.GetNextOccurrence(DateTime.Now);

Console.WriteLine($"Next occurrence: {next}");
```

### **Using Fluent Builder**
```csharp
using System;
using NaturalCron;
using NaturalCron.Builder;

NaturalCronExpr schedule = NaturalCronBuilder
.Every(30).Minutes()
.In(NaturalCronMonth.Jan)
.Between("09:00", "18:00")
.Build();

DateTime next = schedule.GetNextOccurrenceInUtc(DateTime.UtcNow);

Console.WriteLine($"Next occurrence in UTC: {next}");
```

⚡ **Try it online:** [Run on .NET Fiddle](https://dotnetfiddle.net/NfEBM8)


## 🆚 Cron vs NaturalCron
| Task                                      | Cron Expression        | NaturalCron Expression                                          |
|------------------------------------------|------------------------|-----------------------------------------------------------------|
| Every 5 minutes                         | `*/5 * * * *`         | `every 5 minutes`                                              |
| Every weekday at 6 PM                   | `0 18 * * 1-5`        | `every day between mon and fri at 18:00`                       |
| Every 30 min in Jan and Jun (9:00-18:00)| *(Complex in cron)*    | `every 30 minutes in [jan, jun] between 09:00 and 18:00`       |


## 📝 Syntax Overview
Examples:
```
every 5 minutes
every day at 18:00
every 30 minutes in [jan, jun] between 09:00 and 18:00
every day at 10:00 on [monday, wednesday, friday]
```


## 📦 Installation
```bash
dotnet add package NaturalCron
```

## 📖 Documentation
- [Expression Syntax](docs/expression-syntax.md) — Learn how to write human-readable recurrence rules.
- [Fluent Builder Guide](docs/builder.md) — Build expressions easily with type safety and IntelliSense.
- [API Reference](docs/api-reference.md) — Full API details and usage examples.


## 🤝 Contributing
Contributions, bug reports, and feature requests are welcome!

Please open an [issue](../../issues) or submit a [pull request](../../pulls).

## ❤️ Support the Project
If you enjoy using NaturalCron and would like to support its future development, consider buying me a coffee. Your support helps keep the project alive and growing!

[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-☕-orange)](https://buymeacoffee.com/hugoj0s3)

