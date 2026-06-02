using NaturalCron.Builder;
using NaturalCron.CronConverter.Internal;
using NaturalCron.CronConverter.Internal.Strategies;
using NaturalCron.Rules;
using NaturalCron.Utils;

namespace NaturalCron.CronConverter;

public static class NaturalCronToCronConverter
{
    // January 2024 (31 days) — stable reference for "First"/"Last" range resolution.
    private static readonly DateTime ReferenceDate = new(2024, 1, 1);

    // ── Public API ─────────────────────────────────────────────────────────────

    public static string Convert(string expression, CronConverterOptions? options = null)
        => Convert(NaturalCronExpr.Parse(expression), options);

    public static string Convert(NaturalCronExpr expr, CronConverterOptions? options = null)
    {
        options ??= CronConverterOptions.Default;
        var errors = new List<string>();
        var result = BuildCronExpression(expr, options, errors);

        if (errors.Count > 0 && options.NonConvertibleBehavior == NonConvertibleBehavior.ThrowException)
            throw new CronConversionException(errors);

        return result;
    }

    public static CronConversionResult TryConvert(string expression, CronConverterOptions? options = null)
    {
        var (expr, parseErrors) = NaturalCronExpr.TryParse(expression);
        if (expr == null)
            return new CronConversionResult(null, parseErrors.ToList());

        return TryConvert(expr, options); // ← not collapsible: two returns with different types
    }

    public static CronConversionResult TryConvert(NaturalCronExpr expr, CronConverterOptions? options = null)
    {
        options ??= CronConverterOptions.Default;
        var errors = new List<string>();
        var cronExpr = BuildCronExpression(expr, options, errors);
        return new CronConversionResult(cronExpr, errors);
    }

    // ── Core Builder ───────────────────────────────────────────────────────────

    private static string BuildCronExpression(NaturalCronExpr expr, CronConverterOptions options, List<string> errors)
    {
        var fields = new CronFields();
        var closestWeekdayStrategy = StrategyFactory.Create(options.ClosestWeekdaySupport);
        var nthWeekdayStrategy = StrategyFactory.Create(options.NthWeekdaySupport);

        foreach (var rule in expr.Rules)
        {
            switch (rule)
            {
                case NaturalCronEveryXRule everyX:
                    ProcessEveryX(everyX, fields, options, errors);
                    break;
                case NaturalCronAtInOnMultiplesRule multiples:
                    ProcessAtInOnMultiples(multiples, fields, options, closestWeekdayStrategy, nthWeekdayStrategy, errors);
                    break;
                case NaturalCronAtInOnRule atInOn:
                    ProcessAtInOn(atInOn, fields, options, closestWeekdayStrategy, nthWeekdayStrategy, errors);
                    break;
                case NaturalCronBetweenMultiplesRule betweenMultiples:
                    ProcessBetweenMultiples(betweenMultiples, errors);
                    break;
                case NaturalCronBetweenRule between:
                    ProcessBetween(between, fields, options, errors);
                    break;
                case NaturalCronIanaTimeZoneRule timezone:
                    errors.Add($"Timezone '{timezone.IanaId}' is not supported in classic cron and was omitted.");
                    break;
            }
        }

        return fields.Build(options.SupportSeconds, options.DomDowMutualExclusion);
    }

    // ── EveryX Rule ────────────────────────────────────────────────────────────

    private static void ProcessEveryX(NaturalCronEveryXRule rule, CronFields fields, CronConverterOptions options, List<string> errors)
    {
        if (!string.IsNullOrEmpty(rule.AnchoredValue))
            errors.Add($"AnchoredOn/AnchoredAt is not supported in classic cron and was omitted ('{rule.FullExpression}').");

        var step = rule.Value == 1 ? "*" : $"*/{rule.Value}";

        switch (rule.TimeUnit)
        {
            case NaturalCronTimeUnit.Second:
                if (!options.SupportSeconds)
                {
                    errors.Add($"Seconds field is not enabled ('{rule.FullExpression}'). Set SupportSeconds = true to include it.");
                    return;
                }
                fields.Second = step;
                fields.SecondSet = true;
                break;

            case NaturalCronTimeUnit.Minute:
                fields.Minute = step;
                fields.MinuteSet = true;
                break;

            case NaturalCronTimeUnit.Hour:
                fields.Hour = step;
                fields.HourStepSet = true;
                break;

            case NaturalCronTimeUnit.Day:
                fields.Dom = step;
                fields.DomSet = true;
                break;

            case NaturalCronTimeUnit.Week:
                // every 1 week: the DOW AtInOn rule provides the actual constraint.
                // every N weeks (N>1): no cron equivalent.
                if (rule.Value > 1)
                    errors.Add($"'every {rule.Value} week(s)' cannot be expressed in classic cron — there is no week field ('{rule.FullExpression}').");
                break;

            case NaturalCronTimeUnit.Month:
                fields.Month = step;
                break;

            case NaturalCronTimeUnit.Year:
                errors.Add($"'every {rule.Value} year(s)' cannot be expressed in standard cron ('{rule.FullExpression}').");
                break;

            case NaturalCronTimeUnit.TimeZone:
                break;
        }
    }

    // ── AtInOn Rule ────────────────────────────────────────────────────────────

    private static void ProcessAtInOn(
        NaturalCronAtInOnRule rule,
        CronFields fields,
        CronConverterOptions options,
        IClosestWeekdayStrategy closestWeekdayStrategy,
        INthWeekdayStrategy nthWeekdayStrategy,
        List<string> errors)
    {
        switch (rule.TimeUnit)
        {
            case NaturalCronTimeUnit.Second:
                if (!options.SupportSeconds)
                {
                    errors.Add($"Seconds field not enabled ('{rule.FullExpression}'). Set SupportSeconds = true.");
                    return;
                }
                var secVal = ExpressionUtil.TryParseToInt(NaturalCronTimeUnit.Second, rule.InnerExpression);
                if (secVal.HasValue) { fields.Second = secVal.Value.ToString(); fields.SecondSet = true; }
                break;

            case NaturalCronTimeUnit.Minute:
                var minVal = ExpressionUtil.TryParseToInt(NaturalCronTimeUnit.Minute, rule.InnerExpression);
                if (minVal.HasValue) { fields.Minute = minVal.Value.ToString(); fields.MinuteSet = true; }
                break;

            case NaturalCronTimeUnit.Hour:
                var hrVal = ExpressionUtil.TryParseToInt(NaturalCronTimeUnit.Hour, rule.InnerExpression);
                if (hrVal.HasValue) fields.Hour = hrVal.Value.ToString();
                break;

            case NaturalCronTimeUnit.Day:
                SetDomFromExpression(rule.InnerExpression, rule.FullExpression, fields, closestWeekdayStrategy, nthWeekdayStrategy, options.WeekFormat, errors);
                break;

            case NaturalCronTimeUnit.Week:
                var dowVal = ExpressionUtil.TryParseToInt(NaturalCronTimeUnit.Week, rule.InnerExpression);
                if (dowVal.HasValue)
                {
                    fields.Dow = WeekdayFormatter.Format((NaturalCronDayOfWeek)dowVal.Value, options.WeekFormat);
                    fields.DowSet = true;
                }
                break;

            case NaturalCronTimeUnit.Month:
                var mthVal = ExpressionUtil.TryParseToInt(NaturalCronTimeUnit.Month, rule.InnerExpression);
                if (mthVal.HasValue)
                    fields.Month = MonthFormatter.Format((NaturalCronMonth)mthVal.Value, options.MonthFormat);
                break;

            case NaturalCronTimeUnit.Year:
                errors.Add($"Year rules cannot be expressed in standard cron ('{rule.FullExpression}').");
                break;
        }
    }

    // ── AtInOn Multiples Rule ──────────────────────────────────────────────────

    private static void ProcessAtInOnMultiples(
        NaturalCronAtInOnMultiplesRule rule,
        CronFields fields,
        CronConverterOptions options,
        IClosestWeekdayStrategy closestWeekdayStrategy,
        INthWeekdayStrategy nthWeekdayStrategy,
        List<string> errors)
    {
        if (rule.SecondRules.Length > 0)
        {
            if (!options.SupportSeconds)
                errors.Add($"Seconds field not enabled ('{rule.FullExpression}'). Set SupportSeconds = true.");
            else
            {
                fields.Second = JoinIntValues(rule.SecondRules, NaturalCronTimeUnit.Second);
                fields.SecondSet = true;
            }
        }

        if (rule.MinuteRules.Length > 0)
        {
            fields.Minute = JoinIntValues(rule.MinuteRules, NaturalCronTimeUnit.Minute);
            fields.MinuteSet = true;
        }

        if (rule.HourRules.Length > 0)
            fields.Hour = JoinIntValues(rule.HourRules, NaturalCronTimeUnit.Hour);

        if (rule.DayRules.Length > 0)
        {
            var parts = new List<string>();
            foreach (var dayRule in rule.DayRules)
                CollectDomPart(dayRule.InnerExpression, rule.FullExpression, fields, closestWeekdayStrategy, nthWeekdayStrategy, options.WeekFormat, parts, errors);

            if (parts.Count > 0)
            {
                fields.Dom = string.Join(",", parts);
                fields.DomSet = true;
            }
        }

        if (rule.WeekRules.Length > 0)
        {
            var parts = rule.WeekRules
                .Select(r => ExpressionUtil.TryParseToInt(NaturalCronTimeUnit.Week, r.InnerExpression))
                .Where(v => v.HasValue)
                .Select(v => WeekdayFormatter.Format((NaturalCronDayOfWeek)v!.Value, options.WeekFormat));
            fields.Dow = string.Join(",", parts);
            fields.DowSet = true;
        }

        if (rule.MonthRules.Length > 0)
        {
            var parts = rule.MonthRules
                .Select(r => ExpressionUtil.TryParseToInt(NaturalCronTimeUnit.Month, r.InnerExpression))
                .Where(v => v.HasValue)
                .Select(v => MonthFormatter.Format((NaturalCronMonth)v!.Value, options.MonthFormat));
            fields.Month = string.Join(",", parts);
        }

        if (rule.YearRules.Length > 0)
            errors.Add($"Year rules cannot be expressed in standard cron ('{rule.FullExpression}').");
    }

    // ── Between Rule ──────────────────────────────────────────────────────────

    private static void ProcessBetween(NaturalCronBetweenRule rule, CronFields fields, CronConverterOptions options, List<string> errors)
    {
        // A range with multiple simultaneous time units (e.g. "09:00" produces Hour+Minute)
        // represents a time window with no cron equivalent. Single-unit ranges (hour-only,
        // month-only, etc.) map directly to a cron field range.
        if (rule.StartEndValues.Count > 1)
        {
            errors.Add($"Between range with multiple time units ('{rule.FullExpression}') cannot be expressed in classic cron and was omitted.");
            return;
        }

        foreach (var kv in rule.StartEndValues)
        {
            var timeUnit = kv.Key;
            var startEnd = kv.Value;

            switch (timeUnit)
            {
                case NaturalCronTimeUnit.Hour:
                    var startHour = ResolveValue(NaturalCronTimeUnit.Hour, startEnd.StartExpr);
                    var endHour = ResolveValue(NaturalCronTimeUnit.Hour, startEnd.EndExpr);
                    if (startHour.HasValue && endHour.HasValue)
                        fields.Hour = $"{startHour.Value}-{endHour.Value}";
                    break;

                case NaturalCronTimeUnit.Week:
                    var startDow = ResolveValue(NaturalCronTimeUnit.Week, startEnd.StartExpr);
                    var endDow = ResolveValue(NaturalCronTimeUnit.Week, startEnd.EndExpr);
                    if (startDow.HasValue && endDow.HasValue)
                    {
                        fields.Dow = $"{WeekdayFormatter.Format((NaturalCronDayOfWeek)startDow.Value, options.WeekFormat)}-{WeekdayFormatter.Format((NaturalCronDayOfWeek)endDow.Value, options.WeekFormat)}";
                        fields.DowSet = true;
                    }
                    break;

                case NaturalCronTimeUnit.Month:
                    var startMonth = ResolveValue(NaturalCronTimeUnit.Month, startEnd.StartExpr);
                    var endMonth = ResolveValue(NaturalCronTimeUnit.Month, startEnd.EndExpr);
                    if (startMonth.HasValue && endMonth.HasValue)
                        fields.Month = $"{MonthFormatter.Format((NaturalCronMonth)startMonth.Value, options.MonthFormat)}-{MonthFormatter.Format((NaturalCronMonth)endMonth.Value, options.MonthFormat)}";
                    break;

                case NaturalCronTimeUnit.Day:
                    var startDay = ResolveValue(NaturalCronTimeUnit.Day, startEnd.StartExpr);
                    var endDay = ResolveValue(NaturalCronTimeUnit.Day, startEnd.EndExpr);
                    if (startDay.HasValue && endDay.HasValue)
                    {
                        fields.Dom = $"{startDay.Value}-{endDay.Value}";
                        fields.DomSet = true;
                    }
                    break;
            }
        }
    }

    private static void ProcessBetweenMultiples(NaturalCronBetweenMultiplesRule rule, List<string> errors)
        => errors.Add($"Multiple time ranges '{rule.FullExpression}' cannot be expressed in classic cron and were omitted.");

    // ── DOM Helpers ────────────────────────────────────────────────────────────

    private static void SetDomFromExpression(
        string innerExpr,
        string fullExpr,
        CronFields fields,
        IClosestWeekdayStrategy closestWeekdayStrategy,
        INthWeekdayStrategy nthWeekdayStrategy,
        WeekFormat weekFormat,
        List<string> errors)
    {
        var parts = new List<string>();
        CollectDomPart(innerExpr, fullExpr, fields, closestWeekdayStrategy, nthWeekdayStrategy, weekFormat, parts, errors);
        if (parts.Count > 0)
        {
            fields.Dom = string.Join(",", parts);
            fields.DomSet = true;
        }
    }

    private static void CollectDomPart(
        string innerExpr,
        string fullExpr,
        CronFields fields,
        IClosestWeekdayStrategy closestWeekdayStrategy,
        INthWeekdayStrategy nthWeekdayStrategy,
        WeekFormat weekFormat,
        List<string> parts,
        List<string> errors)
    {
        // Plain integer
        var plain = ExpressionUtil.TryParseToInt(NaturalCronTimeUnit.Day, innerExpr);
        if (plain.HasValue) { parts.Add(plain.Value.ToString()); return; }

        // FirstDay variants
        if (KeywordsConstants.FirstDayOfTheMonth.ToUpper().Any(k => innerExpr.ToUpper().ContainsWholeWord(k))
            || KeywordsConstants.First.ToUpper().Any(k => string.Equals(innerExpr, k, StringComparison.OrdinalIgnoreCase)))
        {
            var resolved = ExpressionUtil.TryGetValueForMatch(NaturalCronTimeUnit.Day, ReferenceDate, innerExpr);
            parts.Add(resolved.HasValue ? resolved.Value.ToString() : "1");
            return;
        }

        // LastDay variants — month-dependent, not representable in static cron
        if (KeywordsConstants.LastDayOfTheMonth.ToUpper().Any(k => innerExpr.ToUpper().ContainsWholeWord(k))
            || KeywordsConstants.Last.ToUpper().Any(k => string.Equals(innerExpr, k, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add($"LastDay ('{fullExpr}') is month-dependent and cannot be expressed in standard cron.");
            return;
        }

        // LastWeekday / FirstWeekday
        if (innerExpr.Equals("LastWeekday", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"LastWeekday ('{fullExpr}') cannot be expressed in standard cron.");
            return;
        }

        if (innerExpr.Equals("FirstWeekday", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"FirstWeekday ('{fullExpr}') cannot be expressed in standard cron.");
            return;
        }

        // ClosestWeekdayTo N
        if (innerExpr.StartsWith("ClosestWeekdayTo", StringComparison.OrdinalIgnoreCase))
        {
            var cwParts = innerExpr.Trim().Split(' ');
            if (cwParts.Length == 2 && int.TryParse(cwParts[1], out var cwDay))
            {
                var formatted = closestWeekdayStrategy.Format(cwDay);
                if (formatted != null) { parts.Add(formatted); return; }
            }
            errors.Add($"ClosestWeekdayTo ('{fullExpr}') cannot be expressed in standard cron. Use ClosestWeekdaySupport.NearestWeekdayW for W notation.");
            return;
        }

        // NthWeekday (1stMon, 2ndFri, LastMon …) — maps to DOW field via Quartz notation
        if (TryParseNthWeekday(innerExpr, out var nth, out var dow))
        {
            var formatted = nthWeekdayStrategy.Format(nth, dow, weekFormat);
            if (formatted != null)
            {
                fields.Dow = fields.DowSet ? fields.Dow + "," + formatted : formatted;
                fields.DowSet = true;
                return;
            }
            errors.Add($"Nth weekday ('{fullExpr}') cannot be expressed in standard cron. Use NthWeekdaySupport.NthHash for # notation.");
            return;
        }

        // Day arithmetic (LastDay-1, FirstDay+2) — dynamic, month-dependent
        if (innerExpr.Contains('+') || innerExpr.Contains('-'))
        {
            errors.Add($"Day arithmetic expression ('{fullExpr}') is month-dependent and cannot be expressed in static cron syntax.");
            return;
        }

        errors.Add($"Unrecognised day expression ('{fullExpr}') was omitted.");
    }

    // ── Nth Weekday Parser ─────────────────────────────────────────────────────

    private static bool TryParseNthWeekday(string expr, out NaturalCronNthWeekDay nth, out NaturalCronDayOfWeek dayOfWeek)
    {
        nth = default;
        dayOfWeek = default;
        var upper = expr.ToUpper();

        var weekdayKv = ExpressionUtil.NthWeekDaysMap
            .FirstOrDefault(kv => upper.ContainsWholeWord(kv.Key));
        if (weekdayKv.Key == null) return false;

        dayOfWeek = (NaturalCronDayOfWeek)weekdayKv.Value;

        var nthPart = expr.Split('-')[0].ToUpper().Trim();
        nth = nthPart.StartsWith("FIRST")  || nthPart.StartsWith("1ST")  ? NaturalCronNthWeekDay.First  :
              nthPart.StartsWith("SECOND") || nthPart.StartsWith("2ND")  ? NaturalCronNthWeekDay.Second :
              nthPart.StartsWith("THIRD")  || nthPart.StartsWith("3RD")  ? NaturalCronNthWeekDay.Third  :
              nthPart.StartsWith("FOURTH") || nthPart.StartsWith("4TH")  ? NaturalCronNthWeekDay.Fourth :
              nthPart.StartsWith("FIFTH")  || nthPart.StartsWith("5TH")  ? NaturalCronNthWeekDay.Fifth  :
              nthPart.StartsWith("LAST")                                  ? NaturalCronNthWeekDay.Last   :
              (NaturalCronNthWeekDay)(-1);

        return (int)nth != -1;
    }

    // ── Value Helpers ──────────────────────────────────────────────────────────

    private static int? ResolveValue(NaturalCronTimeUnit timeUnit, string expr)
    {
        var direct = ExpressionUtil.TryParseToInt(timeUnit, expr);
        if (direct.HasValue) return direct;
        // "First" / "Last" boundary keywords produced by upto/from rules
        return ExpressionUtil.TryGetValueForMatch(timeUnit, ReferenceDate, expr);
    }

    private static string JoinIntValues(NaturalCronAtInOnRule[] rules, NaturalCronTimeUnit timeUnit)
        => string.Join(",", rules
            .Select(r => ExpressionUtil.TryParseToInt(timeUnit, r.InnerExpression))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Distinct()
            .Select(v => v.ToString()));
}
