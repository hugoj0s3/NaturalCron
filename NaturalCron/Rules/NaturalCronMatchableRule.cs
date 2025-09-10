using NaturalCron.Utils;

namespace NaturalCron.Rules;

public abstract class NaturalCronMatchableRule : NaturalCronRule
{
    private DateTime? lastMatchCall = null;
    private bool? lastMatchCallResult = null;
    
    private DateTime? lastGetTimeToAdvanceToNextOccurenceCall = null;
    private int? lastTimeToAdvanceToNextOccurence = 0;
    private NaturalCronTimeUnit? lastTimeUnitToAdvanceToNextOccurence = NaturalCronTimeUnit.Second;
    
    public bool Match(DateTime dateTime)
    {
        if (lastMatchCall == dateTime && lastMatchCallResult.HasValue)
        {
            return lastMatchCallResult.Value;
        }
        
        
        lastMatchCallResult = DoMatch(dateTime);
        lastMatchCall = dateTime;

        return lastMatchCallResult.Value;
    }
    
    public (int timeToAdvance, NaturalCronTimeUnit timeUnit) GetTimeToAdvanceToNextOccurence(DateTime dateTime)
    {
        if (lastGetTimeToAdvanceToNextOccurenceCall == dateTime && 
            lastTimeToAdvanceToNextOccurence.HasValue && 
            lastTimeUnitToAdvanceToNextOccurence.HasValue)
        {
            return (lastTimeToAdvanceToNextOccurence.Value, lastTimeUnitToAdvanceToNextOccurence.Value);
        }
        
        (lastTimeToAdvanceToNextOccurence, lastTimeUnitToAdvanceToNextOccurence) = DoGetTimeToAdvanceToNextOccurence(dateTime);
        lastGetTimeToAdvanceToNextOccurenceCall = dateTime;
        return (lastTimeToAdvanceToNextOccurence.Value, lastTimeUnitToAdvanceToNextOccurence.Value);
    }
    
    protected abstract bool DoMatch(DateTime dateTime);
    protected virtual (int timeToAdvance, NaturalCronTimeUnit timeUnit) DoGetTimeToAdvanceToNextOccurence(DateTime dateTime)
    {
        return GetMinSafeTimeToAdvance(dateTime);
    }
    
    internal (int, NaturalCronTimeUnit) GetMinSafeTimeToAdvance(DateTime dateTime)
    {
        if (this.TimeUnit == NaturalCronTimeUnit.Second)
        {
            return (1, NaturalCronTimeUnit.Second);
        }
        
        if (this.TimeUnit == NaturalCronTimeUnit.Week || this.TimeUnit == NaturalCronTimeUnit.Month)
        {
            return (1, NaturalCronTimeUnit.Day);
        }
        
        var timeUnitToAdvance = this.TimeUnit - 1;
        var timeUnitValueToAdvance = ExpressionUtil.GetMaxValueByTimeUnit(timeUnitToAdvance, dateTime) - DateTimeUtil.GetPartValue(timeUnitToAdvance, dateTime) - 1;
        if (timeUnitValueToAdvance <= 0)
        {
            return (1, timeUnitToAdvance);
        }
            
        return (timeUnitValueToAdvance, timeUnitToAdvance);
    }
}