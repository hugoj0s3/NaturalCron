using Quartz;
using Quartz.Impl.Triggers;

namespace NaturalCron.Quartz;

[Serializable]
public class NaturalCronTrigger : AbstractTrigger, INaturalCronTrigger
{
    [NonSerialized]
    private NaturalCronExpr? builtExpression;
    
    [NonSerialized]
    private DateTimeOffset? nextFireTimeUtc;
    
    [NonSerialized]
    private DateTimeOffset? previousFireTimeUtc;

    public NaturalCronTrigger(string name, string group, NaturalCronExpr builtExpression) : base()
    {
        this.builtExpression = builtExpression;
        this.NaturalCronExpression = builtExpression.Expression;
        Key = new TriggerKey(name, group);
    }
    
    public NaturalCronTrigger(NaturalCronExpr builtExpression) : base()
    {
        this.builtExpression = builtExpression;
        this.NaturalCronExpression = builtExpression.Expression;
    }
    
    public NaturalCronTrigger(string expression) : base()
    {
        this.NaturalCronExpression = expression;
    }

    public string? NaturalCronExpression { get; set; }

    public override DateTimeOffset? FinalFireTimeUtc => null;

    public override bool HasMillisecondPrecision => false;

    public override IScheduleBuilder GetScheduleBuilder()
    {
        var expression = EnsureExpressionParsed();
        return new NaturalCronScheduleBuilder(expression);
    }

    public override bool GetMayFireAgain() => true;

    public override DateTimeOffset? GetNextFireTimeUtc() => nextFireTimeUtc;

    public override DateTimeOffset? GetFireTimeAfter(DateTimeOffset? afterTime)
    {
        var expression = GetExpressionParsed();
        if (expression == null)
        {
            return null;
        }
        
        DateTimeOffset baseTime = afterTime ?? DateTimeOffset.UtcNow;
        DateTime nextUtc = expression.GetNextOccurrenceInUtc(baseTime.UtcDateTime);
        nextFireTimeUtc = new DateTimeOffset(nextUtc, TimeSpan.Zero);
        if (afterTime == null)
        {
            return nextFireTimeUtc;
        }
        
        return new DateTimeOffset(nextUtc, afterTime.Value.Offset);
    }

    public override DateTimeOffset? GetPreviousFireTimeUtc() => previousFireTimeUtc;

    public override void Triggered(ICalendar? cal)
    {
        previousFireTimeUtc = nextFireTimeUtc;
        nextFireTimeUtc = GetFireTimeAfter(nextFireTimeUtc);
    }

    public override DateTimeOffset? ComputeFirstFireTimeUtc(ICalendar? cal)
    {
        nextFireTimeUtc = GetFireTimeAfter(DateTimeOffset.UtcNow);
        return nextFireTimeUtc;
    }

    public override void UpdateAfterMisfire(ICalendar? cal)
    {
        // For simplicity, skip misfire handling in first version
        nextFireTimeUtc = GetFireTimeAfter(DateTimeOffset.UtcNow);
    }

    public override void UpdateWithNewCalendar(ICalendar cal, TimeSpan misfireThreshold) {}

    public override void SetNextFireTimeUtc(DateTimeOffset? nextFireTime) => nextFireTimeUtc = nextFireTime;

    public override void SetPreviousFireTimeUtc(DateTimeOffset? previousFireTime) => previousFireTimeUtc = previousFireTime;

    protected override bool ValidateMisfireInstruction(int misfireInstruction) => true;

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
            

        if (string.IsNullOrEmpty(NaturalCronExpression))
        {
            return null;
        }
        
        (builtExpression, var errors) = NaturalCronExpr.TryParse(NaturalCronExpression);
        
        if (builtExpression == null || errors.Any())
        {
            return null;
        }
        
        return builtExpression;
    }
}