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
    /// <param name="utcBaseTime">The base time in UTC from which to find the next occurrence. A DateTimeKind.Unspecified value is treated as UTC</param>
    /// <param name="utcMaxLookahead">Maximum lookahead time in UTC. If null, defaults to DateTime.MaxValue</param>
    /// <returns>The next occurrence in UTC, or null if no occurrence is found</returns>
    /// <exception cref="ArgumentException">Thrown when the base time or the max lookahead is DateTimeKind.Local</exception>
    public DateTime? TryGetNextOccurrenceInUtc(DateTime utcBaseTime, DateTime? utcMaxLookahead = null)
    {
        ValidateUtcInputs(utcBaseTime, utcMaxLookahead);

        var methodTz = TimeZoneInfo.Utc;
        var computeTz = GetComputeTz(methodTz);

        // Computed in the zone of the tz rule, or in UTC without one. Always returned in UTC.
        return InternalTryGetNextOccurrence(
            utcBaseTime,
            utcMaxLookahead,
            methodTz,
            computeTz,
            TimeZoneInfo.Utc,
            DateTimeKind.Utc);
    }

    /// <summary>
    /// Tries to get the next occurrence using the current thread's timezone. Returns null if no occurrence is found within the specified lookahead period.
    /// </summary>
    /// <param name="baseTime">The base time in the current thread's timezone from which to find the next occurrence. A DateTimeKind.Utc value is in UTC</param>
    /// <param name="maxLookahead">Maximum lookahead time, in the same timezone as the base time. If null, defaults to DateTime.MaxValue</param>
    /// <returns>The next occurrence in the timezone and with the Kind of the base time, or null if no occurrence is found</returns>
    public DateTime? TryGetNextOccurrence(DateTime baseTime, DateTime? maxLookahead = null)
    {
        // A UTC base time is in UTC, any other in the zone of the machine.
        var methodTz = baseTime.Kind == DateTimeKind.Utc ? TimeZoneInfo.Utc : TimeZoneInfo.Local;
        var computeTz = GetComputeTz(methodTz);
        var resultTz = methodTz;
        var resultKind = baseTime.Kind;

        // The result keeps the zone and the kind of the base time, with or without a tz rule.
        // Without a tz rule, it also makes the next call (e.g. TryGetNextOccurrences) evaluate the custom keywords in that same zone.
        return InternalTryGetNextOccurrence(
            baseTime,
            maxLookahead,
            methodTz,
            computeTz,
            resultTz,
            resultKind);
    }

    /// <summary>
    /// Tries to get the next occurrence in a specified IANA timezone. Returns null if no occurrence is found within the specified lookahead period.
    /// </summary>
    /// <param name="baseTimeInTz">The base time interpreted in the specified IANA timezone from which to find the next occurrence.
    /// A DateTimeKind.Utc value requires a UTC timezone and a DateTimeKind.Local value the timezone of the machine</param>
    /// <param name="ianaTzId">IANA timezone ID (case-insensitive, e.g., "America/New_York" or "europe/lisbon")</param>
    /// <param name="maxLookaheadInTz">Maximum lookahead time in the specified timezone. If null, defaults to DateTime.MaxValue</param>
    /// <returns>The next occurrence in the specified timezone, with the Kind of the base time, or null if no occurrence is found</returns>
    /// <exception cref="ArgumentException">Thrown when the timezone ID is not valid, or when the Kind of the base time
    /// or of the max lookahead is Utc or Local and does not match the timezone</exception>
    public DateTime? TryGetNextOccurrenceInTz(DateTime baseTimeInTz, string ianaTzId, DateTime? maxLookaheadInTz = null)
    {
        var tz = ResolveAndValidateTz(baseTimeInTz, ianaTzId, maxLookaheadInTz);

        // A UTC or a local base time is in the same zone as tz (checked above).
        var methodTz = baseTimeInTz.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.Utc,
            DateTimeKind.Local => TimeZoneInfo.Local,
            _ => tz
        };
        var computeTz = GetComputeTz(methodTz);

        // Interpret the input in the specified timezone, not Local, and return in the same timezone and kind.
        return InternalTryGetNextOccurrence(
            baseTimeInTz,
            maxLookaheadInTz,
            methodTz,
            computeTz,
            methodTz,
            baseTimeInTz.Kind);
    }

    /// <summary>
    /// Tries to get the next occurrences in UTC. Returns as many occurrences as found, up to the specified count.
    /// Stops when no more occurrences are found or the count is reached.
    /// </summary>
    /// <param name="utcBaseTime">The base time in UTC from which to find the next occurrences</param>
    /// <param name="count">The number of occurrences to retrieve</param>
    /// <param name="utcMaxLookahead">Maximum lookahead time in UTC. If null, defaults to DateTime.MaxValue</param>
    /// <returns>A list of occurrences in UTC (may contain fewer than the requested count)</returns>
    /// <exception cref="ArgumentException">Thrown when the base time or the max lookahead is DateTimeKind.Local</exception>
    public IList<DateTime> TryGetNextOccurrencesInUtc(DateTime utcBaseTime, int count, DateTime? utcMaxLookahead = null)
    {
        ValidateUtcInputs(utcBaseTime, utcMaxLookahead);

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
    /// Tries to get the next occurrences in a specified IANA timezone. Returns as many occurrences as found, up to the specified count.
    /// Stops when no more occurrences are found or the count is reached.
    /// </summary>
    /// <param name="baseTimeInTz">The base time interpreted in the specified IANA timezone from which to find the next occurrences</param>
    /// <param name="count">The number of occurrences to retrieve</param>
    /// <param name="ianaTzId">IANA timezone ID (case-insensitive, e.g., "America/New_York" or "europe/lisbon")</param>
    /// <param name="maxLookaheadInTz">Maximum lookahead time in the specified timezone. If null, defaults to DateTime.MaxValue</param>
    /// <returns>A list of occurrences in the specified timezone (may contain fewer than the requested count)</returns>
    /// <exception cref="ArgumentException">Thrown when the timezone ID is not valid, or when the Kind of the base time
    /// or of the max lookahead is Utc or Local and does not match the timezone</exception>
    public IList<DateTime> TryGetNextOccurrencesInTz(DateTime baseTimeInTz, int count, string ianaTzId, DateTime? maxLookaheadInTz = null)
    {
        ResolveAndValidateTz(baseTimeInTz, ianaTzId, maxLookaheadInTz);

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

    private static void ValidateUtcInputs(DateTime utcBaseTime, DateTime? utcMaxLookahead)
    {
        // A local time is not a UTC time. Unspecified is trusted to be UTC.
        if (utcBaseTime.Kind == DateTimeKind.Local)
        {
            throw new ArgumentException("The base time must be in UTC, but its Kind is Local.", nameof(utcBaseTime));
        }

        if (utcMaxLookahead?.Kind == DateTimeKind.Local)
        {
            throw new ArgumentException("The max lookahead must be in UTC, but its Kind is Local.", nameof(utcMaxLookahead));
        }
    }

    private static TimeZoneInfo ResolveAndValidateTz(DateTime baseTimeInTz, string ianaTzId, DateTime? maxLookaheadInTz)
    {
        // Case-insensitive validation
        if (!TZConvert.KnownIanaTimeZoneNames
                .Contains(ianaTzId, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid IANA time zone ID: {ianaTzId}");
        }

        var tz = TZConvert.GetTimeZoneInfo(ianaTzId);
        EnsureKindMatchesTz(baseTimeInTz, tz, ianaTzId, nameof(baseTimeInTz));
        if (maxLookaheadInTz.HasValue)
        {
            EnsureKindMatchesTz(maxLookaheadInTz.Value, tz, ianaTzId, nameof(maxLookaheadInTz));
        }

        return tz;
    }

    // A UTC time must be in a UTC zone and a local time in the zone of the machine. Aliases count as the same zone.
    private static void EnsureKindMatchesTz(DateTime dateTime, TimeZoneInfo tz, string ianaTzId, string paramName)
    {
        var (kindTz, kindName) = dateTime.Kind switch
        {
            DateTimeKind.Utc => (TimeZoneInfo.Utc, "UTC"),
            DateTimeKind.Local => (TimeZoneInfo.Local, "the local time zone of the machine"),
            _ => ((TimeZoneInfo?)null, string.Empty)
        };

        if (kindTz != null && tz.Id != kindTz.Id && !tz.HasSameRules(kindTz))
        {
            throw new ArgumentException(
                $"The Kind of the time is {dateTime.Kind}, so the time zone must be {kindName}, but it is '{ianaTzId}'. " +
                "Pass a DateTimeKind.Unspecified time to give a wall-clock time in that time zone.", paramName);
        }
    }

    // The every rule is an hour or less, e.g. "every 30 minutes on sunday".
    private bool HasSubDayEveryRule()
    {
        var everyRule = Rules.OfType<NaturalCronEveryXRule>().FirstOrDefault();
        return everyRule != null && everyRule.TimeUnit <= NaturalCronTimeUnit.Hour;
    }

    // Only a sub-day every rule (and maybe a tz rule), e.g. "every 30 minutes":
    // it's just elapsed time, so it is computed in UTC.
    private bool IsOnlySubDayInterval()
    {
        var everyRule = Rules.OfType<NaturalCronEveryXRule>().FirstOrDefault();
        return HasSubDayEveryRule()
               && string.IsNullOrEmpty(everyRule?.AnchoredValue)
               && Rules.All(r => r is NaturalCronEveryXRule or NaturalCronIanaTimeZoneRule);
    }

    // A tz rule always wins over the zone of the method (except sub-day intervals with no other rules, which are timezone-independent).
    private TimeZoneInfo GetComputeTz(TimeZoneInfo methodTz) =>
        IsOnlySubDayInterval() ? TimeZoneInfo.Utc : (TimeZoneRule()?.TimeZoneInfo ?? methodTz);

    /// <summary>
    /// The single flow of all the next occurrence methods.
    /// </summary>
    private DateTime? InternalTryGetNextOccurrence(
        DateTime baseTime, 
        DateTime? maxLookahead, 
        TimeZoneInfo inputTz,
        TimeZoneInfo computeTz, 
        TimeZoneInfo resultTz, 
        DateTimeKind resultKind)
    {
        // 1. Input to exact DateTimeOffset in the compute zone
        var baseTimeInComputeTz = ToOffsetInZone(baseTime, inputTz, computeTz);
        var maxLookaheadInComputeTz = maxLookahead.HasValue
            ? ToOffsetInZone(maxLookahead.Value, inputTz, computeTz)
            : (DateTimeOffset?)null;

        // 2. Search in the compute zone (handles except, spring-forward, and fall-back)
        var resultInComputeTz = InternalTryGetNextOccurrenceInTz(baseTimeInComputeTz, maxLookaheadInComputeTz, computeTz);
        if (resultInComputeTz == null)
        {
            return null;
        }

        // 3. Result to the zone and the kind the caller wants
        var resultDateTime = ToResultDateTime(resultInComputeTz.Value, resultTz, resultKind);

        if (resultKind != DateTimeKind.Unspecified)
        {
            return resultDateTime;
        }

        if (!ReferenceEquals(inputTz, resultTz))
        {
            return resultDateTime;
        }

        // When returning an Unspecified wall-clock time in the same zone as the input (e.g. TryGetNextOccurrenceInTz),
        // the returned DateTime must strictly advance past baseTime; otherwise a repeated fall-back hour wraps the
        // wall-clock backwards (e.g. 01:30 -> 01:00) and chained singular/plural calls would loop forever.
        while (resultDateTime <= baseTime)
        {
            resultInComputeTz = InternalTryGetNextOccurrenceInTz(resultInComputeTz.Value, maxLookaheadInComputeTz, computeTz);
            if (resultInComputeTz == null)
            {
                return null;
            }

            resultDateTime = ToResultDateTime(resultInComputeTz.Value, resultTz, resultKind);
        }

        return resultDateTime;
    }

    // Finds the first valid second after a spring-forward gap (e.g. 02:30 -> 03:00 in New York, 02:15 -> 02:30 in Lord Howe).
    private static DateTime MoveToFirstValidTime(DateTime dateTime, TimeZoneInfo timeZone)
    {
        if (ReferenceEquals(timeZone, TimeZoneInfo.Utc))
        {
            return dateTime;
        }

        var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        if (!timeZone.IsInvalidTime(unspecified))
        {
            return dateTime;
        }

        var lo = new DateTime(unspecified.Year, unspecified.Month, unspecified.Day,
            unspecified.Hour, unspecified.Minute, unspecified.Second);
        var gap = timeZone.GetUtcOffset(lo.AddDays(1)) - timeZone.GetUtcOffset(lo.AddDays(-1));
        var hi = gap > TimeSpan.Zero ? lo + gap : lo.AddHours(1);
        while (timeZone.IsInvalidTime(hi))
        {
            hi = hi.AddHours(1);
        }

        // Binary search to the exact second the gap ends (~12 iterations for a 1-hour gap).
        while ((hi - lo).Ticks > TimeSpan.TicksPerSecond)
        {
            var midSeconds = (hi - lo).Ticks / TimeSpan.TicksPerSecond / 2;
            var mid = lo.AddSeconds(midSeconds);
            if (timeZone.IsInvalidTime(mid))
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        return hi;
    }

    private static DateTimeOffset ToOffsetInZone(DateTime dateTime, TimeZoneInfo fromTz, TimeZoneInfo toTz)
    {
        if (dateTime == DateTime.MaxValue)
        {
            return DateTimeOffset.MaxValue;
        }

        dateTime = MoveToFirstValidTime(dateTime, fromTz);

        DateTimeOffset sourceOffset;
        if (ReferenceEquals(fromTz, TimeZoneInfo.Utc))
        {
            sourceOffset = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
        }
        else if (dateTime.Kind == DateTimeKind.Local && ReferenceEquals(fromTz, TimeZoneInfo.Local))
        {
            // Preserves .NET's internal IsAmbiguousDst flag on Local DateTimes.
            sourceOffset = new DateTimeOffset(dateTime);
        }
        else
        {
            // Ambiguous Unspecified wall-clock inputs default to the 1st pass (earlier instant) so a run is never skipped.
            sourceOffset = ToFirstPassOffset(dateTime, fromTz);
        }

        return TimeZoneInfo.ConvertTime(sourceOffset, toTz);
    }

    // Converts a valid wall-clock time in tz to a DateTimeOffset, preferring the 1st pass (earlier instant) on fall-back nights.
    private static DateTimeOffset ToFirstPassOffset(DateTime wall, TimeZoneInfo tz)
    {
        var unspecified = DateTime.SpecifyKind(wall, DateTimeKind.Unspecified);
        if (tz.IsAmbiguousTime(unspecified))
        {
            var offsets = tz.GetAmbiguousTimeOffsets(unspecified);
            var firstPassOffset = offsets[0] >= offsets[1] ? offsets[0] : offsets[1];
            return new DateTimeOffset(unspecified, firstPassOffset);
        }

        return new DateTimeOffset(unspecified, tz.GetUtcOffset(unspecified));
    }

    private static DateTime ToResultDateTime(DateTimeOffset value, TimeZoneInfo resultTz, DateTimeKind resultKind)
    {
        return resultKind switch
        {
            DateTimeKind.Utc => value.UtcDateTime,
            DateTimeKind.Local => value.LocalDateTime,
            _ => TimeZoneInfo.ConvertTime(value, resultTz).DateTime
        };
    }

    private static DateTime AddHoursClamped(DateTime dateTime, int hours)
    {
        var ticksToAdd = hours * TimeSpan.TicksPerHour;
        return dateTime.Ticks > DateTime.MaxValue.Ticks - ticksToAdd
            ? DateTime.MaxValue
            : dateTime.AddHours(hours);
    }

    private DateTimeOffset? InternalTryGetNextOccurrenceInTz(DateTimeOffset baseTimeInTz, DateTimeOffset? maxLookaheadInTz, TimeZoneInfo timeZone)
    {
        if (maxLookaheadInTz.HasValue && baseTimeInTz >= maxLookaheadInTz.Value)
        {
            return null;
        }

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

        var exceptRules = Rules.OfType<NaturalCronExceptRule>().ToList();
        var isUtc = ReferenceEquals(timeZone, TimeZoneInfo.Utc);

        // Buffer the wall-clock lookahead on non-UTC zones so a fall-back transition (where a later instant
        // has an earlier wall-clock hour) never cuts the search short; exact instant limit is enforced below.
        var maxLookaheadWall = !maxLookaheadInTz.HasValue || maxLookaheadInTz.Value == DateTimeOffset.MaxValue
            ? DateTime.MaxValue
            : isUtc
                ? maxLookaheadInTz.Value.DateTime
                : AddHoursClamped(maxLookaheadInTz.Value.DateTime, 2);

        var wallCursor = baseTimeInTz.DateTime;
        while (wallCursor < maxLookaheadWall)
        {
            DateTime? candidate;
            try
            {
                candidate = InternalTryGetNextScheduleOccurrenceInTz(wallCursor, maxLookaheadWall);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Every occurrence up to DateTime.MaxValue was excluded.
                return null;
            }

            if (candidate == null)
            {
                return null;
            }

            var wall = candidate.Value;

            // Except rules remove occurrences from the schedule, they don't shift it:
            // e.g "every hour except 22:00" skips 22:00 and continues at 23:00 rather than 22:01.
            if (exceptRules.Any(x => !x.Match(wall)))
            {
                wallCursor = wall;
                continue;
            }

            if (isUtc)
            {
                return new DateTimeOffset(wall, TimeSpan.Zero);
            }

            // Spring-forward gap: sub-day intervals advance past the gap to keep their cadence;
            // fixed / daily+ schedules move forward to the first valid moment after the gap (e.g. 02:30 -> 03:00).
            if (timeZone.IsInvalidTime(wall))
            {
                if (HasSubDayEveryRule())
                {
                    wallCursor = wall;
                    continue;
                }

                wall = MoveToFirstValidTime(wall, timeZone);
            }

            // Fall-back repeated hour: resolve ambiguous times to the 1st pass (earlier instant).
            // If that 1st pass already happened <= baseTimeInTz (e.g. baseTime is in the 2nd pass), keep searching.
            var candidateOffset = ToFirstPassOffset(wall, timeZone);
            if (candidateOffset <= baseTimeInTz)
            {
                wallCursor = wall;
                continue;
            }

            return !maxLookaheadInTz.HasValue || candidateOffset < maxLookaheadInTz.Value
                ? candidateOffset
                : null;
        }

        return null;
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