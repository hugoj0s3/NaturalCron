using NaturalCron.Rules;
using NaturalCron.Tokens.Parser.Dtos;

namespace NaturalCron.Tokens.Parser.ParseSpecStrategies;

internal class AtInOnParseRuleSpecStrategy : ParseRuleSpecStrategy
{
    public override RuleSpecType Type => RuleSpecType.AtInOn;

    public override (IList<NaturalCronRule> rules, IList<string> errors) Parse(GroupedRuleSpec groupedRuleSpec)
    {
        var tokens = TokenParserUtil.TrimWhitespaceAndIgnoredTokens(groupedRuleSpec.Tokens);
        if (tokens.Any(x => x.Type == NaturalCronTokenType.OpenBrackets))
        {
            return ParseMultiplesAtInOn(tokens);
        }

        return ParseSingleAtInOn(tokens);
    }

    private static (IList<NaturalCronRule> rules, IList<string> errors) ParseSingleAtInOn(IList<NaturalCronToken> tokens)
    {
        var errors = new List<string>();
        
        // Ignore the first token it is the rule spec.
        var timeUnits = TokenParserUtil.ParseTimeUnitValues(tokens.Skip(1).ToList());
        var atInOnRules = new List<NaturalCronRule>();
        foreach (var timeUnit in timeUnits)
        {
            if (timeUnit.TimeUnit == NaturalCronTimeUnitAndUnknown.Unknown)
            {
                var error = string.IsNullOrEmpty(timeUnit.Error)
                    ? "Invalid at/In/On expression. start and end must have known time units" : timeUnit.Error;
                errors.Add(error);
                return (atInOnRules, errors);
            }

            var atRule = new NaturalCronAtInOnRule()
            {
                TimeUnit = (NaturalCronTimeUnit)(int)timeUnit.TimeUnit, 
                InnerExpression = timeUnit.Value.NormalizeExpression(),
                FullExpression = TokenParserUtil.JoinTokens(tokens).NormalizeExpression(),
            };
            atInOnRules.Add(atRule);
        }

        return (atInOnRules, errors);
    }

    private static (IList<NaturalCronRule> rules, IList<string> errors) ParseMultiplesAtInOn(IList<NaturalCronToken> tokens)
    {
        var atInOnMultipleValues = new Dictionary<NaturalCronTimeUnitAndUnknown, List<string>>();

        var tokensOutsideBrackets = new List<NaturalCronToken>();
        var tokensInsideBrackets = new List<NaturalCronToken>();
        var isAccumulating = false;

        // Ignore the first token it is the rule spec.
        foreach (var token in tokens.Skip(1))
        {
            if (token.Type == NaturalCronTokenType.OpenBrackets)
            {
                isAccumulating = true;
                continue;
            }

            if (!isAccumulating)
            {
                tokensOutsideBrackets.Add(token);
                continue;
            }

            if (token.Type == NaturalCronTokenType.CloseBrackets)
            {
                isAccumulating = false;
                continue;
            }

            tokensInsideBrackets.Add(token);
        }

        var tokensWithValues = TokenParserUtil.SplitTokens(tokensInsideBrackets, NaturalCronTokenType.Comma);
        var defaultTimeUnitToken = tokensOutsideBrackets.FirstOrDefault(t => t.Type == NaturalCronTokenType.TimeUnit);
        var defaultTimeUnit = NaturalCronTimeUnitAndUnknown.Unknown;
        if (defaultTimeUnitToken != null)
        {
            defaultTimeUnit = TokenParserUtil.GetTimeUnit(defaultTimeUnitToken.Value);
        }
        
        var errors = new List<string>();
        foreach (var tokenWithValues in tokensWithValues)
        {
            var timeUnitValues = TokenParserUtil.ParseTimeUnitValues(tokenWithValues, defaultTimeUnit);
            foreach (var timeUnitValue in timeUnitValues)
            {
                if (atInOnMultipleValues.TryGetValue(timeUnitValue.TimeUnit, out var multipleValues))
                {
                    multipleValues.Add(timeUnitValue.Value);
                }
                else
                {
                    multipleValues = new List<string>() { timeUnitValue.Value };
                    atInOnMultipleValues.Add(timeUnitValue.TimeUnit, multipleValues);
                }
                
                if (!timeUnitValue.IsValid)
                {
                    errors.Add(timeUnitValue.Error);
                }
            }
        }
        
        if (errors.Any())
        {
            var error = string.Join(" ", errors);
            return (new List<NaturalCronRule>(), new List<string>() { error });
        }

        // Valid if the atInOnMultipleValues are equals
        var maxValueLength = atInOnMultipleValues.Values.Max(x => x.Count);
        if (atInOnMultipleValues.Values.Any(x => x.Count != maxValueLength))
        {
            var error = "Invalid at, on, in expression. Multiple values must have the same number of time units";
            return (new List<NaturalCronRule>(), new List<string>() { error });
        }

        if (atInOnMultipleValues.ContainsKey(NaturalCronTimeUnitAndUnknown.Unknown))
        {
            return (new List<NaturalCronRule>(), new List<string>()
            {
                "Invalid at, on, in expression. Multiple values must have known time units"
            });
        }
        
        var timeUnits = new[] {
            NaturalCronTimeUnitAndUnknown.Second,
            NaturalCronTimeUnitAndUnknown.Minute,
            NaturalCronTimeUnitAndUnknown.Hour,
            NaturalCronTimeUnitAndUnknown.Day,
            NaturalCronTimeUnitAndUnknown.Week,
            NaturalCronTimeUnitAndUnknown.Month,
            NaturalCronTimeUnitAndUnknown.Year
        };
        
        var rulesByUnit = new Dictionary<NaturalCronTimeUnitAndUnknown, List<NaturalCronAtInOnRule>>();
        foreach (var timeUnit in timeUnits)
        {
            atInOnMultipleValues.TryGetValue(timeUnit, out var values);
            values ??= new List<string>();

            var rules = values
                .Select(value => new NaturalCronAtInOnRule { TimeUnit = (NaturalCronTimeUnit)timeUnit, InnerExpression = value.NormalizeExpression() })
                .ToList();

            rulesByUnit[timeUnit] = rules;
        }

        NaturalCronRule result = new NaturalCronAtInOnMultiplesRule()
        {
            SecondRules = rulesByUnit[NaturalCronTimeUnitAndUnknown.Second].ToArray(),
            MinuteRules = rulesByUnit[NaturalCronTimeUnitAndUnknown.Minute].ToArray(),
            HourRules = rulesByUnit[NaturalCronTimeUnitAndUnknown.Hour].ToArray(),
            DayRules = rulesByUnit[NaturalCronTimeUnitAndUnknown.Day].ToArray(),
            MonthRules = rulesByUnit[NaturalCronTimeUnitAndUnknown.Month].ToArray(),
            WeekRules = rulesByUnit[NaturalCronTimeUnitAndUnknown.Week].ToArray(),
            YearRules = rulesByUnit[NaturalCronTimeUnitAndUnknown.Year].ToArray(),
            TimeUnit = (NaturalCronTimeUnit)(int) rulesByUnit.First(x => x.Value.Any()).Key,
            FullExpression = TokenParserUtil.JoinTokens(tokens),
        };

        return (new List<NaturalCronRule>() { result }, new List<string>());
    }
}