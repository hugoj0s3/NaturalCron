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
        Reorder(dateTime.Year, dateTime.Month);
        
        (int, NaturalCronTimeUnit) targetIndexResult = GetNextOccurrenceByTargetIndex(dateTime);
        (int, NaturalCronTimeUnit) maxTargetResult = GetNextOccurenceByMaxTargetResult(dateTime);
        (int, NaturalCronTimeUnit) minTargetResult = GetMinTimeToAdvanceForMultiple(dateTime);
        
        var allResults = new[] { targetIndexResult, maxTargetResult, minTargetResult };
        var maxResult = allResults
            .OrderByDescending(r => DateTimeUtil.GetDurationInSeconds(r.Item2, r.Item1, dateTime.Year, dateTime.Month))
            .First();
        
        
        return maxResult;
    }

    private (int, NaturalCronTimeUnit) GetNextOccurenceByMaxTargetResult(DateTime dateTime)
    { 
       var timeUnits = TimeUnitsExceptTimeZoneDesc.Where(x => x != NaturalCronTimeUnit.Week && x != NaturalCronTimeUnit.Second);
       foreach (var timeUnit in timeUnits)
       {
           var rules = GetRules(timeUnit);
           if (!rules.Any())
           {
               continue;
           }

           var maxValue = ExpressionUtil.TryGetValueForMatch(timeUnit, dateTime, rules[rules.Length - 1].InnerExpression);
           if (!maxValue.HasValue)
           {
               continue;
           }

           var partValue = DateTimeUtil.GetPartValue(timeUnit, dateTime);
           if (partValue > maxValue.Value)
           {
               var targetDatetime = DateTimeUtil.AdvanceTimeSafety(timeUnit, dateTime, 1);
               var timeToDecrease = TimeUnitsExceptTimeZoneDesc.Where(x => x != NaturalCronTimeUnit.Week).Where(x => x < timeUnit);
               foreach (var timeUnitToDecrease in timeToDecrease)
               {
                   var datePartValue = DateTimeUtil.GetPartValue(timeUnitToDecrease, targetDatetime);
                   targetDatetime = DateTimeUtil.Substract(timeUnitToDecrease, targetDatetime, datePartValue);
               }
               
               var diff = targetDatetime - dateTime;
               if (diff.TotalSeconds > 0)
               {
                   return ((int)diff.TotalSeconds, NaturalCronTimeUnit.Second);
               }
           }
       }
       
       return (1, NaturalCronTimeUnit.Second);
    }

    private (int, NaturalCronTimeUnit) GetNextOccurenceByDirectlyTargetResult(DateTime dateTime)
    {
        DateTime? nextTarget = GetNextTargetDirectly(dateTime);
        (int, NaturalCronTimeUnit) directlyTargetResult = (1, NaturalCronTimeUnit.Second);
        if (nextTarget.HasValue)
        {
            var diff = nextTarget.Value - dateTime;
            var totalSeconds = (int)diff.TotalSeconds;
            if (totalSeconds <= 0)
            {
                totalSeconds = 1;
            } 
            directlyTargetResult = (totalSeconds, NaturalCronTimeUnit.Second);
        }

        return directlyTargetResult;
    }

    private DateTime? GetNextTargetDirectly(DateTime dateTime)
    {
        var length = GetLength();
        DateTime? earliestTarget = null;
        
        for (int i = 0; i < length; i++)
        {
            var target = TryGetDirectTargetFromPosition(i, dateTime);
            if (target.HasValue && target > dateTime)
            {
                if (!earliestTarget.HasValue || target < earliestTarget)
                {
                    earliestTarget = target;
                }
            }
        }
        
        return earliestTarget;
    }
    
    private DateTime? TryGetDirectTargetFromPosition(int position, DateTime baseTime)
    {
        var year = baseTime.Year;
        var month = baseTime.Month;
        var day = 1;
        var hour = 0;
        var minute = 0;
        var second = 0;
        
        if (YearRules.Any() && position < YearRules.Length)
        {
            var yearValue = ExpressionUtil.TryGetValueForMatch(NaturalCronTimeUnit.Year, baseTime, YearRules[position].InnerExpression);
            if (yearValue.HasValue) year = yearValue.Value;
        }
        
        if (MonthRules.Any() && position < MonthRules.Length)
        {
            var monthValue = ExpressionUtil.TryGetValueForMatch(NaturalCronTimeUnit.Month, baseTime, MonthRules[position].InnerExpression);
            if (monthValue.HasValue) month = monthValue.Value;
        }
        
        if (DayRules.Any() && position < DayRules.Length)
        {
            var dayValue = ExpressionUtil.TryGetValueForMatch(NaturalCronTimeUnit.Day, baseTime, DayRules[position].InnerExpression);
            if (dayValue.HasValue) day = dayValue.Value;
        }
        
        if (HourRules.Any() && position < HourRules.Length)
        {
            var hourValue = ExpressionUtil.TryGetValueForMatch(NaturalCronTimeUnit.Hour, baseTime, HourRules[position].InnerExpression);
            if (hourValue.HasValue) hour = hourValue.Value;
        }
        
        if (MinuteRules.Any() && position < MinuteRules.Length)
        {
            var minuteValue = ExpressionUtil.TryGetValueForMatch(NaturalCronTimeUnit.Minute, baseTime, MinuteRules[position].InnerExpression);
            if (minuteValue.HasValue) minute = minuteValue.Value;
        }
        
        if (SecondRules.Any() && position < SecondRules.Length)
        {
            var secondValue = ExpressionUtil.TryGetValueForMatch(NaturalCronTimeUnit.Second, baseTime, SecondRules[position].InnerExpression);
            if (secondValue.HasValue) second = secondValue.Value;
        }
        
        try
        {
            return new DateTime(year, month, day, hour, minute, second);
        }
        catch
        {
            return null;
        }
    }
    
    private (int, NaturalCronTimeUnit) GetNextOccurrenceByTargetIndex(DateTime dateTime)
    {
        var length = GetLength();
        Reorder(dateTime.Year, dateTime.Month);
        
        var targetIndex = -1;
        for (var i = 0; i < length; i++)
        {
            foreach (var timeUnit in TimeUnitsExceptTimeZoneDesc.Where(x => x != NaturalCronTimeUnit.Week))
            {
                var rules = GetRules(timeUnit);
                if (rules.Any())
                {
                    var valueForMatch = ExpressionUtil.TryGetValueForMatch(timeUnit, dateTime, rules[i].InnerExpression);
                    var partValue = DateTimeUtil.GetPartValue(timeUnit, dateTime);
                    if (valueForMatch.HasValue && partValue < valueForMatch.Value)
                    {
                        targetIndex = i;
                        break;
                    } 
                    
                    if (valueForMatch.HasValue && partValue > valueForMatch.Value)
                    {
                        break;
                    }
                }
            }
            
            if (targetIndex != -1)
            {
                break;
            }
        }

        if (targetIndex != -1)
        {
            var targetTimeUnit = TimeUnitsExceptTimeZoneAsc
                .Where(x => x != NaturalCronTimeUnit.Week)
                .First(x => !Match(targetIndex, x, dateTime));
            
            if (this.WeekRules.Any() && targetTimeUnit >= NaturalCronTimeUnit.Week)
            {
                return (1, NaturalCronTimeUnit.Day);
            }
            
            var targetRule = GetRules(targetTimeUnit)[targetIndex];
            return targetRule.GetTimeToAdvanceToNextOccurence(dateTime);
        }

        return (1, NaturalCronTimeUnit.Second);
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

            if (minSecond.HasValue && actualSecond > maxSecond)
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
    
    internal void Reorder(int year, int month)
    {
        if (lastYearOrder == year && 
            lastMonthOrder == month)
        {
            return;
        }
        
        var length = GetLength();

        if (length > 20)
        {
            QuickSort(year, month, 0, length - 1);
        }
        else
        {
            BubbleSort(year, month, length);
        }

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
}
