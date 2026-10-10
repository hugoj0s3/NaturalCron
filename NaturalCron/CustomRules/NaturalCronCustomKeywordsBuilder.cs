namespace NaturalCron.CustomRules;

/// <summary>
/// Collects custom keyword registrations inside <see cref="NaturalCronCustomKeywords.Configure"/>.
/// </summary>
public sealed class NaturalCronCustomKeywordsBuilder
{
    internal NaturalCronCustomKeywordsBuilder()
    {
    }

    internal List<(string Keyword, Func<INaturalCronCustomRule> CreateRule)> Registrations { get; } = new();

    /// <summary>
    /// Registers a keyword backed by <typeparamref name="TRule"/>, created with its parameterless constructor
    /// once per parsed expression.
    /// </summary>
    public NaturalCronCustomKeywordsBuilder Add<TRule>(string keyword) where TRule : INaturalCronCustomRule, new()
    {
        Registrations.Add((keyword, () => new TRule()));
        return this;
    }

    /// <summary>
    /// Registers a keyword whose rule instance is created by <paramref name="factory"/> once per parsed expression,
    /// e.g. to resolve it from an IoC container: <c>() => serviceProvider.GetRequiredService&lt;HolidayRule&gt;()</c>.
    /// </summary>
    public NaturalCronCustomKeywordsBuilder Add(string keyword, Func<INaturalCronCustomRule> factory)
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        Registrations.Add((keyword, factory));
        return this;
    }

    /// <summary>
    /// Registers a keyword backed by a match function.
    /// </summary>
    /// <param name="keyword">The keyword, [A-Za-z][A-Za-z0-9]*, case-insensitive.</param>
    /// <param name="match">Receives the wall-clock time and the time zone it is in. See <see cref="INaturalCronCustomRule.Match"/>.</param>
    /// <param name="timeUnit">Granularity of the rule, default Day.</param>
    public NaturalCronCustomKeywordsBuilder Add(
        string keyword,
        Func<DateTime, TimeZoneInfo, bool> match,
        NaturalCronTimeUnit timeUnit = NaturalCronTimeUnit.Day)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        Registrations.Add((keyword, () => new FuncWithTimeZoneCustomRule(match, timeUnit)));
        return this;
    }
    
    /// <summary>
    /// Registers a keyword backed by a match function.
    /// </summary>
    /// <param name="keyword">The keyword, [A-Za-z][A-Za-z0-9]*, case-insensitive.</param>
    /// <param name="match">Receives the wall-clock time in the evaluated time zone. See <see cref="INaturalCronCustomRule.Match"/>.</param>
    /// <param name="timeUnit">Granularity of the rule, default Day.</param>
    public NaturalCronCustomKeywordsBuilder Add(
        string keyword,
        Func<DateTime, bool> match,
        NaturalCronTimeUnit timeUnit = NaturalCronTimeUnit.Day)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        Registrations.Add((keyword, () => new FuncCustomRule(match, timeUnit)));
        return this;
    }
}

internal sealed class FuncWithTimeZoneCustomRule : INaturalCronCustomRule
{
    private readonly Func<DateTime, TimeZoneInfo, bool> match;

    public FuncWithTimeZoneCustomRule(Func<DateTime, TimeZoneInfo, bool> match, NaturalCronTimeUnit timeUnit)
    {
        this.match = match;
        TimeUnit = timeUnit;
    }

    public NaturalCronTimeUnit TimeUnit { get; }

    public bool Match(DateTime dateTimeInTz, TimeZoneInfo timeZone) => match(dateTimeInTz, timeZone);
}

internal sealed class FuncCustomRule : INaturalCronCustomRule
{
    private readonly Func<DateTime, bool> match;

    public FuncCustomRule(Func<DateTime, bool> match, NaturalCronTimeUnit timeUnit)
    {
        this.match = match;
        TimeUnit = timeUnit;
    }

    public NaturalCronTimeUnit TimeUnit { get; }

    public bool Match(DateTime dateTimeInTz, TimeZoneInfo timeZone) => match(dateTimeInTz);
}
