using System.Collections.Generic;

namespace NaturalCron.CronConverter;

public class CronConversionResult
{
    public string? CronExpression { get; }
    public bool IsSuccess { get; }
    public IReadOnlyList<string> Errors { get; }

    internal CronConversionResult(string cronExpression)
    {
        CronExpression = cronExpression;
        IsSuccess = true;
        Errors = new List<string>();
    }

    internal CronConversionResult(string? cronExpression, IReadOnlyList<string> errors)
    {
        CronExpression = cronExpression;
        IsSuccess = errors.Count == 0;
        Errors = errors;
    }
}
