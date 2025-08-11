using System.Diagnostics.CodeAnalysis;
using Quartz;
using Quartz.Impl.Triggers;

namespace NaturalCron.Quartz;

/// <summary>
/// A Quartz.NET trigger that supports natural language cron expressions,
/// allowing you to schedule jobs using human-friendly phrases such as
/// "every 5 seconds", "every Monday at 9am", or "every last day of the month".
///
/// <para>
/// This trigger is compatible with all Quartz.NET schedulers. It does not yet support
/// calendar-based exclusions (ICalendar).
/// </para>
/// </summary>
[Serializable]
public class NaturalCronTriggerImpl : AbstractTrigger, INaturalCronTrigger
{
    [NonSerialized]
    private NaturalCronExpr? builtExpression;
    
    [NonSerialized]
    private DateTimeOffset? nextFireTimeUtc;
    
    [NonSerialized]
    private DateTimeOffset? previousFireTimeUtc;

    /// <summary>
    /// Initializes a new instance of the <see cref="NaturalCronTriggerImpl"/> class with a name, group, and parsed natural cron expression.
    /// </summary>
    /// <param name="name">The trigger name.</param>
    /// <param name="group">The trigger group.</param>
    /// <param name="builtExpression">The parsed natural cron expression.</param>
    public NaturalCronTriggerImpl(string name, string group, NaturalCronExpr builtExpression) : base()
    {
        this.builtExpression = builtExpression;
        this.naturalCronExpression = builtExpression.Expression;
        Key = new TriggerKey(name, group);
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="NaturalCronTriggerImpl"/> class with a parsed natural cron expression.
    /// </summary>
    /// <param name="builtExpression">The parsed natural cron expression.</param>
    public NaturalCronTriggerImpl(NaturalCronExpr builtExpression) : base()
    {
        this.builtExpression = builtExpression;
        this.naturalCronExpression = builtExpression.Expression;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="NaturalCronTriggerImpl"/> class with a raw natural cron expression string.
    /// </summary>
    /// <param name="expression">The natural cron expression as a string.</param>
    public NaturalCronTriggerImpl(string expression) : base()
    {
        this.NaturalCronExpression = expression;
    }

    private string? naturalCronExpression;

    /// <summary>
    /// Gets/Sets the natural language cron expression used by this trigger.
    /// </summary>
    public string? NaturalCronExpression
    {
        get => naturalCronExpression;
        set
        {
            naturalCronExpression = value;
            builtExpression = null;
            EnsureExpressionParsed();
        }
    }
    
    public override DateTimeOffset? FinalFireTimeUtc => null;

    public override bool HasMillisecondPrecision => false;

    public override IScheduleBuilder GetScheduleBuilder()
    {
        var expression = EnsureExpressionParsed();
        return new NaturalCronScheduleBuilder(expression);
    }

    public override bool GetMayFireAgain() => true;

    public override DateTimeOffset? GetNextFireTimeUtc() => nextFireTimeUtc;

    /// <summary>
    /// Calculates the next fire time after the given time.
    /// </summary>
    /// <param name="afterTime">The time after which to find the next fire time.</param>
    /// <returns>The next scheduled fire time, or null if none.</returns>
    public override DateTimeOffset? GetFireTimeAfter(DateTimeOffset? afterTime)
    {
        var expression = GetExpressionParsed();
        if (expression == null)
        {
            return null;
        }
        
        if (afterTime == null)
        {
            afterTime = StartTimeUtc;
        }
        
        if (StartTimeUtc > afterTime.Value.ToUniversalTime())
        {
            afterTime = StartTimeUtc.AddSeconds(-1).ToOffset(afterTime.Value.Offset);
        }
        
        if (EndTimeUtc.HasValue && afterTime.Value.ToUniversalTime() > EndTimeUtc.Value)
        {
            return null;
        }
        
        return CalcAfterTime(afterTime.Value, expression);
    }

    public override DateTimeOffset? GetPreviousFireTimeUtc() => previousFireTimeUtc;

    public override void Triggered(ICalendar? cal)
    {
        previousFireTimeUtc = nextFireTimeUtc;
        nextFireTimeUtc = GetFireTimeAfter(nextFireTimeUtc);
    }

    public override DateTimeOffset? ComputeFirstFireTimeUtc(ICalendar? cal)
    {
        nextFireTimeUtc = GetFireTimeAfter(StartTimeUtc);
        return nextFireTimeUtc;
    }

    public override void UpdateAfterMisfire(ICalendar? cal)
    {
        nextFireTimeUtc = GetFireTimeAfter(StartTimeUtc);
    }
    
    /// <summary>
    /// This trigger currently does not support Quartz's ICalendar-based calendar exclusions. Calendar support may be added in a future release.
    /// </summary>
    /// <param name="cal">The calendar instance.</param>
    /// <param name="misfireThreshold">The misfire threshold.</param>

    public override void UpdateWithNewCalendar(ICalendar cal, TimeSpan misfireThreshold) {}

    public override void SetNextFireTimeUtc(DateTimeOffset? nextFireTime) => nextFireTimeUtc = nextFireTime;

    public override void SetPreviousFireTimeUtc(DateTimeOffset? previousFireTime) => previousFireTimeUtc = previousFireTime;

    protected override bool ValidateMisfireInstruction(int misfireInstruction) => true;
    
    private DateTimeOffset? CalcAfterTime(DateTimeOffset afterTime, NaturalCronExpr expression)
    {
        DateTime next = expression.GetNextOccurrence(afterTime.LocalDateTime);
        var nextOffSet = new DateTimeOffset(next, afterTime.Offset);
        
        nextFireTimeUtc = nextOffSet.ToUniversalTime();
        return nextOffSet;
    }

    private NaturalCronExpr EnsureExpressionParsed()
    {
        var expression = GetExpressionParsed();
        if (expression == null)
        {
            throw new ArgumentException($"Invalid NaturalCron expression: {NaturalCronExpression}");
        }
        
        return expression;
    }
    
    private NaturalCronExpr? GetExpressionParsed()
    {
        if (builtExpression != null)
        {
            return builtExpression;
        }

        if (string.IsNullOrEmpty(naturalCronExpression))
        {
            return null;
        }
        
        (builtExpression, var errors) = NaturalCronExpr.TryParse(naturalCronExpression);
        
        if (builtExpression == null || errors.Any())
        {
            return null;
        }
        
        return builtExpression;
    }
}