using NaturalCron.Rules;
using NaturalCron.Tokens.Parser.Dtos;
using NaturalCron.Utils;

namespace NaturalCron.Tokens.Parser.ParseSpecStrategies;

// except accepts the same values as on/at/in and negates them.
internal class ExceptParseRuleSpecStrategy : ParseRuleSpecStrategy
{
    public override RuleSpecType Type => RuleSpecType.Except;

    public override (IList<NaturalCronRule> rules, IList<string> errors) Parse(GroupedRuleSpec groupedRuleSpec)
    {
        var tokens = TokenParserUtil.TrimWhitespaceAndIgnoredTokens(groupedRuleSpec.Tokens);
        var (innerRules, errors) = AtInOnParseRuleSpecStrategy.ParseValues(tokens);
        if (errors.Any())
        {
            return (new List<NaturalCronRule>(), errors);
        }

        var matchableRules = innerRules.OfType<NaturalCronMatchableRule>().ToList();
        if (!matchableRules.Any())
        {
            return (new List<NaturalCronRule>(), new List<string>() { "Missing except value" });
        }

        NaturalCronRule exceptRule = new NaturalCronExceptRule
        {
            Rules = matchableRules,
            TimeUnit = matchableRules.Min(x => x.TimeUnit),
            FullExpression = TokenParserUtil.JoinTokens(tokens).NormalizeExpression(),
        };

        return (new List<NaturalCronRule>() { exceptRule }, new List<string>());
    }
}
