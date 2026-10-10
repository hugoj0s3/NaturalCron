using NaturalCron.Utils;

namespace NaturalCron.Rules;

public class NaturalCronEveryXRule : NaturalCronRule
{
    internal NaturalCronEveryXRule()
    {
    }
    
    private static readonly NaturalCronTimeUnit[] TimeUnitsWithoutTimezone =
    [
        NaturalCronTimeUnit.Second,
        NaturalCronTimeUnit.Minute,
        NaturalCronTimeUnit.Hour,
        NaturalCronTimeUnit.Day,
        NaturalCronTimeUnit.Month,
        NaturalCronTimeUnit.Year,
        NaturalCronTimeUnit.Week
    ];

    public override NaturalCronRuleSpec Spec => NaturalCronRuleSpec.Every;

    public int Value { get; internal set; }
    
    /// <summary>
    /// An anchored recurrence is a repeating schedule that always aligns with a specific value within the time unit.
    /// For example:
    ///   - `every 2 months AnchoredOn Feb` means the recurrence will happen in February, April, June, August, October, December, etc.
    ///   - `every 3 months AnchoredOn Feb` means the recurrence will happen in February, May, August, November, and so on—always anchored to February.
    /// 
    /// When using an anchored value (e.g., `AnchoredOn Feb` for months), the recurrence interval must be a divisor of the time unit's total length
    /// (e.g., 12 for months, 60 for minutes) to ensure that the anchor is always hit consistently.
    /// Example: `every 3 months AnchoredOn Feb` is valid because every 3 months from February will always include February in the cycle.
    /// Example: `every 5 months AnchoredOn Feb` is NOT valid, as 5 does not divide 12, so February will not always be included.
    /// Although we will not have any exception if this condition is not met, the anchor behavior will be inconsistent and unintuitive.
    /// </summary>
    public string? AnchoredValue { get; internal set; }
    
    internal DateTime ApplyAnchoredValueIfPresent(DateTime dateTime, DateTime baseDateTime, IList<NaturalCronMatchableRule> matchableRules)
    {
        var anchoredValue = AnchoredValue ?? string.Empty;
        if (string.IsNullOrEmpty(anchoredValue))
        {
            return dateTime;
        }

        var anchoredValueInt = ExpressionUtil.TryParseToInt(TimeUnit, anchoredValue);
        if (!anchoredValueInt.HasValue)
        {
            return dateTime;
        }
        
        if (TimeUnit == NaturalCronTimeUnit.Week)
        {
            // Return to the first day of the week.
            var dayOfWeek = ExpressionUtil.TryGetValueForMatch(NaturalCronTimeUnit.Week, dateTime, anchoredValue);
            while (DateTimeUtil.GetPartValue(NaturalCronTimeUnit.Week, dateTime) != dayOfWeek)
            {
                dateTime = DateTimeUtil.Add(NaturalCronTimeUnit.Day, dateTime, 1);
            }
            
            return dateTime;
        }
        
        var timeValue = DateTimeUtil.GetPartValue(TimeUnit, dateTime);
        if (Value <= 0)
        {
            return dateTime; // avoid division by zero
        }

        // If timeValue aligns with anchoredValueInt modulo Value, it's on the anchor
        if ((timeValue - anchoredValueInt.Value) % Value == 0)
        {
            return dateTime;
        }

        var startDateTime = dateTime;
        
        var offset = (timeValue - anchoredValueInt.Value) % Value;
        if (offset < 0) offset += Value; // ensure positive
        if (offset != 0)
        {
            dateTime = DateTimeUtil.Substract(TimeUnit, dateTime, offset);
        }
        
        while (dateTime < baseDateTime)
        {
            dateTime = AddEveryXTime(dateTime, matchableRules);
        }

        return dateTime;
    }

    internal DateTime AddEveryXTime(DateTime localNextOccurrence, IList<NaturalCronMatchableRule> matchableRules)
    {
        localNextOccurrence = DateTimeUtil.Add(this.TimeUnit, localNextOccurrence, this.Value);
        
        var timeUnitsToReset = TimeUnitsWithoutTimezone
            .Where(x => x < this.TimeUnit)
            .Where(x => HasTimeUnitRule(matchableRules, x))
            .Select(x => x == NaturalCronTimeUnit.Week ? NaturalCronTimeUnit.Day : x)
            .Distinct()
            .ToList();

        foreach (var timeUnitToReset in timeUnitsToReset)
        {
            var partValue = DateTimeUtil.GetPartValue(timeUnitToReset, localNextOccurrence) -
                            ExpressionUtil.GetMinValueByTimeUnit(timeUnitToReset);
            localNextOccurrence = DateTimeUtil.Add(timeUnitToReset, localNextOccurrence, partValue * -1);
        }
      
        // If we have multiples At/On/In for weeks, back 6 days to match with the others days of the week.
        if (matchableRules.Any(x => x is NaturalCronAtInOnMultiplesRule m && m.HasTimeUnit(NaturalCronTimeUnit.Week)) && this.TimeUnit == NaturalCronTimeUnit.Week)
        {
            localNextOccurrence = localNextOccurrence.AddDays(-6);
        }

        return localNextOccurrence;
    }
    
    private static bool HasTimeUnitRule(IList<NaturalCronMatchableRule> rules, NaturalCronTimeUnit timeUnit)
    {
        foreach (var matchableRule in rules)
        {
            // Filter-only rules (except, custom keywords, mixed lists) have no target value to reset to.
            if (matchableRule is NaturalCronExceptRule || matchableRule is NaturalCronCustomKeywordRule ||
                matchableRule is NaturalCronAnyOfRule)
            {
                continue;
            }

            if (matchableRule.TimeUnit == timeUnit)
            {
                return true;
            }

            if (matchableRule is NaturalCronAtInOnMultiplesRule atInOnMultiplesRule && 
                atInOnMultiplesRule.HasTimeUnit(timeUnit))
            {
                return true;
            }
        }

        return false;
    }
}