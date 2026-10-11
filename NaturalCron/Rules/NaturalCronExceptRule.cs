namespace NaturalCron.Rules;

/// <summary>
/// Negates the rules parsed from an except expression, e.g. <c>except [saturday, sunday]</c>, <c>except holiday</c>.
/// It matches when the inner rules do not all match.
/// </summary>
public class NaturalCronExceptRule : NaturalCronMatchableRule
{
    internal NaturalCronExceptRule()
    {
    }

    public override NaturalCronRuleSpec Spec => NaturalCronRuleSpec.Except;

    /// <summary>
    /// The excluded rules, parsed as an on/at/in expression.
    /// </summary>
    public IReadOnlyList<NaturalCronMatchableRule> Rules { get; internal set; } = Array.Empty<NaturalCronMatchableRule>();

    protected override bool DoMatch(DateTime dateTime)
    {
        foreach (var rule in Rules)
        {
            if (!rule.Match(dateTime))
            {
                return true;
            }
        }

        return false;
    }
}
