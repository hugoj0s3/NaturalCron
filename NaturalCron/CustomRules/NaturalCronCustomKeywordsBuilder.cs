namespace NaturalCron.CustomRules;

/// <summary>
/// Collects custom keyword registrations inside <see cref="NaturalCronCustomKeywords.Configure"/>.
/// </summary>
public sealed class NaturalCronCustomKeywordsBuilder
{
    internal NaturalCronCustomKeywordsBuilder()
    {
    }

    internal List<(string Keyword, Func<INaturalCronMatchableCustomKeyword> CreateRule)> Registrations { get; } = new();

    /// <summary>
    /// Registers a matchable keyword (usable with on/at/in and except) implemented by <typeparamref name="TKeyword"/>,
    /// created with its parameterless constructor once per parsed expression.
    /// </summary>
    public NaturalCronCustomKeywordsBuilder AddMatchable<TKeyword>(string keyword) where TKeyword : INaturalCronMatchableCustomKeyword, new()
    {
        return AddMatchable<TKeyword>(new[] { keyword });
    }

    /// <summary>
    /// Registers several keywords for the same implementation, e.g. <c>["holiday", "holidays"]</c>.
    /// See <see cref="AddMatchable{TKeyword}(string)"/>.
    /// </summary>
    public NaturalCronCustomKeywordsBuilder AddMatchable<TKeyword>(IEnumerable<string> keywords) where TKeyword : INaturalCronMatchableCustomKeyword, new()
    {
        return Register(keywords, () => new TKeyword());
    }

    /// <summary>
    /// Registers a matchable keyword whose instance is created by <paramref name="factory"/> once per parsed expression,
    /// e.g. to resolve it from an IoC container: <c>() => serviceProvider.GetRequiredService&lt;HolidayKeyword&gt;()</c>.
    /// </summary>
    public NaturalCronCustomKeywordsBuilder AddMatchable(string keyword, Func<INaturalCronMatchableCustomKeyword> factory)
    {
        return AddMatchable(new[] { keyword }, factory);
    }

    /// <summary>
    /// Registers several keywords for the same implementation, e.g. <c>["holiday", "holidays"]</c>.
    /// See <see cref="AddMatchable(string, Func{INaturalCronMatchableCustomKeyword})"/>.
    /// </summary>
    public NaturalCronCustomKeywordsBuilder AddMatchable(IEnumerable<string> keywords, Func<INaturalCronMatchableCustomKeyword> factory)
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return Register(keywords, factory);
    }

    /// <summary>
    /// Registers a matchable keyword backed by a match function.
    /// </summary>
    /// <param name="keyword">The keyword, [A-Za-z][A-Za-z0-9]*, case-insensitive.</param>
    /// <param name="match">Receives the wall-clock time and the time zone it is in. See <see cref="INaturalCronMatchableCustomKeyword.Match"/>.</param>
    /// <param name="timeUnit">Granularity of the keyword, default Day.</param>
    public NaturalCronCustomKeywordsBuilder AddMatchable(
        string keyword,
        Func<DateTime, TimeZoneInfo, bool> match,
        NaturalCronTimeUnit timeUnit = NaturalCronTimeUnit.Day)
    {
        return AddMatchable(new[] { keyword }, match, timeUnit);
    }

    /// <summary>
    /// Registers several keywords for the same match function, e.g. <c>["holiday", "holidays"]</c>.
    /// </summary>
    /// <param name="keywords">The keywords, each [A-Za-z][A-Za-z0-9]*, case-insensitive.</param>
    /// <param name="match">Receives the wall-clock time and the time zone it is in. See <see cref="INaturalCronMatchableCustomKeyword.Match"/>.</param>
    /// <param name="timeUnit">Granularity of the keywords, default Day.</param>
    public NaturalCronCustomKeywordsBuilder AddMatchable(
        IEnumerable<string> keywords,
        Func<DateTime, TimeZoneInfo, bool> match,
        NaturalCronTimeUnit timeUnit = NaturalCronTimeUnit.Day)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        return Register(keywords, () => new FuncWithTimeZoneMatchableKeyword(match, timeUnit));
    }

    /// <summary>
    /// Registers a matchable keyword backed by a match function.
    /// </summary>
    /// <param name="keyword">The keyword, [A-Za-z][A-Za-z0-9]*, case-insensitive.</param>
    /// <param name="match">Receives the wall-clock time in the evaluated time zone. See <see cref="INaturalCronMatchableCustomKeyword.Match"/>.</param>
    /// <param name="timeUnit">Granularity of the keyword, default Day.</param>
    public NaturalCronCustomKeywordsBuilder AddMatchable(
        string keyword,
        Func<DateTime, bool> match,
        NaturalCronTimeUnit timeUnit = NaturalCronTimeUnit.Day)
    {
        return AddMatchable(new[] { keyword }, match, timeUnit);
    }

    /// <summary>
    /// Registers several keywords for the same match function, e.g. <c>["holiday", "holidays"]</c>.
    /// </summary>
    /// <param name="keywords">The keywords, each [A-Za-z][A-Za-z0-9]*, case-insensitive.</param>
    /// <param name="match">Receives the wall-clock time in the evaluated time zone. See <see cref="INaturalCronMatchableCustomKeyword.Match"/>.</param>
    /// <param name="timeUnit">Granularity of the keywords, default Day.</param>
    public NaturalCronCustomKeywordsBuilder AddMatchable(
        IEnumerable<string> keywords,
        Func<DateTime, bool> match,
        NaturalCronTimeUnit timeUnit = NaturalCronTimeUnit.Day)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        return Register(keywords, () => new FuncMatchableKeyword(match, timeUnit));
    }

    private NaturalCronCustomKeywordsBuilder Register(IEnumerable<string> keywords, Func<INaturalCronMatchableCustomKeyword> createRule)
    {
        if (keywords == null)
        {
            throw new ArgumentNullException(nameof(keywords));
        }

        var keywordList = keywords.ToList();
        if (keywordList.Count == 0)
        {
            throw new ArgumentException("At least one keyword is required.", nameof(keywords));
        }

        foreach (var keyword in keywordList)
        {
            Registrations.Add((keyword, createRule));
        }

        return this;
    }
}

internal sealed class FuncWithTimeZoneMatchableKeyword : INaturalCronMatchableCustomKeyword
{
    private readonly Func<DateTime, TimeZoneInfo, bool> match;

    public FuncWithTimeZoneMatchableKeyword(Func<DateTime, TimeZoneInfo, bool> match, NaturalCronTimeUnit timeUnit)
    {
        this.match = match;
        TimeUnit = timeUnit;
    }

    public NaturalCronTimeUnit TimeUnit { get; }

    public bool Match(DateTime dateTimeInTz, TimeZoneInfo timeZone) => match(dateTimeInTz, timeZone);
}

internal sealed class FuncMatchableKeyword : INaturalCronMatchableCustomKeyword
{
    private readonly Func<DateTime, bool> match;

    public FuncMatchableKeyword(Func<DateTime, bool> match, NaturalCronTimeUnit timeUnit)
    {
        this.match = match;
        TimeUnit = timeUnit;
    }

    public NaturalCronTimeUnit TimeUnit { get; }

    public bool Match(DateTime dateTimeInTz, TimeZoneInfo timeZone) => match(dateTimeInTz);
}
