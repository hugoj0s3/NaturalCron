using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using NaturalCron.Rules;
using NaturalCron.Tokens.Parser;
using NaturalCron.Utils;
using NaturalCron.Tokens;
using TimeZoneConverter;

namespace NaturalCron;

/// <summary>
/// Represents a parsed NaturalCron expression with rules for generating recurrence schedules.
/// </summary>
public class NaturalCronExpr
{
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

    internal NaturalCronExpr() {}
    
    /// <summary>
    /// The original expression string used to create this object.
    /// </summary>
    public string Expression { get; private set; } = string.Empty;
    
    /// <summary>
    /// The parsed rules that define the recurrence.
    /// </summary>
    public IReadOnlyCollection<NaturalCronRule> Rules { get; internal set; } = new List<NaturalCronRule>();

    public IReadOnlyCollection<NaturalCronRule> SecondRules() =>
        Rules.Where(x => x.TimeUnit == NaturalCronTimeUnit.Second).ToList();

    public IReadOnlyCollection<NaturalCronRule> MinuteRules() =>
        Rules.Where(x => x.TimeUnit == NaturalCronTimeUnit.Minute).ToList();

    public IReadOnlyCollection<NaturalCronRule> HourRules() =>
        Rules.Where(x => x.TimeUnit == NaturalCronTimeUnit.Hour).ToList();

    public IReadOnlyCollection<NaturalCronRule> DayRules() =>
        Rules.Where(x => x.TimeUnit == NaturalCronTimeUnit.Day).ToList();

    public IReadOnlyCollection<NaturalCronRule> WeekRules() =>
        Rules.Where(x => x.TimeUnit == NaturalCronTimeUnit.Week).ToList();

    public IReadOnlyCollection<NaturalCronRule> MonthRules() =>
        Rules.Where(x => x.TimeUnit == NaturalCronTimeUnit.Month).ToList();

    public IReadOnlyCollection<NaturalCronRule> YearRules() =>
        Rules.Where(x => x.TimeUnit == NaturalCronTimeUnit.Year).ToList();

    public NaturalCronIanaTimeZoneRule? TimeZoneRule() =>
        Rules.FirstOrDefault(x => x.TimeUnit == NaturalCronTimeUnit.TimeZone) as NaturalCronIanaTimeZoneRule;

    /// <summary>
    /// Tries to get the next occurrence in UTC. Returns null if no occurrence is found within the specified lookahead period.
    /// </summary>
    /// <param name="utcBaseTime">The base time in UTC from which to find the next occurrence</param>
    /// <param name="utcMaxLookahead">Maximum lookahead time in UTC. If null, defaults to DateTime.MaxValue</param>
    /// <returns>The next occurrence in UTC, or null if no occurrence is found</returns>
    public DateTime? TryGetNextOccurrenceInUtc(DateTime utcBaseTime, DateTime? utcMaxLookahead = null)
    {
        // 1. Convert to the expression time zone (UTC when there is none)
        var baseTimeInTz = TimeZoneRule()?.ConvertFromUtc(utcBaseTime) ?? utcBaseTime;
        DateTime maxLookaheadInTz;
        if (utcMaxLookahead.HasValue)
        {
            maxLookaheadInTz = TimeZoneRule()?.ConvertFromUtc(utcMaxLookahead.Value) ?? utcMaxLookahead.Value;
        }
        else
        {
            maxLookaheadInTz = DateTime.MaxValue;
        }

        // 2. Get the next occurrence in that time zone
        var resultInTz = InternalTryGetNextOccurrenceInTz(baseTimeInTz, maxLookaheadInTz,
            TimeZoneRule()?.TimeZoneInfo ?? TimeZoneInfo.Utc);

        // 3. Convert back to UTC
        return !resultInTz.HasValue ? null : TimeZoneRule()?.ConvertToUtc(resultInTz.Value)
            ?? DateTime.SpecifyKind(resultInTz.Value, DateTimeKind.Utc);
    }

    /// <summary>
    /// Gets the next occurrence in UTC. Throws an exception if no occurrence is found within the specified lookahead period.
    /// </summary>
    /// <param name="utcBaseTime">The base time in UTC from which to find the next occurrence</param>
    /// <param name="utcMaxLookahead">Maximum lookahead time in UTC. If null, defaults to DateTime.MaxValue</param>
    /// <returns>The next occurrence in UTC</returns>
    /// <exception cref="InvalidOperationException">Thrown when no next occurrence is found</exception>
    public DateTime GetNextOccurrenceInUtc(DateTime utcBaseTime, DateTime? utcMaxLookahead = null)
    {
        var nextOccurrence = TryGetNextOccurrenceInUtc(utcBaseTime, utcMaxLookahead);
        if (nextOccurrence == null)
        {
            throw new InvalidOperationException("No next occurrence found");
        }

        return nextOccurrence.Value;
    }

    /// <summary>
    /// Tries to get the next occurrences in UTC. Returns as many occurrences as found, up to the specified count.
    /// Stops when no more occurrences are found or the count is reached.
    /// </summary>
    /// <param name="utcBaseTime">The base time in UTC from which to find the next occurrences</param>
    /// <param name="count">The number of occurrences to retrieve</param>
    /// <param name="utcMaxLookahead">Maximum lookahead time in UTC. If null, defaults to DateTime.MaxValue</param>
    /// <returns>A list of occurrences in UTC (may contain fewer than the requested count)</returns>
    public IList<DateTime> TryGetNextOccurrencesInUtc(DateTime utcBaseTime, int count, DateTime? utcMaxLookahead = null)
    {
        var result = new List<DateTime>();
        for (var i = 0; i < count; i++)
        {
            var nextOccurrence = TryGetNextOccurrenceInUtc(utcBaseTime, utcMaxLookahead);
            if (nextOccurrence == null)
            {
                break;
            }

            result.Add(nextOccurrence.Value);
            utcBaseTime = nextOccurrence.Value;
        }

        return result;
    }

    /// <summary>
    /// Gets the next occurrences in UTC. Throws an exception if fewer occurrences than requested are found.
    /// Use TryGetNextOccurrencesInUtc if you are not sure there are enough occurrences available.
    /// </summary>
    /// <param name="utcBaseTime">The base time in UTC from which to find the next occurrences</param>
    /// <param name="count">The number of occurrences to retrieve</param>
    /// <param name="utcMaxLookahead">Maximum lookahead time in UTC. If null, defaults to DateTime.MaxValue</param>
    /// <returns>A list of occurrences in UTC</returns>
    /// <exception cref="InvalidOperationException">Thrown when fewer than the requested number of occurrences are found</exception>
    public IList<DateTime> GetNextOccurrencesInUtc(DateTime utcBaseTime, int count, DateTime? utcMaxLookahead = null)
    {
        var result = TryGetNextOccurrencesInUtc(utcBaseTime, count, utcMaxLookahead);
        if (result.Count < count)
        {
            throw new InvalidOperationException("Not enough occurrences found");
        }

        return result;
    }

    /// <summary>
    /// Tries to get the next occurrence using the current thread's timezone. Returns null if no occurrence is found within the specified lookahead period.
    /// </summary>
    /// <param name="baseTime">The base time in the current thread's timezone from which to find the next occurrence</param>
    /// <param name="maxLookahead">Maximum lookahead time in the current thread's timezone. If null, defaults to DateTime.MaxValue</param>
    /// <returns>The next occurrence in the current thread's timezone, or null if no occurrence is found</returns>
    public DateTime? TryGetNextOccurrence(DateTime baseTime, DateTime? maxLookahead = null)
    {
        if (this.TimeZoneRule() == null)
        {
            return InternalTryGetNextOccurrenceInTz(baseTime, maxLookahead ?? DateTime.MaxValue,
                baseTime.Kind == DateTimeKind.Utc ? TimeZoneInfo.Utc : TimeZoneInfo.Local);
        }

        // Convert local time to UTC and call the UTC method
        var utcBaseTime = TimeZoneInfo.ConvertTimeToUtc(baseTime);
        var utcMaxLookahead = maxLookahead.HasValue ? TimeZoneInfo.ConvertTimeToUtc(maxLookahead.Value) : DateTime.MaxValue;

        var result = TryGetNextOccurrenceInUtc(utcBaseTime, utcMaxLookahead);

        // Convert UTC to local time
        return result == null ? null : TimeZoneInfo.ConvertTimeFromUtc(result.Value, TimeZoneInfo.Local);
    }

    /// <summary>
    /// Gets the next occurrence using the current thread's timezone. Throws an exception if no occurrence is found within the specified lookahead period.
    /// </summary>
    /// <param name="baseTime">The base time in the current thread's timezone from which to find the next occurrence</param>
    /// <param name="maxLookahead">Maximum lookahead time in the current thread's timezone. If null, defaults to DateTime.MaxValue</param>
    /// <returns>The next occurrence in the current thread's timezone</returns>
    /// <exception cref="InvalidOperationException">Thrown when no next occurrence is found</exception>
    public DateTime GetNextOccurrence(DateTime baseTime, DateTime? maxLookahead = null)
    {
        var nextOccurrence = TryGetNextOccurrence(baseTime, maxLookahead);
        if (nextOccurrence == null)
        {
            throw new InvalidOperationException("No next occurrence found");
        }

        return nextOccurrence.Value;
    }

    /// <summary>
    /// Tries to get the next occurrences using the current thread's timezone. Returns as many occurrences as found, up to the specified count.
    /// Stops when no more occurrences are found or the count is reached.
    /// </summary>
    /// <param name="baseTime">The base time in the current thread's timezone from which to find the next occurrences</param>
    /// <param name="count">The number of occurrences to retrieve</param>
    /// <param name="maxLookahead">Maximum lookahead time in the current thread's timezone. If null, defaults to DateTime.MaxValue</param>
    /// <returns>A list of occurrences in the current thread's timezone (may contain fewer than the requested count)</returns>
    public IList<DateTime> TryGetNextOccurrences(DateTime baseTime, int count, DateTime? maxLookahead = null)
    {
        var result = new List<DateTime>();
        for (var i = 0; i < count; i++)
        {
            var nextOccurrence = TryGetNextOccurrence(baseTime, maxLookahead);
            if (nextOccurrence == null)
            {
                break;
            }

            result.Add(nextOccurrence.Value);
            baseTime = nextOccurrence.Value;
        }

        return result;
    }

    /// <summary>
    /// Gets the next occurrences using the current thread's timezone. Throws an exception if fewer occurrences than requested are found.
    /// Use TryGetNextOccurrences if you are not sure there are enough occurrences available.
    /// </summary>
    /// <param name="baseTime">The base time in the current thread's timezone from which to find the next occurrences</param>
    /// <param name="count">The number of occurrences to retrieve</param>
    /// <param name="maxLookahead">Maximum lookahead time in the current thread's timezone. If null, defaults to DateTime.MaxValue</param>
    /// <returns>A list of occurrences in the current thread's timezone</returns>
    /// <exception cref="InvalidOperationException">Thrown when fewer than the requested number of occurrences are found</exception>
    public IList<DateTime> GetNextOccurrences(DateTime baseTime, int count, DateTime? maxLookahead = null)
    {
        var result = TryGetNextOccurrences(baseTime, count, maxLookahead);
        if (result.Count < count)
        {
            throw new InvalidOperationException("Not enough occurrences found");
        }

        return result;
    }
    
    /// <summary>
    /// Tries to get the next occurrence in a specified IANA timezone. Returns null if no occurrence is found within the specified lookahead period.
    /// </summary>
    /// <param name="baseTimeInTz">The base time interpreted in the specified IANA timezone from which to find the next occurrence</param>
    /// <param name="ianaTzId">IANA timezone ID (case-insensitive, e.g., "America/New_York" or "europe/lisbon")</param>
    /// <param name="maxLookaheadInTz">Maximum lookahead time in the specified timezone. If null, defaults to DateTime.MaxValue</param>
    /// <returns>The next occurrence in the specified timezone, or null if no occurrence is found</returns>
    public DateTime? TryGetNextOccurrenceInTz(DateTime baseTimeInTz, string ianaTzId, DateTime? maxLookaheadInTz = null)
    {
        // Case-insensitive validation
        if (!TZConvert.KnownIanaTimeZoneNames
                .Contains(ianaTzId, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid IANA time zone ID: {ianaTzId}");
        }

        var tz = TZConvert.GetTimeZoneInfo(ianaTzId);

        if (this.TimeZoneRule() == null)
        {
            // No tz rule: evaluate the rules directly on the provided wall-clock time (baseTimeInTz) in that zone.
            return InternalTryGetNextOccurrenceInTz(baseTimeInTz, maxLookaheadInTz ?? DateTime.MaxValue, tz);
        }

        // Interpret the input in the specified timezone, not Local
        var utcBaseTime = TimeZoneInfo.ConvertTimeToUtc(baseTimeInTz, tz);
        DateTime? utcMaxLookahead = maxLookaheadInTz.HasValue
            ? TimeZoneInfo.ConvertTimeToUtc(maxLookaheadInTz.Value, tz)
            : null; // let the UTC method apply its own default

        var resultUtc = TryGetNextOccurrenceInUtc(utcBaseTime, utcMaxLookahead);

        // Return in the same specified timezone
        return resultUtc == null ? null : TimeZoneInfo.ConvertTimeFromUtc(resultUtc.Value, tz);
    }

    /// <summary>
    /// Gets the next occurrence in a specified IANA timezone. Throws an exception if no occurrence is found within the specified lookahead period.
    /// </summary>
    /// <param name="baseTimeInZone">The base time interpreted in the specified IANA timezone from which to find the next occurrence</param>
    /// <param name="ianaTimeZoneId">IANA timezone ID (case-insensitive, e.g., "America/New_York" or "europe/lisbon")</param>
    /// <param name="maxLookaheadInZone">Maximum lookahead time in the specified timezone. If null, defaults to DateTime.MaxValue</param>
    /// <returns>The next occurrence in the specified timezone</returns>
    /// <exception cref="InvalidOperationException">Thrown when no next occurrence is found</exception>
    public DateTime GetNextOccurrenceInTz(DateTime baseTimeInZone, string ianaTimeZoneId, DateTime? maxLookaheadInZone = null)
    {
        var next = TryGetNextOccurrenceInTz(baseTimeInZone, ianaTimeZoneId, maxLookaheadInZone);
        if (next == null)
        {
            throw new InvalidOperationException("No next occurrence found");
        }

        return next.Value;
    }

    /// <summary>
    /// Tries to get the next occurrences in a specified IANA timezone. Returns as many occurrences as found, up to the specified count.
    /// Stops when no more occurrences are found or the count is reached.
    /// </summary>
    /// <param name="baseTimeInTz">The base time interpreted in the specified IANA timezone from which to find the next occurrences</param>
    /// <param name="count">The number of occurrences to retrieve</param>
    /// <param name="ianaTzId">IANA timezone ID (case-insensitive, e.g., "America/New_York" or "europe/lisbon")</param>
    /// <param name="maxLookaheadInTz">Maximum lookahead time in the specified timezone. If null, defaults to DateTime.MaxValue</param>
    /// <returns>A list of occurrences in the specified timezone (may contain fewer than the requested count)</returns>
    public IList<DateTime> TryGetNextOccurrencesInTz(DateTime baseTimeInTz, int count, string ianaTzId, DateTime? maxLookaheadInTz = null)
    {
        var result = new List<DateTime>();
        for (var i = 0; i < count; i++)
        {
            var nextOccurrence = TryGetNextOccurrenceInTz(baseTimeInTz, ianaTzId, maxLookaheadInTz);
            if (nextOccurrence == null)
            {
                break;
            }

            result.Add(nextOccurrence.Value);
            baseTimeInTz = nextOccurrence.Value;
        }

        return result;
    }

    /// <summary>
    /// Gets the next occurrences in a specified IANA timezone. Throws an exception if fewer occurrences than requested are found.
    /// Use TryGetNextOccurrencesInTz if you are not sure there are enough occurrences available.
    /// </summary>
    /// <param name="baseTimeInTz">The base time interpreted in the specified IANA timezone from which to find the next occurrences</param>
    /// <param name="count">The number of occurrences to retrieve</param>
    /// <param name="ianaTzId">IANA timezone ID (case-insensitive, e.g., "America/New_York" or "europe/lisbon")</param>
    /// <param name="maxLookaheadInTz">Maximum lookahead time in the specified timezone. If null, defaults to DateTime.MaxValue</param>
    /// <returns>A list of occurrences in the specified timezone</returns>
    /// <exception cref="InvalidOperationException">Thrown when fewer than the requested number of occurrences are found</exception>
    public IList<DateTime> GetNextOccurrencesInTz(DateTime baseTimeInTz, int count, string ianaTzId, DateTime? maxLookaheadInTz = null)
    {
        var result = TryGetNextOccurrencesInTz(baseTimeInTz, count, ianaTzId, maxLookaheadInTz);
        if (result.Count < count)
        {
            throw new InvalidOperationException("Not enough occurrences found");
        }

        return result;
    }

    /// <summary>
    /// Parses a string expression into a NaturalCronExpr. Throws an exception if the expression is invalid.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static NaturalCronExpr Parse(string expression)
    {
        var (naturalCronExpr, errors) = InternalTryParse(expression);
        if (errors.Any())
        {
            throw new ArgumentException(string.Join("\n", errors));
        }

        return naturalCronExpr!;
    }

    /// <summary>
    /// Tries to parse a string expression into a NaturalCronExpr. Returns null if the expression is invalid and a list of error messages.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public static (NaturalCronExpr?, IList<string> errors) TryParse(string expression)
    {
        var (naturalCronExpr, errors) = InternalTryParse(expression);

        if (errors.Any())
        {
            return (null, errors);
        }

        return (naturalCronExpr, new List<string>());
    }

    internal static (NaturalCronExpr?, IList<string> errors) InternalTryParse(string expression)
    {
        var tokens = TokenParserUtil.Tokenizer(expression);
        if (tokens.Any(x => !x.IsValid))
        {
            return (null, tokens.Where(x => !x.IsValid).Select(x => x.GetErrorWithLinePosition()).ToList());
        }

        var groupSpecs = TokenParserUtil.GroupSpecs(tokens);
        var rules = new List<NaturalCronRule>();
        var parserErrors = new List<string>();
        foreach (var groupedSpec in groupSpecs)
        {
            var result = TokenParserUtil.ParseGroupedSpec(groupedSpec);
            rules.AddRange(result.rules);
            parserErrors.AddRange(result.errors.Select(x => $"{TokenParserUtil.JoinTokens(groupedSpec.Tokens)} - {x}"));
        }

        if (tokens.Any(x => !x.IsValid))
        {
            var tokenErrors = tokens.Where(x => !x.IsValid).Select(x => x.GetErrorWithLinePosition()).ToList();
            parserErrors.AddRange(tokenErrors);
        }

        if (parserErrors.Any())
        {
            return (null, parserErrors);
        }

        NaturalCronExpr naturalCronExpr = new()
        {
            Expression = expression, Rules = rules.AsReadOnly(),
        };

        var validationErrors = naturalCronExpr.Validate();

        if (!validationErrors.Any())
        {
            var everyXRule = naturalCronExpr.Rules.FirstOrDefault(x => x is NaturalCronEveryXRule);
            if (everyXRule != null && everyXRule.TimeUnit >= NaturalCronTimeUnit.Month)
            {
                var matchableRules = naturalCronExpr.Rules.OfType<NaturalCronMatchableRule>().ToList();
                // If there is no day rule and every is year or month, transform week rules to day rules.
                // It is basically the same. E.g every month on [monday, wednesday] => every month on [1stMon, 1stWed]
                TransformWeekRulesToFirstWeekDayRulesIfNeeded(matchableRules);
            }
        }

        return (naturalCronExpr, validationErrors);
    }

    private static void TransformWeekRulesToFirstWeekDayRulesIfNeeded(List<NaturalCronMatchableRule> matchableRules)
    {
        bool hasDayRules = matchableRules.Any(x =>
            (x is NaturalCronAtInOnRule || x is NaturalCronAtInOnMultiplesRule) &&
            (x.TimeUnit == NaturalCronTimeUnit.Day ||
             (x is NaturalCronAtInOnMultiplesRule m && m.HasTimeUnit(NaturalCronTimeUnit.Day)))
        );

        if (hasDayRules)
        {
            return;
        }

        var weekRules = matchableRules
            .Where(x => x.TimeUnit == NaturalCronTimeUnit.Week ||
                        (x is NaturalCronAtInOnMultiplesRule m && m.HasTimeUnit(NaturalCronTimeUnit.Week)))
            .ToList();

        if (!weekRules.Any())
        {
            return;
        }

        foreach (var weekRule in weekRules)
        {
            if (weekRule is NaturalCronAtInOnMultiplesRule m)
            {
                m.DayRules = m.WeekRules
                    .Select(x =>
                    {
                        var weekdayIdx =
                            ExpressionUtil.TryGetValueForMatch(NaturalCronTimeUnit.Week, DateTime.Now, x.InnerExpression) ?? 1;
                        return new NaturalCronAtInOnRule
                        {
                            TimeUnit = NaturalCronTimeUnit.Day,
                            FullExpression = x.FullExpression,
                            InnerExpression = "1st" + KeywordsConstants.WeekdayWords[weekdayIdx - 1]
                        };
                    })
                    .ToArray();
                m.WeekRules = Array.Empty<NaturalCronAtInOnRule>();
            }
            else if (weekRule is NaturalCronAtInOnRule atInOnRule)
            {
                var weekdayIdx =
                    ExpressionUtil.TryGetValueForMatch(NaturalCronTimeUnit.Week, DateTime.Now, atInOnRule.InnerExpression) ?? 1;
                atInOnRule.InnerExpression = "1st" + KeywordsConstants.WeekdayWords[weekdayIdx - 1];
            }
        }
    }

    private (int timeToAdvance, NaturalCronTimeUnit timeUnit) GetTimeToAdvanceToNextOccurence(DateTime datetime)
    {
        var unmatchedRules = ScheduleRules
            .OfType<NaturalCronMatchableRule>()
            .Where(x => !x.Match(datetime))
            .ToList();

        if (!unmatchedRules.Any())
        {
            return (1, NaturalCronTimeUnit.Second);
        }

        var minTimeUnit = unmatchedRules.Select(x =>
            {
                var (timeToAdvance, timeUnit) = x.GetTimeToAdvanceToNextOccurence(datetime);

                // In case of week we just advance to next day. Week doesn't follow the pattern of other time units e.g advance one week
                // it will simple advance to next day of the week.
                if (timeUnit == NaturalCronTimeUnit.Week)
                {
                    return (1, NaturalCronTimeUnit.Day);
                }

                // Not all month have the same number of days.
                // For special cases like last day of month, closest day to, Last[WeekDay] It can advance to the next occurrence skipping the day rules.
                if (timeUnit == NaturalCronTimeUnit.Month ||
                    (timeUnit == NaturalCronTimeUnit.Year && DateTime.IsLeapYear(datetime.Year) && datetime.Month == 2))
                {
                    var simulatedNextOccurrence = AdvanceNextOccurrence(datetime, NaturalCronTimeUnit.Month, timeToAdvance);
                    simulatedNextOccurrence = DateTimeUtil.SetPartValue(NaturalCronTimeUnit.Day, simulatedNextOccurrence, 1);
                    var totalDays = (int)(simulatedNextOccurrence - datetime).TotalDays;
                    return (totalDays, NaturalCronTimeUnit.Day);
                }

                return (timeToAdvance, timeUnit);
            })
            .OrderBy(x => DateTimeUtil.GetDurationInSeconds(x.Item2, x.Item1, datetime.Year, datetime.Month))
            .FirstOrDefault();

        return minTimeUnit;
    }

    private bool AllMatch(DateTime dateTime)
    {
        return AllMatch(dateTime, ScheduleRules.ToList());
    }

    private bool ShouldAddEveryX(DateTime baseTime, DateTime anchoredOccurrence, NaturalCronTimeUnit everyXTimeUnit)
    {
        var result = anchoredOccurrence == baseTime && AllMatch(anchoredOccurrence);
        if (!result)
        {
            return false;
        }

        // If we have multiples ensure it matches with the last occurence of it to guarantee the cycle is complete.
        var multiples = Rules.OfType<NaturalCronAtInOnMultiplesRule>()
            .Where(x => x.GetAllRules().Any(y => y.TimeUnit <= everyXTimeUnit))
            .ToList();
        return multiples.All(x => x.Match(x.GetLength() - 1, baseTime));
    }

    private static bool AllMatch(DateTime dateTime, IList<NaturalCronRule> rules)
    {
        return rules.OfType<NaturalCronMatchableRule>().All(x => x.Match(dateTime));
    }

    private IList<string> Validate()
    {
        var errors = new List<string>();

        bool hasAtOnInOrEveryXRule = Rules.Any(x => x is NaturalCronAtInOnRule) ||
                                     Rules.Any(x => x is NaturalCronEveryXRule) ||
                                     Rules.Any(x => x is NaturalCronAtInOnMultiplesRule);
        if (!hasAtOnInOrEveryXRule)
        {
            errors.Add("At least one At/On/In or Every/Secondly/Minutely/Hourly/Daily/Weekly/Monthly/Yearly is required.");
            return errors;
        }

        // 2. Only one EveryX rule allowed
        var everyXRules = Rules.Where(x => x is NaturalCronEveryXRule).ToList();
        if (everyXRules.Count > 1)
        {
            errors.Add("Duplicate Every/Secondly/Minutely/Hourly/Daily/Weekly/Monthly/Yearly rule is not allowed.");
            return errors;
        }

        // 3. No At/On/In or AtInOnMultiples rule at same unit as EveryX.
        //    No Every day combined with At/On/In week rule
        //    No Every week with anchored combined with At/On/In multiples rule
        var everyXRule = everyXRules.FirstOrDefault() as NaturalCronEveryXRule;
        if (everyXRule != null)
        {
            var everyTimeUnit = everyXRule.TimeUnit;
            if (everyTimeUnit != NaturalCronTimeUnit.Week)
            {
                if (Rules.Any(x =>
                        (x.TimeUnit == everyTimeUnit && x is NaturalCronAtInOnRule) ||
                        (x is NaturalCronAtInOnMultiplesRule m && m.HasTimeUnit(everyTimeUnit))))
                {
                    errors.Add("You cannot use an At/On/In rules at the same time unit as an 'every X' rule except for week.");
                    return errors;
                }
            }

            if (everyTimeUnit == NaturalCronTimeUnit.Week)
            {
                if (Rules.Any(x =>
                        (x.TimeUnit == NaturalCronTimeUnit.Week && x is NaturalCronAtInOnRule) ||
                        (x is NaturalCronAtInOnMultiplesRule m && m.HasTimeUnit(NaturalCronTimeUnit.Week))) &&
                    !string.IsNullOrEmpty(everyXRule.AnchoredValue))
                {
                    errors.Add("You cannot combine an 'every week' + anchored rule with an At/On/In rule for weeks.");
                    return errors;
                }
            }
        }

        // 4. No duplicate At/On/In rules at the same time unit (Between is allowed)
        foreach (var timeUnit in TimeUnitsWithoutTimezone)
        {
            var atOnInRules = Rules.Where(x => x is NaturalCronAtInOnMultiplesRule || x is NaturalCronAtInOnRule);
            var count = atOnInRules
                .Count(x => x.TimeUnit == timeUnit || x is NaturalCronAtInOnMultiplesRule m && m.HasTimeUnit(timeUnit));

            if (count > 1)
            {
                errors.Add($"Duplicate At/On/In rules for the same time unit are not allowed.");
                return errors;
            }
        }

        // 5. Check if there are any invalid between rules
        var allBetweenRules = Rules
            .SelectMany(x =>
            {
                if (x is NaturalCronBetweenRule b)
                {
                    return new[]
                    {
                        b
                    };
                }

                if (x is NaturalCronBetweenMultiplesRule bM)
                {
                    return bM.BetweenRules;
                }

                return Array.Empty<NaturalCronBetweenRule>();
            }).ToList();

        if (allBetweenRules.Any() && everyXRule == null)
        {
            errors.Add("Between rules is not allowed without Every/Secondly/Minutely/Hourly/Daily/Weekly/Monthly/Yearly rule.");
            return errors;
        }

        foreach (var betweenRule in allBetweenRules)
        {
            if (!betweenRule.Validate())
            {
                var rangeError = string.Join(", ",
                    betweenRule.StartEndValues.Select(x => $"{betweenRule.FullExpression}"));
                errors.Add($"Between rule ranges are invalid for {rangeError}");
            }
        }

        var allAtOnInRules = RulesIncludingExceptInnerRules()
            .Where(x => x is NaturalCronAtInOnMultiplesRule || x is NaturalCronAtInOnRule)
            .SelectMany(x =>
            {
                if (x is NaturalCronAtInOnMultiplesRule b)
                {
                    return b.GetAllRules();
                }

                if (x is NaturalCronAtInOnRule bM)
                {
                    return new[]
                    {
                        bM
                    };
                }

                return Array.Empty<NaturalCronAtInOnRule>();
            })
            .ToList();
        foreach (var atOnInRule in allAtOnInRules)
        {
            var isValid = ExpressionUtil.ValidateValueForMatch(atOnInRule.TimeUnit, 1, 1, atOnInRule.InnerExpression);
            if (!isValid)
            {
                errors.Add($"At/On/In rule '{atOnInRule.FullExpression}' is not valid.");
            }
        }

        var timezoneRule = Rules.FirstOrDefault(x => x is NaturalCronIanaTimeZoneRule) as NaturalCronIanaTimeZoneRule;
        if (timezoneRule != null)
        {
            if (!TZConvert.KnownIanaTimeZoneNames.ToArray().ToUpper().Contains(timezoneRule.IanaId.ToUpper()))
            {
                errors.Add($"Timezone rule '{timezoneRule.IanaId}' is not valid.");
            }
        }

        if (everyXRule != null && everyXRule.Value <= 0)
        {
            errors.Add("Every value cannot be less than or equal to 0.");
        }

        return errors;
    }

    private DateTime? InternalTryGetNextOccurrenceInTz(DateTime baseTimeInTz, DateTime maxLookaheadInTz, TimeZoneInfo timeZone)
    {
        // Custom rules receive the zone the wall-clock time is in
        var customRules = RulesIncludingExceptInnerRules()
            .OfType<NaturalCronCustomKeywordRule>();
        var timeZoneChanged = false;
        foreach (var customRule in customRules)
        {
            timeZoneChanged |= customRule.SetEffectiveTimeZone(timeZone);
        }

        // The rules wrapping a custom rule cache its result too.
        if (timeZoneChanged)
        {
            foreach (var wrapperRule in Rules.SelectMany(GetWrapperRules))
            {
                wrapperRule.ResetCache();
            }
        }

        // Except rules remove occurrences from the schedule, they don't shift it:
        // e.g "every hour except 22:00" skips 22:00 and continues at 23:00 rather than 22:01.
        var exceptRules = Rules.OfType<NaturalCronExceptRule>().ToList();
        var nextOccurrenceInTz = InternalTryGetNextScheduleOccurrenceInTz(baseTimeInTz, maxLookaheadInTz);
        while (nextOccurrenceInTz.HasValue && exceptRules.Any(x => !x.Match(nextOccurrenceInTz.Value)))
        {
            try
            {
                nextOccurrenceInTz = InternalTryGetNextScheduleOccurrenceInTz(nextOccurrenceInTz.Value, maxLookaheadInTz);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Every occurrence up to DateTime.MaxValue was excluded.
                return null;
            }
        }

        return nextOccurrenceInTz;
    }

    // Rules that define the schedule, i.e. all rules but the except exclusions.
    private IReadOnlyCollection<NaturalCronRule> ScheduleRules =>
        scheduleRules ??= Rules.Where(x => x is not NaturalCronExceptRule).ToList();

    private IReadOnlyCollection<NaturalCronRule>? scheduleRules;

    private DateTime? InternalTryGetNextScheduleOccurrenceInTz(DateTime baseTimeInTz, DateTime maxLookaheadInTz)
    {
        // 1. Normalize input
        baseTimeInTz = new DateTime(
            baseTimeInTz.Year, baseTimeInTz.Month, baseTimeInTz.Day,
            baseTimeInTz.Hour, baseTimeInTz.Minute, baseTimeInTz.Second);

        var nextOccurrenceInTz = baseTimeInTz;

        // 2. Apply "every X" rule anchoring if present
        var everyRule = Rules.OfType<NaturalCronEveryXRule>().FirstOrDefault();
        if (everyRule != null)
        {
            // Anchor to the correct value (e.g., hour 0, minute 0, etc.)
            var matchableRules = ScheduleRules.OfType<NaturalCronMatchableRule>().ToList();
            var anchoredOccurrence = everyRule.ApplyAnchoredValueIfPresent(nextOccurrenceInTz, baseTimeInTz, matchableRules);

            // If at the base time and all rules match, advance once more
            if (ShouldAddEveryX(baseTimeInTz, anchoredOccurrence, everyRule.TimeUnit))
            {
                anchoredOccurrence = everyRule.AddEveryXTime(anchoredOccurrence, matchableRules);
            }

            // If we crossed into a new upper unit (e.g., new day), re-anchor and re-advance if needed
            if (everyRule.AnchoredValue != null)
            {
                var upperUnit = everyRule.TimeUnit + 1;
                if (upperUnit < NaturalCronTimeUnit.Year)
                {
                    var upperPartNow = DateTimeUtil.GetPartValue(upperUnit, anchoredOccurrence);
                    var upperPartBase = DateTimeUtil.GetPartValue(upperUnit, baseTimeInTz);
                    if (upperPartNow > upperPartBase)
                    {
                        anchoredOccurrence =
                            everyRule.ApplyAnchoredValueIfPresent(anchoredOccurrence, nextOccurrenceInTz, matchableRules);

                        // If at the base time and all rules match, advance once more
                        if (ShouldAddEveryX(baseTimeInTz, anchoredOccurrence, everyRule.TimeUnit))
                        {
                            anchoredOccurrence = everyRule.AddEveryXTime(anchoredOccurrence, matchableRules);
                        }
                    }
                }
            }

            nextOccurrenceInTz = anchoredOccurrence;
        }

        // 3. If still not after base time, advance by the minimum non-everyX unit
        if (nextOccurrenceInTz <= baseTimeInTz)
        {
            var matchableRules = ScheduleRules.OfType<NaturalCronMatchableRule>().ToList();

            // 1. Filter out all between rules from matchableRules
            var nonBetweenRules = matchableRules
                .Where(x => x is not NaturalCronBetweenRule && x is not NaturalCronBetweenMultiplesRule);

            // 2. Add between rules that do NOT match nextOccurrenceInTz
            var unmatchedBetweenRules = matchableRules
                .Where(x => x is NaturalCronBetweenRule b || x is NaturalCronBetweenMultiplesRule)
                .Where(x => !x.Match(nextOccurrenceInTz))
                .ToList();

            // 3. Combine all candidates
            var allCandidates = nonBetweenRules
                .Concat(unmatchedBetweenRules);

            // 4. Find the minimum TimeUnit
            var minAdvanceUnit = allCandidates
                .OrderBy(x => x.TimeUnit)
                .FirstOrDefault()?
                .TimeUnit ?? NaturalCronTimeUnit.Second;

            if (minAdvanceUnit == NaturalCronTimeUnit.Week)
            {
                minAdvanceUnit = NaturalCronTimeUnit.Day;
            }

            // When the every rule ticks more often than the smallest rule unit (e.g. every hour on wednesday),
            // advance by the every unit instead, otherwise the ticks in between would be skipped.
            if (everyRule != null && everyRule.TimeUnit != NaturalCronTimeUnit.Week && everyRule.TimeUnit < minAdvanceUnit)
            {
                minAdvanceUnit = everyRule.TimeUnit;
            }

            while (nextOccurrenceInTz <= baseTimeInTz)
            {
                nextOccurrenceInTz = AdvanceNextOccurrence(nextOccurrenceInTz, minAdvanceUnit, 1);
            }
        }

        // 4. Find the next actual match (with all rules)
        (nextOccurrenceInTz, var hasMatch) = FindNextMatchOccurrence(nextOccurrenceInTz, maxLookaheadInTz, everyRule);

        // 5. Return UTC if match found
        return hasMatch ? nextOccurrenceInTz : (DateTime?)null;
    }

    private (DateTime, bool) FindNextMatchOccurrence(DateTime nextOccurrenceInTz, DateTime maxLookaheadInTz,
        NaturalCronEveryXRule? everyRule)
    {
        var rules = ScheduleRules.ToList();
        bool hasMatch = false;
        while (nextOccurrenceInTz < maxLookaheadInTz && !hasMatch)
        {
            if (AllMatch(nextOccurrenceInTz, rules))
            {
                hasMatch = true;
                break;
            }

            var timeUnitToAdvance = GetTimeToAdvanceToNextOccurence(nextOccurrenceInTz);
            var advanced = AdvanceNextOccurrence(nextOccurrenceInTz, timeUnitToAdvance.Item2, timeUnitToAdvance.Item1);
            nextOccurrenceInTz = RestartEveryAtPeriodStart(advanced, timeUnitToAdvance.Item2, nextOccurrenceInTz, everyRule);
        }

        return (nextOccurrenceInTz, hasMatch);
    }

    private static readonly NaturalCronTimeUnit[] RestartableTimeUnits =
    [
        NaturalCronTimeUnit.Second,
        NaturalCronTimeUnit.Minute,
        NaturalCronTimeUnit.Hour,
        NaturalCronTimeUnit.Day,
        NaturalCronTimeUnit.Month
    ];

    /// <summary>
    /// After jumping forward by <paramref name="advancedUnit"/> the every rule restarts at the beginning of the
    /// period it landed in, otherwise the first occurrences of that period are skipped.
    /// e.g. "every hour on wednesday" from Tuesday 10:30: the jump to Wednesday lands at 10:30, it must restart at 00:30.
    /// The units from the every unit up to the advanced unit are reset; the units below the every unit are kept.
    /// </summary>
    private DateTime RestartEveryAtPeriodStart(DateTime advanced, NaturalCronTimeUnit advancedUnit, DateTime previous,
        NaturalCronEveryXRule? everyRule)
    {
        // Weeks are not aligned with the other calendar units, so there is no period start to go back to.
        if (everyRule == null || everyRule.TimeUnit == NaturalCronTimeUnit.Week || advanced >= DateTime.MaxValue)
        {
            return advanced;
        }

        if (advancedUnit == NaturalCronTimeUnit.Week)
        {
            advancedUnit = NaturalCronTimeUnit.Day;
        }

        var restarted = advanced;
        foreach (var timeUnit in RestartableTimeUnits)
        {
            if (timeUnit >= everyRule.TimeUnit && timeUnit < advancedUnit)
            {
                restarted = DateTimeUtil.SetPartValue(timeUnit, restarted, ExpressionUtil.GetMinValueByTimeUnit(timeUnit));
            }
        }

        if (restarted == advanced)
        {
            return advanced;
        }

        if (!string.IsNullOrEmpty(everyRule.AnchoredValue))
        {
            // Anchored values restart in every period, e.g. every 15 minutes anchored on 5: 05, 20, 35, 50.
            var matchableRules = ScheduleRules.OfType<NaturalCronMatchableRule>().ToList();
            restarted = everyRule.ApplyAnchoredValueIfPresent(restarted, restarted, matchableRules);
        }

        // Never go back to an already checked time.
        return restarted > previous ? restarted : advanced;
    }

    // Top-level rules plus the rules wrapped by except and by mixed lists (any-of), without the wrappers themselves.
    private IEnumerable<NaturalCronRule> RulesIncludingExceptInnerRules()
    {
        return Rules.SelectMany(Unwrap);
    }

    // The except and any-of rules, at any depth.
    private static IEnumerable<NaturalCronMatchableRule> GetWrapperRules(NaturalCronRule rule)
    {
        var innerRules = (rule as NaturalCronExceptRule)?.Rules ?? (rule as NaturalCronAnyOfMatchableRule)?.Rules;
        if (innerRules == null)
        {
            return Array.Empty<NaturalCronMatchableRule>();
        }

        return new[] { (NaturalCronMatchableRule)rule }.Concat(innerRules.SelectMany(GetWrapperRules));
    }

    private static IEnumerable<NaturalCronRule> Unwrap(NaturalCronRule rule)
    {
        if (rule is NaturalCronExceptRule exceptRule)
        {
            return exceptRule.Rules.SelectMany(Unwrap);
        }

        if (rule is NaturalCronAnyOfMatchableRule anyOfRule)
        {
            return anyOfRule.Rules.SelectMany(Unwrap);
        }

        return new[] { rule };
    }

    private static DateTime AdvanceNextOccurrence(DateTime nextOccurrenceInTz, NaturalCronTimeUnit timeUnitToAdvance, int amount)
    {
        return DateTimeUtil.AdvanceTimeSafety(timeUnitToAdvance, nextOccurrenceInTz, amount);
    }
}