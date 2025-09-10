using NaturalCron.Utils;

namespace NaturalCron.Rules;

public class NaturalCronAtInOnMultiplesRule : NaturalCronMatchableRule
{
    internal NaturalCronAtInOnMultiplesRule()
    {
    }
    
    private static readonly List<NaturalCronTimeUnit> TimeUnitsExceptTimeZone = Enum.GetValues(typeof(NaturalCronTimeUnit))
        .Cast<NaturalCronTimeUnit>()
        .Where(x => x != NaturalCronTimeUnit.TimeZone)
        .ToList();
    
    private static readonly List<NaturalCronTimeUnit> TimeUnitsExceptTimeZoneAsc = TimeUnitsExceptTimeZone
        .OrderBy(x => (int)x)
        .ToList();
    
    private static readonly List<NaturalCronTimeUnit> TimeUnitsExceptTimeZoneDesc = TimeUnitsExceptTimeZone
        .OrderByDescending(x => (int)x)
        .ToList();

    public override NaturalCronRuleSpec Spec => NaturalCronRuleSpec.AtInOn;

    internal NaturalCronAtInOnRule[] SecondRules { get; set; } = Array.Empty<NaturalCronAtInOnRule>();
    internal NaturalCronAtInOnRule[] MinuteRules { get; set; } = Array.Empty<NaturalCronAtInOnRule>();
    internal NaturalCronAtInOnRule[] HourRules { get; set; } = Array.Empty<NaturalCronAtInOnRule>();
    internal NaturalCronAtInOnRule[] DayRules { get; set; } = Array.Empty<NaturalCronAtInOnRule>();
    internal NaturalCronAtInOnRule[] WeekRules { get; set; } = Array.Empty<NaturalCronAtInOnRule>();
    internal NaturalCronAtInOnRule[] MonthRules { get; set; } = Array.Empty<NaturalCronAtInOnRule>();
    internal NaturalCronAtInOnRule[] YearRules { get; set; } = Array.Empty<NaturalCronAtInOnRule>();
    
    internal NaturalCronAtInOnRule[] GetAllRules()
    {
        return SecondRules
            .Concat(MinuteRules)
            .Concat(HourRules)
            .Concat(DayRules)
            .Concat(WeekRules)
            .Concat(MonthRules)
            .Concat(YearRules)
            .ToArray();
    }
    
    private int? lastYearOrder = null;
    private int? lastMonthOrder = null;

    internal bool HasTimeUnit(NaturalCronTimeUnit timeUnit)
    {
        switch (timeUnit)
        {
            case NaturalCronTimeUnit.Second:
                return SecondRules.Any();
            case NaturalCronTimeUnit.Minute:
                return MinuteRules.Any();
            case NaturalCronTimeUnit.Hour:
                return HourRules.Any();
            case NaturalCronTimeUnit.Day:
                return DayRules.Any();
            case NaturalCronTimeUnit.Week:
                return WeekRules.Any();
            case NaturalCronTimeUnit.Month:
                return MonthRules.Any();
            case NaturalCronTimeUnit.Year:
                return YearRules.Any();
            default:
                return false;
        }
    }

    protected override bool DoMatch(DateTime dateTime)
    {
        var length = GetLength();
        Reorder(dateTime.Year, dateTime.Month);

        for (var i = 0; i < length; i++)
        {
            if (Match(i, dateTime))
            {
                return true;
            }
        }

        return false;
    }

    internal int GetLength()
    {
        var length = new[]
            {
                SecondRules, MinuteRules, HourRules, DayRules, WeekRules, MonthRules, YearRules
            }
            .Max(x => x.Length);
        return length;
    }

    public bool Match(int index, DateTime dateTime)
    {
        Reorder(dateTime.Year, dateTime.Month);

        if (!Match(index, NaturalCronTimeUnit.Second, dateTime))
            return false;
        if (!Match(index, NaturalCronTimeUnit.Minute, dateTime))
            return false;
        if (!Match(index, NaturalCronTimeUnit.Hour, dateTime))
            return false;
        if (!Match(index, NaturalCronTimeUnit.Day, dateTime))
            return false;
        if (!Match(index, NaturalCronTimeUnit.Week, dateTime))
            return false;
        if (!Match(index, NaturalCronTimeUnit.Month, dateTime))
            return false;
        if (!Match(index, NaturalCronTimeUnit.Year, dateTime))
            return false;

        return true;
    }

    protected override (int, NaturalCronTimeUnit) DoGetTimeToAdvanceToNextOccurence(DateTime dateTime)
    {
        // Ensure positions are chronologically ordered for current year/month context
        Reorder(dateTime.Year, dateTime.Month);
        
        // Strategy 1: Find next position where current time < target time (most efficient)
        var targetIndexResult = GetNextOccurrenceByTargetIndex(dateTime);
        
        // Strategy 2: Handle cases where current time exceeds maximum allowed values
        var maxTargetResult = GetNextOccurenceByMaxTargetResult(dateTime);
        
        // Strategy 3: Handle edge cases and provide minimum safe advancement
        var minTargetResult = GetMinTimeToAdvanceForMultiple(dateTime);
        
        // Select the strategy that advances the furthest (most conservative)
        var allResults = new[] { targetIndexResult, maxTargetResult, minTargetResult };
        var maxResult = allResults
            .OrderByDescending(r => DateTimeUtil.GetDurationInSeconds(r.Item2, r.Item1, dateTime.Year, dateTime.Month))
            .First();
        
        return maxResult;
    }

    /// <summary>
    /// Handles cases where current time exceeds the maximum allowed value for the largest time unit.
    /// Advances to the next clean boundary If last position is "1st December" advance to nest month. 
    /// </summary>
    private (int, NaturalCronTimeUnit) GetNextOccurenceByMaxTargetResult(DateTime dateTime)
    { 
       // Find the largest time unit that has rules (Year > Month > Day > Hour > Minute)
       var maxTimeUnit = TimeUnitsExceptTimeZoneDesc
           .Where(x => x != NaturalCronTimeUnit.Week && x != NaturalCronTimeUnit.Second)
           .Where(x => GetRules(x).Any())
           .FirstOrDefault();
       
       if (maxTimeUnit == default)
       {
           return (1, NaturalCronTimeUnit.Second);
       }
       
       var rules = GetRules(maxTimeUnit);
       if (!rules.Any())
       {
           return (1, NaturalCronTimeUnit.Second);
       }

       // Get the maximum allowed value for this time unit (last position after reordering)
       var maxValue = ExpressionUtil.TryGetValueForMatch(maxTimeUnit, dateTime, rules[rules.Length - 1].InnerExpression);
       if (!maxValue.HasValue)
       {
           return (1, NaturalCronTimeUnit.Second);
       }

       var currentValue = DateTimeUtil.GetPartValue(maxTimeUnit, dateTime);
       
       // Current time exceeds maximum allowed - advance to next boundary
       if (currentValue > maxValue.Value)
       {
           if (maxTimeUnit == NaturalCronTimeUnit.Year)
           {
               // Just force the skip. 
               return (9999, NaturalCronTimeUnit.Year);
           }
           
           var calcToAdvance = CalcToAdvancePerTimeUnit(maxTimeUnit, dateTime);
           return calcToAdvance;
       }
       
       return (1, NaturalCronTimeUnit.Second);
    }
    
    /// <summary>
    /// Finds the next occurrence by locating the first position where current time < target time
    /// This is the most efficient strategy as it leverages chronological ordering from Reorder
    /// </summary>
    private (int, NaturalCronTimeUnit) GetNextOccurrenceByTargetIndex(DateTime dateTime)
    {
        var length = GetLength();
        Reorder(dateTime.Year, dateTime.Month);
        
        // Search for first position where we haven't reached the target time yet
        var targetIndex = -1;
        var targetTimeUnit = NaturalCronTimeUnit.Second;
        
        for (var i = 0; i < length; i++)
        {
            // Check time units from largest to smallest (Year -> Second)
            foreach (var timeUnit in TimeUnitsExceptTimeZoneDesc.Where(x => x != NaturalCronTimeUnit.Week))
            {
                var rules = GetRules(timeUnit);
                if (rules.Any())
                {
                    var targetValue = ExpressionUtil.TryGetValueForMatch(timeUnit, dateTime, rules[i].InnerExpression);
                    var currentValue = DateTimeUtil.GetPartValue(timeUnit, dateTime);
                    
                    // Found a position where we haven't reached the target yet
                    if (targetValue.HasValue && currentValue < targetValue.Value)
                    {
                        targetIndex = i;
                        targetTimeUnit = timeUnit;
                        break; // Exit time unit loop
                    } 
                    
                    // Current time exceeds target for this position, skip to next position
                    if (targetValue.HasValue && currentValue > targetValue.Value)
                    {
                        break; // Exit time unit loop, try next position
                    }
                }
            }
            
            // Found a valid target, exit position loop
            if (targetIndex != -1)
            {
                break;
            }
        }
        
        if (targetIndex != -1)
        {
            if (this.WeekRules.Any() && targetTimeUnit >= NaturalCronTimeUnit.Week)
            {
                return (1, NaturalCronTimeUnit.Day);
            }

            return CalcToAdvancePerTimeUnit(targetTimeUnit, dateTime);
        }
        
        // Fallback: No future position found - current time has passed all scheduled positions
        // We need to cycle back to the first position (index 0) in the next occurrence cycle
        // Find the smallest time unit that doesn't match at position 0 and advance it
        // Example: If positions are [10:30, 14:45] and current time is 16:00 
        //          we advance the hours to reach 10:30 tomorrow
        var minUnMatchedTimeUnit = TimeUnitsExceptTimeZoneAsc
            .Where(x => x != NaturalCronTimeUnit.Week)
            .FirstOrDefault(x => !Match(0, x, dateTime));
            
        if (minUnMatchedTimeUnit == default)
        {
            return (1, NaturalCronTimeUnit.Second);
        }
            
        var minRule = GetRules(minUnMatchedTimeUnit)[0];
        return minRule.GetTimeToAdvanceToNextOccurence(dateTime);
    }
    
    private (int, NaturalCronTimeUnit) GetMinTimeToAdvanceForMultiple(DateTime dateTime)
    {
        if (this.TimeUnit == NaturalCronTimeUnit.Second)
        {
            var minSecond = SecondRules.Select(x => ExpressionUtil.TryGetValueForMatch(NaturalCronTimeUnit.Second, dateTime, x.InnerExpression)).OrderBy(x => x).First();
            var maxSecond = SecondRules.Select(x => ExpressionUtil.TryGetValueForMatch(NaturalCronTimeUnit.Second, dateTime, x.InnerExpression)).OrderByDescending(x => x).First();
            
            var actualSecond = DateTimeUtil.GetPartValue(NaturalCronTimeUnit.Second, dateTime);

            if (minSecond.HasValue && actualSecond < minSecond.Value)
            {
                return (minSecond.Value - actualSecond, NaturalCronTimeUnit.Second);
            }

            if (maxSecond.HasValue && actualSecond > maxSecond)
            {
                return (60 - actualSecond, NaturalCronTimeUnit.Second);
            }
        }

        return this.GetMinSafeTimeToAdvance(dateTime);
    }
    


    private bool Match(int index, NaturalCronTimeUnit timeUnit, DateTime dateTime)
    {
        switch (timeUnit)
        {
            case NaturalCronTimeUnit.Second:
                return !SecondRules.Any() || SecondRules[index].Match(dateTime);
            case NaturalCronTimeUnit.Minute:
                return !MinuteRules.Any() || MinuteRules[index].Match(dateTime);
            case NaturalCronTimeUnit.Hour:
                return !HourRules.Any() || HourRules[index].Match(dateTime);
            case NaturalCronTimeUnit.Day:
                return !DayRules.Any() || DayRules[index].Match(dateTime);
            case NaturalCronTimeUnit.Week:
                return !WeekRules.Any() || WeekRules[index].Match(dateTime);
            case NaturalCronTimeUnit.Month:
                return !MonthRules.Any() || MonthRules[index].Match(dateTime);
            case NaturalCronTimeUnit.Year:
                return !YearRules.Any() || YearRules[index].Match(dateTime);
            default:
                throw new ArgumentOutOfRangeException(nameof(timeUnit), timeUnit, null);
        }
    }

    internal NaturalCronAtInOnRule[] GetRules(NaturalCronTimeUnit timeUnit)
    {
        if (timeUnit == NaturalCronTimeUnit.Second)
        {
            return SecondRules;
        }

        if (timeUnit == NaturalCronTimeUnit.Minute)
        {
            return MinuteRules;
        }

        if (timeUnit == NaturalCronTimeUnit.Hour)
        {
            return HourRules;
        }

        if (timeUnit == NaturalCronTimeUnit.Day)
        {
            return DayRules;
        }

        if (timeUnit == NaturalCronTimeUnit.Week)
        {
            return WeekRules;
        }

        if (timeUnit == NaturalCronTimeUnit.Month)
        {
            return MonthRules;
        }

        if (timeUnit == NaturalCronTimeUnit.Year)
        {
            return YearRules;
        }

        throw new ArgumentOutOfRangeException(nameof(timeUnit), timeUnit, null);
    }
    
    /// <summary>
    /// Reorders all positions chronologically for the given year/month context.
    /// Uses caching to avoid redundant sorting when year/month hasn't changed.
    /// Critical for GetNextOccurrenceByTargetIndex efficiency - ensures positions are in time order.
    /// </summary>
    internal void Reorder(int year, int month)
    {
        // Skip reordering if already sorted for this year/month context
        if (lastYearOrder == year && lastMonthOrder == month)
        {
            return;
        }
        
        var length = GetLength();

        // Choose sorting algorithm based on data size
        if (length > 20)
        {
            QuickSort(year, month, 0, length - 1);
        }
        else
        {
            BubbleSort(year, month, length);
        }

        // Cache the context to avoid redundant sorting
        lastYearOrder = year;
        lastMonthOrder = month;
    }

    private void BubbleSort(int year, int month, int length)
    {
        for (int i = 0; i < length - 1; i++)
        {
            for (int j = 0; j < length - i - 1; j++)
            {
                if (CompareTo(year, month, j, j + 1) > 0)
                {
                    Swap(j, j + 1);
                }
            }
        }
    }

    private void QuickSort(int year, int month, int left, int right)
    {
        if (left < right)
        {
            int pivotIndex = Partition(year, month, left, right);
            QuickSort(year, month, left, pivotIndex - 1);
            QuickSort(year, month, pivotIndex + 1, right);
        }
    }

    private int Partition(int year, int month, int left, int right)
    {
        int pivotIndex = right;
        int storeIndex = left;
        for (int i = left; i < right; i++)
        {
            if (CompareTo(year, month, i, pivotIndex) < 0)
            {
                Swap(i, storeIndex);
                storeIndex++;
            }
        }

        Swap(storeIndex, pivotIndex);
        return storeIndex;
    }

    private int CompareTo(int year, int month, int indexA, int indexB)
    {
        var yearCompareTo = CompareTo(NaturalCronTimeUnit.Year, year, month, indexA, indexB);
        if (yearCompareTo != 0)
        {
            return yearCompareTo;
        }

        var monthCompareTo = CompareTo(NaturalCronTimeUnit.Month, year, month, indexA, indexB);
        if (monthCompareTo != 0)
        {
            return monthCompareTo;
        }

        var weekCompareTo = CompareTo(NaturalCronTimeUnit.Week, year, month, indexA, indexB);
        if (weekCompareTo != 0)
        {
            return weekCompareTo;
        }

        var dayCompareTo = CompareTo(NaturalCronTimeUnit.Day, year, month, indexA, indexB);
        if (dayCompareTo != 0)
        {
            return dayCompareTo;
        }

        var hourCompareTo = CompareTo(NaturalCronTimeUnit.Hour, year, month, indexA, indexB);
        if (hourCompareTo != 0)
        {
            return hourCompareTo;
        }

        var minuteCompareTo = CompareTo(NaturalCronTimeUnit.Minute, year, month, indexA, indexB);
        if (minuteCompareTo != 0)
        {
            return minuteCompareTo;
        }

        var secondCompareTo = CompareTo(NaturalCronTimeUnit.Second, year, month, indexA, indexB);
        if (secondCompareTo != 0)
        {
            return secondCompareTo;
        }

        return 0;
    }

    private int CompareTo(NaturalCronTimeUnit timeUnit, int year, int month, int indexA, int indexB)
    {
        var array = GetRules(timeUnit);
        if (array.Any())
        {
            var aValue = ExpressionUtil.TryGetValueForMatch(timeUnit, year, month, array[indexA].InnerExpression);
            var bValue = ExpressionUtil.TryGetValueForMatch(timeUnit, year, month, array[indexB].InnerExpression);
            
            if (!aValue.HasValue && !bValue.HasValue)
            {
                return 0;
            }

            if (aValue.HasValue && bValue.HasValue)
            {
                return aValue.Value.CompareTo(bValue.Value);
            }

            if (aValue.HasValue && !bValue.HasValue)
            {
                return 1;
            }

            if (!aValue.HasValue && bValue.HasValue)
            {
                return -1;
            }
        }

        return 0;
    }

    private void Swap(int indexA, int indexB)
    {
        var secondRules = GetRules(NaturalCronTimeUnit.Second);
        var minuteRules = GetRules(NaturalCronTimeUnit.Minute);
        var hourRules = GetRules(NaturalCronTimeUnit.Hour);
        var dayRules = GetRules(NaturalCronTimeUnit.Day);
        var weekRules = GetRules(NaturalCronTimeUnit.Week);
        var monthRules = GetRules(NaturalCronTimeUnit.Month);
        var yearRules = GetRules(NaturalCronTimeUnit.Year);

        SwapIfHasAny(secondRules, indexA, indexB);
        SwapIfHasAny(minuteRules, indexA, indexB);
        SwapIfHasAny(hourRules, indexA, indexB);
        SwapIfHasAny(dayRules, indexA, indexB);
        SwapIfHasAny(weekRules, indexA, indexB);
        SwapIfHasAny(monthRules, indexA, indexB);
        SwapIfHasAny(yearRules, indexA, indexB);
    }

    private void SwapIfHasAny(NaturalCronAtInOnRule[] array, int indexA, int indexB)
    {
        if (array.Any())
        {
            var temp = array[indexA];
            array[indexA] = array[indexB];
            array[indexB] = temp;
        }
    }

    /// <summary>
    /// Calculates exact seconds to advance to the next clean boundary for a given time unit.
    /// Example: For Year, advances to January 1st 00:00:00 of next year.
    /// This eliminates edge cases by calculating precise target DateTime and returning exact seconds.
    /// </summary>
    private (int, NaturalCronTimeUnit) CalcToAdvancePerTimeUnit(NaturalCronTimeUnit timeUnit, DateTime dateTime)
    {
        
        // Step 1: Advance by 1 unit (e.g., next year, next month, next day)
        var targetDatetime = DateTimeUtil.AdvanceTimeSafety(timeUnit, dateTime, 1);
        
        // Step 2: Reset all smaller time units to 0 to create clean boundaries
        // Example: 2024-06-15 14:30:45 + 1 Year = 2025-06-15 14:30:45
        //          Reset smaller units = 2025-01-01 00:00:00
        var timeUnitsToReset = TimeUnitsExceptTimeZoneDesc
            .Where(x => x != NaturalCronTimeUnit.Week)
            .Where(x => x < timeUnit);
            
        foreach (var timeUnitToReset in timeUnitsToReset)
        {
            var currentPartValue = DateTimeUtil.GetPartValue(timeUnitToReset, targetDatetime);
            var minTimeUnitValue = ExpressionUtil.GetMinValueByTimeUnit(timeUnitToReset);
            targetDatetime = DateTimeUtil.Substract(timeUnitToReset, targetDatetime, currentPartValue - minTimeUnitValue);
        }
               
        // Step 3: Calculate exact seconds difference (always positive since target is in future)
        var diff = targetDatetime - dateTime;
        if (diff.TotalSeconds > 0)
        {
            // Use Math.Ceiling to ensure fractional seconds are properly advanced
            return ((int)Math.Ceiling(diff.TotalSeconds), NaturalCronTimeUnit.Second);
        }
        
        return (1, NaturalCronTimeUnit.Second);
    }
}
