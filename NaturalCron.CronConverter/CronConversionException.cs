using System;
using System.Collections.Generic;

namespace NaturalCron.CronConverter;

public class CronConversionException : Exception
{
    public IReadOnlyList<string> Reasons { get; }

    internal CronConversionException(IReadOnlyList<string> reasons)
        : base("Cannot convert NaturalCron expression to cron: " + string.Join("; ", reasons))
    {
        Reasons = reasons;
    }
}
