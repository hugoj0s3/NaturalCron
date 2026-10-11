using System.Text.RegularExpressions;
using FluentAssertions;
using TimeZoneConverter;

namespace NaturalCron.UnitTests;

/// <summary>
/// Checks that a JSON test case gives the same occurrences through the three families of methods:
/// the UTC methods, the *InTz methods and the local methods.
/// </summary>
internal static class AllOccurrenceMethodsAssert
{
    // tz Asia/Tokyo, @tz Asia/Tokyo or @timezone Asia/Tokyo
    internal static readonly Regex TimeZoneRuleRegex =
        new(@"(?<![A-Za-z0-9])@?(?:timezone|tz)\s+(?<zone>\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Zones without daylight saving time, so that converting the results never hits a skipped or repeated hour.
    private static readonly string[] OtherZones = { "Asia/Tokyo", "Asia/Kolkata" };

    /// <param name="expression">The expression of the test case.</param>
    /// <param name="baseTimeUtc">The base time of the test case, in UTC.</param>
    /// <param name="expectedUtc">The expected occurrences, in UTC.</param>
    /// <param name="dependsOnTheZoneOfTheMethod">
    /// True when the expression has keywords that look at the time zone: without a tz rule they are only
    /// evaluated in UTC, the zone the expected occurrences were written for.
    /// </param>
    /// <param name="hasCustomKeywords">
    /// True when the expression has custom keywords: they are not evaluated in the zone of the machine,
    /// so that the test gives the same result on every machine.
    /// </param>
    internal static void ShouldReturn(string expression, DateTime baseTimeUtc, IReadOnlyList<DateTime> expectedUtc,
        bool dependsOnTheZoneOfTheMethod, bool hasCustomKeywords, string description)
    {
        baseTimeUtc = DateTime.SpecifyKind(baseTimeUtc, DateTimeKind.Utc);
        expectedUtc = expectedUtc.Select(x => DateTime.SpecifyKind(x, DateTimeKind.Utc)).ToList();
        var count = expectedUtc.Count;
        var naturalCronExpr = NaturalCronExpr.Parse(expression);

        // 1. UTC methods
        naturalCronExpr.TryGetNextOccurrencesInUtc(baseTimeUtc, count).Should().Equal(expectedUtc, $"UTC methods: {description}");

        var timeZoneRule = TimeZoneRuleRegex.Match(expression);
        if (timeZoneRule.Success)
        {
            ShouldReturnWithTimeZoneRule(naturalCronExpr, expression, timeZoneRule, baseTimeUtc, expectedUtc, description);
        }
        else
        {
            ShouldReturnWithoutTimeZoneRule(naturalCronExpr, baseTimeUtc, expectedUtc, dependsOnTheZoneOfTheMethod, hasCustomKeywords, description);
        }
    }

    // The expression has a tz rule: it always wins, whatever method is called.
    private static void ShouldReturnWithTimeZoneRule(NaturalCronExpr naturalCronExpr, string expression, Match timeZoneRule,
        DateTime baseTimeUtc, IReadOnlyList<DateTime> expectedUtc, string description)
    {
        var count = expectedUtc.Count;
        var ruleZoneId = timeZoneRule.Groups["zone"].Value;
        var ruleZone = TZConvert.GetTimeZoneInfo(ruleZoneId);

        // 2. *InTz methods with the zone of the rule, and with other zones: the results come back in the zone of the method.
        foreach (var zoneId in new[] { ruleZoneId }.Concat(OtherZones))
        {
            var zone = TZConvert.GetTimeZoneInfo(zoneId);
            var occurrences = naturalCronExpr.TryGetNextOccurrencesInTz(ToZone(baseTimeUtc, zone), count, zoneId);
            ToUtc(occurrences, zone).Should().Equal(expectedUtc, $"InTz methods with {zoneId}: {description}");
        }

        // 3. *InTz methods without the rule: the zone of the method is the zone the expression is evaluated in.
        var withoutTimeZoneRule = NaturalCronExpr.Parse(expression.Remove(timeZoneRule.Index, timeZoneRule.Length));
        withoutTimeZoneRule.TimeZoneRule().Should().BeNull();
        var occurrencesInRuleZone = withoutTimeZoneRule.TryGetNextOccurrencesInTz(ToZone(baseTimeUtc, ruleZone), count, ruleZoneId);
        ToUtc(occurrencesInRuleZone, ruleZone).Should().Equal(expectedUtc, $"InTz methods without the tz rule: {description}");

        // 4. Local methods: the results come back in the zone of the machine.
        var baseTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(baseTimeUtc, TimeZoneInfo.Local);
        var expectedLocal = expectedUtc.Select(x => TimeZoneInfo.ConvertTimeFromUtc(x, TimeZoneInfo.Local)).ToList();
        naturalCronExpr.TryGetNextOccurrences(baseTimeLocal, count).Should().Equal(expectedLocal, $"local methods: {description}");
    }

    // No tz rule: the rules are evaluated directly on the wall-clock time that is passed in.
    private static void ShouldReturnWithoutTimeZoneRule(NaturalCronExpr naturalCronExpr, DateTime baseTimeUtc,
        IReadOnlyList<DateTime> expectedUtc, bool dependsOnTheZoneOfTheMethod, bool hasCustomKeywords, string description)
    {
        var count = expectedUtc.Count;
        var baseWallClock = DateTime.SpecifyKind(baseTimeUtc, DateTimeKind.Unspecified);

        // 2. *InTz methods: with UTC, and with other zones when the expression does not look at the zone.
        var zoneIds = dependsOnTheZoneOfTheMethod ? new[] { "UTC" } : new[] { "UTC" }.Concat(OtherZones).ToArray();
        foreach (var zoneId in zoneIds)
        {
            naturalCronExpr.TryGetNextOccurrencesInTz(baseWallClock, count, zoneId)
                .Should().Equal(expectedUtc, $"InTz methods with {zoneId}: {description}");
        }

        // 3. Local methods. A UTC base time is evaluated in UTC, any other in the zone of the machine,
        // which only matters for custom keywords.
        naturalCronExpr.TryGetNextOccurrences(baseTimeUtc, count)
            .Should().Equal(expectedUtc, $"local methods with a UTC base time: {description}");

        if (!hasCustomKeywords)
        {
            naturalCronExpr.TryGetNextOccurrences(baseWallClock, count)
                .Should().Equal(expectedUtc, $"local methods: {description}");
        }
    }

    private static DateTime ToZone(DateTime utc, TimeZoneInfo zone) =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utc, zone), DateTimeKind.Unspecified);

    private static List<DateTime> ToUtc(IEnumerable<DateTime> occurrencesInZone, TimeZoneInfo zone) =>
        occurrencesInZone
            .Select(x => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(x, DateTimeKind.Unspecified), zone))
            .ToList();
}
