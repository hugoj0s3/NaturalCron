using NaturalCron.CustomRules;
using NaturalCron.Rules;
using NaturalCron.Utils;

namespace NaturalCron.Tokens.Parser.ParseSpecStrategies;

internal static class CustomKeywordParseHelper
{
    internal static bool ContainsCustomKeyword(IList<NaturalCronToken> tokens) =>
        tokens.Any(x => x.Type == NaturalCronTokenType.CustomKeyword);

    // Parses "<spec> holiday", "<spec> [holiday, payday]" or a list that also has built-in values
    // e.g. "<spec> [holiday, 15th]", which matches when any entry matches.
    internal static (IList<NaturalCronRule> rules, IList<string> errors) Parse(IList<NaturalCronToken> tokens)
    {
        var fullExpression = TokenParserUtil.JoinTokens(tokens).NormalizeExpression();

        // Ignore the first token it is the rule spec.
        var valueTokens = tokens.Skip(1).ToList();
        var openBracket = valueTokens.FirstOrDefault(x => x.Type == NaturalCronTokenType.OpenBrackets);
        if (openBracket == null)
        {
            var singleValueTokens = valueTokens.Where(IsValueToken).ToList();
            if (singleValueTokens.Count > 1)
            {
                var error = singleValueTokens.All(x => x.Type == NaturalCronTokenType.CustomKeyword)
                    ? "Multiple custom keywords must be inside brackets e.g [holiday, payday]"
                    : "Custom keywords can only be combined with other values inside brackets e.g [holiday, 15th]";
                return (new List<NaturalCronRule>(), new List<string>() { error });
            }

            return BuildCustomKeywordRule(singleValueTokens, fullExpression);
        }

        var tokensOutsideBrackets = new List<NaturalCronToken>();
        var tokensInsideBrackets = new List<NaturalCronToken>();
        NaturalCronToken? closeBracket = null;
        var isAccumulating = false;
        foreach (var token in valueTokens)
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
                closeBracket = token;
                isAccumulating = false;
                continue;
            }

            tokensInsideBrackets.Add(token);
        }

        if (ContainsCustomKeyword(tokensOutsideBrackets))
        {
            return (new List<NaturalCronRule>(), new List<string>() { "Custom keywords must be inside the brackets" });
        }

        var keywordTokens = new List<NaturalCronToken>();
        var builtInEntries = new List<IList<NaturalCronToken>>();
        foreach (var entry in TokenParserUtil.SplitTokens(tokensInsideBrackets, NaturalCronTokenType.Comma))
        {
            var entryValueTokens = entry.Where(IsValueToken).ToList();
            if (entryValueTokens.Count == 0)
            {
                continue;
            }

            if (!ContainsCustomKeyword(entryValueTokens))
            {
                // Line breaks inside the list are not part of the value.
                builtInEntries.Add(entry.Where(x => x.Type != NaturalCronTokenType.IgnoredToken).ToList());
                continue;
            }

            if (entryValueTokens.Count > 1)
            {
                return (new List<NaturalCronRule>(), new List<string>()
                {
                    "A custom keyword cannot be combined with other values in the same list entry"
                });
            }

            keywordTokens.Add(entryValueTokens[0]);
        }

        var customResult = BuildCustomKeywordRule(keywordTokens, fullExpression);
        if (customResult.errors.Any() || builtInEntries.Count == 0)
        {
            return customResult;
        }

        // Parse the built-in values as a regular list, without the custom keywords.
        var builtInTokens = new List<NaturalCronToken> { tokens[0] };
        builtInTokens.AddRange(tokensOutsideBrackets);
        builtInTokens.Add(openBracket);
        var comma = tokensInsideBrackets.First(x => x.Type == NaturalCronTokenType.Comma);
        for (var i = 0; i < builtInEntries.Count; i++)
        {
            if (i > 0)
            {
                builtInTokens.Add(comma);
            }

            builtInTokens.AddRange(builtInEntries[i]);
        }

        if (closeBracket != null)
        {
            builtInTokens.Add(closeBracket);
        }

        var builtInResult = AtInOnParseRuleSpecStrategy.ParseMultiplesAtInOn(builtInTokens);
        if (builtInResult.errors.Any())
        {
            return (new List<NaturalCronRule>(), builtInResult.errors);
        }

        var anyOfRules = customResult.rules
            .Concat(builtInResult.rules)
            .OfType<NaturalCronMatchableRule>()
            .ToList();

        NaturalCronRule anyOfRule = new NaturalCronAnyOfRule
        {
            Rules = anyOfRules,
            TimeUnit = anyOfRules.Min(GetMinTimeUnit),
            FullExpression = fullExpression,
        };

        return (new List<NaturalCronRule>() { anyOfRule }, new List<string>());
    }

    private static bool IsValueToken(NaturalCronToken token) =>
        token.Type != NaturalCronTokenType.WhiteSpace &&
        token.Type != NaturalCronTokenType.IgnoredToken &&
        token.Type != NaturalCronTokenType.EndOfExpression;

    private static NaturalCronTimeUnit GetMinTimeUnit(NaturalCronMatchableRule rule) =>
        rule is NaturalCronAtInOnMultiplesRule multiplesRule
            ? multiplesRule.GetAllRules().Min(x => x.TimeUnit)
            : rule.TimeUnit;

    private static (IList<NaturalCronRule> rules, IList<string> errors) BuildCustomKeywordRule(
        IList<NaturalCronToken> keywordTokens,
        string fullExpression)
    {
        var rules = new List<NaturalCronRule>();
        var errors = new List<string>();

        if (keywordTokens.Count == 0)
        {
            errors.Add("Missing custom keyword");
            return (rules, errors);
        }

        var keywords = new List<string>();
        var instances = new List<INaturalCronMatchableCustomKeyword>();
        foreach (var keywordToken in keywordTokens)
        {
            if (!NaturalCronCustomKeywords.TryGet(keywordToken.Value, out var registration))
            {
                errors.Add($"Custom keyword '{keywordToken.Value}' is not configured");
                continue;
            }

            if (keywords.Contains(registration.Keyword, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var instance = registration.CreateRule();
            if (instance == null)
            {
                errors.Add($"Custom keyword '{registration.Keyword}' rule factory returned null");
                continue;
            }

            if (instance.TimeUnit == NaturalCronTimeUnit.TimeZone ||
                !Enum.IsDefined(typeof(NaturalCronTimeUnit), instance.TimeUnit))
            {
                errors.Add($"Custom keyword '{registration.Keyword}' has an invalid time unit '{instance.TimeUnit}'");
                continue;
            }

            keywords.Add(registration.Keyword);
            instances.Add(instance);
        }

        if (errors.Any())
        {
            return (rules, errors);
        }

        rules.Add(new NaturalCronCustomKeywordRule
        {
            Keywords = keywords,
            Instances = instances,
            TimeUnit = instances.Min(x => x.TimeUnit),
            FullExpression = fullExpression,
        });

        return (rules, errors);
    }
}
