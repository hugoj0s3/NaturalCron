namespace NaturalCron.CustomRules;

/// <summary>
/// A user-defined rule bound to a custom keyword (e.g. "holiday") configured via
/// <see cref="NaturalCronCustomKeywords.Configure"/>.
/// </summary>
/// <remarks>
/// <see cref="Match"/> is called on every step of the occurrence search (potentially many times per
/// GetNextOccurrence call), so it must be fast and non-blocking. Load or cache any external data up front
/// (e.g. a HashSet of holiday dates refreshed periodically) instead of querying a database or HTTP endpoint inside Match.
/// The instance is created once per parsed expression and reused for every match.
/// </remarks>
public interface INaturalCronCustomRule
{
    /// <summary>
    /// Granularity of the rule. It drives how far the engine safely advances when the rule doesn't match
    /// (e.g. Day for a holiday calendar, Hour for an hourly maintenance window).
    /// </summary>
    NaturalCronTimeUnit TimeUnit { get; }

    /// <summary>
    /// Returns true when the given time matches the rule.
    /// </summary>
    /// <param name="dateTimeInTz">Wall-clock time (Kind = Unspecified) in <paramref name="timeZone"/>.</param>
    /// <param name="timeZone">
    /// The zone the expression is evaluated in. The expression's tz rule always wins; without one, it depends on the method called:
    /// <list type="bullet">
    /// <item><description>*InUtc methods: UTC.</description></item>
    /// <item><description>*InTz methods: the IANA zone passed in.</description></item>
    /// <item><description>Plain methods: the local zone (UTC if the base time has <c>Kind = Utc</c>).</description></item>
    /// </list>
    /// </param>
    bool Match(DateTime dateTimeInTz, TimeZoneInfo timeZone);
}
