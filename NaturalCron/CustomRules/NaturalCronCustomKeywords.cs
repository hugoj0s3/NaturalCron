using System.Text.RegularExpressions;
using NaturalCron.Utils;

namespace NaturalCron.CustomRules;

/// <summary>
/// Global, one-time configuration of custom keywords (e.g. "holiday") usable in expressions:
/// <c>every day on holiday at 09:00</c>, <c>every day at 09:00 except holiday</c>.
/// </summary>
public static class NaturalCronCustomKeywords
{
    private static readonly Regex KeywordRegex = new(@"^[A-Za-z][A-Za-z0-9]*\z", RegexOptions.Compiled);
    private static readonly object ConfigureLock = new();

    private static volatile IReadOnlyDictionary<string, CustomKeywordRegistration> registrations =
        new Dictionary<string, CustomKeywordRegistration>(StringComparer.OrdinalIgnoreCase);

    private static volatile bool isConfigured;

    /// <summary>
    /// True once <see cref="Configure"/> has completed successfully.
    /// </summary>
    public static bool IsConfigured => isConfigured;

    /// <summary>
    /// Configures the custom keywords. Can only be called once; all registrations are validated together and
    /// nothing is applied if any of them is invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when custom keywords were already configured.</exception>
    /// <exception cref="ArgumentException">Thrown when any keyword is invalid, duplicated or collides with a built-in keyword.</exception>
    public static void Configure(Action<NaturalCronCustomKeywordsBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        lock (ConfigureLock)
        {
            if (isConfigured)
            {
                throw new InvalidOperationException("Custom keywords are already configured. Configure can only be called once.");
            }

            var builder = new NaturalCronCustomKeywordsBuilder();
            configure(builder);

            var errors = new List<string>();
            var result = new Dictionary<string, CustomKeywordRegistration>(StringComparer.OrdinalIgnoreCase);
            foreach (var (keyword, createRule) in builder.Registrations)
            {
                if (keyword is null || !KeywordRegex.IsMatch(keyword))
                {
                    errors.Add($"Custom keyword '{keyword}' is invalid. It must match [A-Za-z][A-Za-z0-9]*.");
                    continue;
                }

                if (IsBuiltInKeyword(keyword))
                {
                    errors.Add($"Custom keyword '{keyword}' collides with a built-in keyword.");
                    continue;
                }

                if (result.ContainsKey(keyword))
                {
                    errors.Add($"Custom keyword '{keyword}' is registered more than once.");
                    continue;
                }

                result.Add(keyword, new CustomKeywordRegistration(keyword, createRule));
            }

            if (errors.Any())
            {
                throw new ArgumentException(string.Join("\n", errors));
            }

            registrations = result;
            isConfigured = true;
        }
    }

    internal static bool TryGet(string keyword, out CustomKeywordRegistration registration)
    {
        return registrations.TryGetValue(keyword, out registration!);
    }

    internal static bool HasAny => registrations.Count > 0;

    internal static void Reset()
    {
        lock (ConfigureLock)
        {
            registrations = new Dictionary<string, CustomKeywordRegistration>(StringComparer.OrdinalIgnoreCase);
            isConfigured = false;
        }
    }

    private static bool IsBuiltInKeyword(string keyword)
    {
        var upper = keyword.ToUpper();
        return KeywordsConstants.RuleSpec
            .Concat(KeywordsConstants.Anchored)
            .Concat(KeywordsConstants.TimeUnitWords)
            .Concat(KeywordsConstants.MonthWords)
            .Concat(KeywordsConstants.WeekdayWords)
            .Concat(KeywordsConstants.LastDayOfTheMonth)
            .Concat(KeywordsConstants.FirstDayOfTheMonth)
            .Concat(KeywordsConstants.LastNthWeekDays)
            .Concat(KeywordsConstants.NthWeekDays)
            .Concat(KeywordsConstants.RelativeWeekdays)
            .Concat(KeywordsConstants.Last)
            .Concat(KeywordsConstants.First)
            .Concat(KeywordsConstants.PmOrAm)
            .Concat(KeywordsConstants.And)
            .Concat(KeywordsConstants.IanaTimeZones)
            .Any(x => x.ToUpper() == upper);
    }
}

internal sealed class CustomKeywordRegistration
{
    public CustomKeywordRegistration(string keyword, Func<INaturalCronMatchableCustomKeyword> createRule)
    {
        Keyword = keyword;
        CreateRule = createRule;
    }

    public string Keyword { get; }
    public Func<INaturalCronMatchableCustomKeyword> CreateRule { get; }
}
