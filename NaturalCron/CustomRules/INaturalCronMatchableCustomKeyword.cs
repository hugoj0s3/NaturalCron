namespace NaturalCron.CustomRules;

/// <summary>
/// A custom keyword (e.g. "holiday") that says whether a time matches, configured via
/// <see cref="NaturalCronCustomKeywords.Configure"/>.
/// It can be used with on/at/in and except, e.g. <c>every day on holiday</c>, <c>every day except holiday</c>.
/// It cannot be used as a boundary of between, upto or from.
/// </summary>
/// <remarks>
/// <see cref="Match"/> is called on every step of the occurrence search (potentially many times per
/// GetNextOccurrence call). Avoid a database or HTTP round trip on every call: keep the data in memory and let Match
/// read from it. Loading the data now and then is fine, e.g. load all the holidays once and refresh them periodically,
/// or cache the holidays per year: the database is only reached the first time a date of a new year is checked,
/// every other call for that year is answered from memory.
/// The instance is created once per parsed expression and reused for every match.
/// </remarks>
public interface INaturalCronMatchableCustomKeyword
{
    /// <summary>
    /// Granularity of the keyword. It drives how far the engine safely advances when the keyword doesn't match
    /// (e.g. Day for a holiday calendar, Hour for an hourly maintenance window).
    /// </summary>
    NaturalCronTimeUnit TimeUnit { get; }

    /// <summary>
    /// Returns true when the given time matches the keyword.
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
