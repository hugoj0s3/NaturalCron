namespace NaturalCron.Rules;

/// <summary>
/// A bracket list mixing custom keywords and built-in values, e.g. <c>on [holiday, 15th]</c>.
/// It matches when any of its rules matches.
/// </summary>
public class NaturalCronAnyOfRule : NaturalCronMatchableRule
{
    internal NaturalCronAnyOfRule()
    {
    }

    public override NaturalCronRuleSpec Spec => NaturalCronRuleSpec.AtInOn;

    /// <summary>
    /// The alternatives: the custom keyword rule and the rule holding the built-in values.
    /// </summary>
    public IReadOnlyList<NaturalCronMatchableRule> Rules { get; internal set; } = Array.Empty<NaturalCronMatchableRule>();

    protected override bool DoMatch(DateTime dateTime)
    {
        foreach (var rule in Rules)
        {
            if (rule.Match(dateTime))
            {
                return true;
            }
        }

        return false;
    }
}
