using NaturalCron.CustomRules;

namespace NaturalCron.Rules;

public class NaturalCronCustomKeywordRule : NaturalCronMatchableRule
{
    internal NaturalCronCustomKeywordRule()
    {
    }

    public override NaturalCronRuleSpec Spec => NaturalCronRuleSpec.Custom;

    /// <summary>
    /// The custom keywords of this rule. It matches when any of them matches.
    /// Exclusions (except) wrap this rule in a <see cref="NaturalCronExceptRule"/>.
    /// </summary>
    public IReadOnlyList<string> Keywords { get; internal set; } = Array.Empty<string>();

    internal IReadOnlyList<INaturalCronMatchableCustomKeyword> Instances { get; set; } = Array.Empty<INaturalCronMatchableCustomKeyword>();

    internal TimeZoneInfo EffectiveTimeZone { get; private set; } = TimeZoneInfo.Local;

    // Returns true when the zone changed, so cached matches are no longer valid.
    internal bool SetEffectiveTimeZone(TimeZoneInfo timeZone)
    {
        if (EffectiveTimeZone.Id == timeZone.Id)
        {
            return false;
        }

        EffectiveTimeZone = timeZone;
        ResetCache();
        return true;
    }

    protected override bool DoMatch(DateTime dateTime)
    {
        foreach (var instance in Instances)
        {
            if (instance.Match(dateTime, EffectiveTimeZone))
            {
                return true;
            }
        }

        return false;
    }
}
