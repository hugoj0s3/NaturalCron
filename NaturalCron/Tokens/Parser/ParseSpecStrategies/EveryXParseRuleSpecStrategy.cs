using NaturalCron.Rules;
using NaturalCron.Tokens.Parser.Dtos;
using NaturalCron.Utils;

namespace NaturalCron.Tokens.Parser.ParseSpecStrategies;

internal class EveryXParseRuleSpecStrategy : ParseRuleSpecStrategy
{

    public override RuleSpecType Type => RuleSpecType.EveryX;
    public override (IList<NaturalCronRule> rules, IList<string> errors) Parse(GroupedRuleSpec groupedRuleSpec)
    {
        var tokens = TokenParserUtil.RemoveWhitespaceAndIgnoredTokens(groupedRuleSpec.Tokens);
        if (tokens.Count < 2)
        {
            return (new List<NaturalCronRule>(), "Invalid every expression".AsList());
        }

        if (IsEveryValue(tokens))
        {
            return ParseEveryValue(groupedRuleSpec, tokens);
        }

        var everyTimeUnit = tokens.Count > 2
            ? ExpressionUtil.TryParseToTimeUnit(tokens[2].Value) ?? ExpressionUtil.TryParseToTimeUnit(tokens[1].Value)
            : ExpressionUtil.TryParseToTimeUnit(tokens[1].Value);

        if (everyTimeUnit == null)
        {
            var containsWeekdayWord = KeywordsConstants.WeekdayWords.ToUpper().Contains(tokens[1].Value.ToUpper());
            if (containsWeekdayWord)
            {
                var naturalCronEveryXRule = new NaturalCronEveryXRule()
                {
                    Value = 1,
                    AnchoredValue = tokens[1].Value.ToUpper(),
                    TimeUnit =  NaturalCronTimeUnit.Week,
                    FullExpression = TokenParserUtil.JoinTokens(tokens, " ", NaturalCronTokenType.WhiteSpace).NormalizeExpression()
                };
                return (naturalCronEveryXRule.AsList<NaturalCronRule>(), new List<string>());
            }

            return (new List<NaturalCronRule>(), "Invalid every expression".AsList());
        }

        // every 1 day or every day
        var value = 1;
        if (tokens.Count > 2 && tokens[2].Type == NaturalCronTokenType.TimeUnit)
        {
            if (!int.TryParse(tokens[1].Value, out value))
            {
                return (new List<NaturalCronRule>(), "Invalid every expression".AsList());
            }
        }

        var anchoredValue = TokenParserUtil.GetAnchoredValue(tokens);
        var everyXExpression = new NaturalCronEveryXRule()
        {
            Value = value,
            AnchoredValue = anchoredValue,
            TimeUnit = everyTimeUnit.Value,
            FullExpression = TokenParserUtil.JoinTokens(tokens, " ", NaturalCronTokenType.WhiteSpace).NormalizeExpression()
        };

        return (everyXExpression.AsList<NaturalCronRule>(), new List<string>());
    }

    // "every <value>" is a shorthand for "every <period> on <value>", e.g. every january, every 15th, every 9am, every holiday.
    // It is everything that is not an interval (every day, every 2 days) nor a single weekday (every monday).
    private static bool IsEveryValue(IList<NaturalCronToken> tokens)
    {
        var valueTokens = tokens.Skip(1).Where(x => x.Type != NaturalCronTokenType.EndOfExpression).ToList();
        if (valueTokens.Count == 0)
        {
            return false;
        }

        var first = valueTokens[0];
        if (IsIntervalTimeUnit(first))
        {
            return false;
        }

        // every 2 days is an interval, every 15th is a value: "th" is also a day time unit word.
        if (first.Type == NaturalCronTokenType.WholeNumber && valueTokens.Count > 1 && IsIntervalTimeUnit(valueTokens[1]))
        {
            return false;
        }

        var isSingleWeekday = valueTokens.Count == 1 &&
                              KeywordsConstants.WeekdayWords.ToUpper().Contains(first.Value.ToUpper());
        return !isSingleWeekday;
    }

    private static bool IsIntervalTimeUnit(NaturalCronToken token) =>
        token.Type == NaturalCronTokenType.TimeUnit && !token.IsDayOrdinal();

    private static (IList<NaturalCronRule> rules, IList<string> errors) ParseEveryValue(
        GroupedRuleSpec groupedRuleSpec,
        IList<NaturalCronToken> tokens)
    {
        // The values are parsed like on/at/in values, where the whitespaces matter (e.g. ClosestWeekdayTo 15).
        var valueTokens = TokenParserUtil.TrimWhitespaceAndIgnoredTokens(groupedRuleSpec.Tokens.ToList());
        var (valueRules, errors) = AtInOnParseRuleSpecStrategy.ParseValues(valueTokens);
        if (errors.Any())
        {
            return (new List<NaturalCronRule>(), errors);
        }

        if (!valueRules.Any())
        {
            return (new List<NaturalCronRule>(), "Invalid every expression".AsList());
        }

        var everyTimeUnit = GetEveryTimeUnit(valueRules);
        if (everyTimeUnit == null)
        {
            return (new List<NaturalCronRule>(),
                "Invalid every expression. A date with a year occurs only once, use 'on' instead".AsList());
        }

        var rules = new List<NaturalCronRule>
        {
            new NaturalCronEveryXRule()
            {
                Value = 1,
                TimeUnit = everyTimeUnit.Value,
                FullExpression = TokenParserUtil.JoinTokens(tokens, " ", NaturalCronTokenType.WhiteSpace).NormalizeExpression()
            }
        };
        rules.AddRange(valueRules);

        return (rules, new List<string>());
    }

    // The period that contains one occurrence of the value: every 9am => day, every 15th => month, every january => year.
    private static NaturalCronTimeUnit? GetEveryTimeUnit(IList<NaturalCronRule> valueRules)
    {
        // Custom keywords and lists with custom keywords only say whether a time matches: every unit of theirs is checked,
        // e.g. every holiday => every day on holiday.
        var filterRules = valueRules.Where(x => x is NaturalCronCustomKeywordRule || x is NaturalCronAnyOfMatchableRule).ToList();
        if (filterRules.Any())
        {
            return filterRules.Min(x => x.TimeUnit);
        }

        var timeUnits = valueRules
            .SelectMany(x => x is NaturalCronAtInOnMultiplesRule multiplesRule
                ? multiplesRule.GetAllRules().Select(y => y.TimeUnit)
                : new[] { x.TimeUnit })
            .ToList();

        if (timeUnits.Contains(NaturalCronTimeUnit.Year))
        {
            return null;
        }

        if (timeUnits.Contains(NaturalCronTimeUnit.Month))
        {
            return NaturalCronTimeUnit.Year;
        }

        if (timeUnits.Contains(NaturalCronTimeUnit.Day))
        {
            return NaturalCronTimeUnit.Month;
        }

        if (timeUnits.Contains(NaturalCronTimeUnit.Week))
        {
            return NaturalCronTimeUnit.Week;
        }

        // Second => Minute, Minute => Hour, Hour => Day
        return timeUnits.Max() + 1;
    }
}
