# NaturalCron Quartz Integration

> ⚠️ **Alpha Release:** This package is under active development. The API and features may change before 1.0. Use in production at your own risk.

**NaturalCron.Quartz** adds natural language scheduling support to Quartz.NET, allowing you to define job schedules using human-friendly expressions.

## Features

- Schedule jobs with phrases like `every 5 seconds`, `every Monday at 9am`.
- Seamless integration with Quartz.NET triggers and jobs.
- Familiar API: use `TriggerBuilder.WithNaturalCronSchedule()` just like Quartz's built-in schedules.
- Alpha release: actively developed, API and features may change.

## Installation

Add the NuGet package (replace with your actual package name and version):

```sh
dotnet add package NaturalCron.Quartz --version x.y.z-alpha
```

## Usage Example

```csharp
using Quartz;
using NaturalCron.Quartz;

var trigger = TriggerBuilder.Create()
    .WithIdentity("naturalTrigger")
    .WithNaturalCronSchedule("every 25 min on friday between 1:00pm and 03:00pm")
    .StartNow()
    .Build();

var triigger2 = TriggerBuilder.Create()
    .WithIdentity("naturalTrigger", "group1")
    .WithNaturalCronSchedule(NaturalCronBuilder.Daily().AtTime(9, 30).Build())
    .Build();

```


## Compatibility
- Quartz.NET 3.x

## Limitations
- **ICalendar support is not yet implemented.** Calendar-based exclusions (holidays, etc.) are ignored.

## NaturalCron Documentation
- [Main README](../README.md)
- [Expression Syntax](../expression-syntax.md)
- [API Reference](../api-reference.md)
- [Builder Usage](../builder.md)

## License
MIT

## Contributing
Pull requests and feedback are welcome! Open an issue or PR on GitHub.
