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
        bool dependsOnTheZoneOfTheMethod, bool hasCustomKeywords, string description,
        IReadOnlyList<DateTime>? fallbackOnAmbiguousBaseTimeExpectedUtc = null)
    {
        baseTimeUtc = DateTime.SpecifyKind(baseTimeUtc, DateTimeKind.Utc);
        expectedUtc = expectedUtc.Select(x => DateTime.SpecifyKind(x, DateTimeKind.Utc)).ToList();
        fallbackOnAmbiguousBaseTimeExpectedUtc = fallbackOnAmbiguousBaseTimeExpectedUtc?
            .Select(x => DateTime.SpecifyKind(x, DateTimeKind.Utc)).ToList();
        var count = expectedUtc.Count;
        var naturalCronExpr = NaturalCronExpr.Parse(expression);

        // 1. UTC methods
        naturalCronExpr.TryGetNextOccurrencesInUtc(baseTimeUtc, count).Should().Equal(expectedUtc, $"UTC methods: {description}");

        var timeZoneRule = TimeZoneRuleRegex.Match(expression);
        if (timeZoneRule.Success)
        {
            ShouldReturnWithTimeZoneRule(naturalCronExpr, expression, timeZoneRule, baseTimeUtc, expectedUtc,
                fallbackOnAmbiguousBaseTimeExpectedUtc, description);
        }
        else
        {
            ShouldReturnWithoutTimeZoneRule(naturalCronExpr, baseTimeUtc, expectedUtc, dependsOnTheZoneOfTheMethod, hasCustomKeywords, description);
        }
    }

    // The expression has a tz rule: it always wins, whatever method is called.
    private static void ShouldReturnWithTimeZoneRule(NaturalCronExpr naturalCronExpr, string expression, Match timeZoneRule,
        DateTime baseTimeUtc, IReadOnlyList<DateTime> expectedUtc,
        IReadOnlyList<DateTime>? fallbackOnAmbiguousBaseTimeExpectedUtc, string description)
    {
        var count = expectedUtc.Count;
        var ruleZoneId = timeZoneRule.Groups["zone"].Value;
        var ruleZone = TZConvert.GetTimeZoneInfo(ruleZoneId);

        // 2. *InTz methods with the zone of the rule, and with other zones: the results come back in the zone of the method.
        // Compare in the method's zone rather than round-tripping back through ConvertTimeToUtc, because an Unspecified
        // wall-clock time in a repeated fall-back hour cannot carry which pass it came from (the non-DST OtherZones still verify the exact UTC instant).
        foreach (var zoneId in new[] { ruleZoneId }.Concat(OtherZones))
        {
            var zone = TZConvert.GetTimeZoneInfo(zoneId);
            var baseInZone = ToZone(baseTimeUtc, zone);
            var expectedForZone = NeedsAmbiguousWallClockFallback(baseInZone, expectedUtc, zone) && fallbackOnAmbiguousBaseTimeExpectedUtc != null
                ? fallbackOnAmbiguousBaseTimeExpectedUtc
                : expectedUtc;
            var occurrences = naturalCronExpr.TryGetNextOccurrencesInTz(baseInZone, count, zoneId);
            occurrences.Should().Equal(expectedForZone.Select(x => ToZone(x, zone)), $"InTz methods with {zoneId}: {description}");
        }

        // 3. *InTz methods without the rule: the zone of the method is the zone the expression is evaluated in.
        var withoutTimeZoneRule = NaturalCronExpr.Parse(expression.Remove(timeZoneRule.Index, timeZoneRule.Length));
        withoutTimeZoneRule.TimeZoneRule().Should().BeNull();
        var baseInRuleZone = ToZone(baseTimeUtc, ruleZone);
        var expectedForRuleZone = NeedsAmbiguousWallClockFallback(baseInRuleZone, expectedUtc, ruleZone) && fallbackOnAmbiguousBaseTimeExpectedUtc != null
            ? fallbackOnAmbiguousBaseTimeExpectedUtc
            : expectedUtc;
        var occurrencesInRuleZone = withoutTimeZoneRule.TryGetNextOccurrencesInTz(baseInRuleZone, count, ruleZoneId);
        occurrencesInRuleZone.Should().Equal(expectedForRuleZone.Select(x => ToZone(x, ruleZone)), $"InTz methods without the tz rule: {description}");

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

    private static bool NeedsAmbiguousWallClockFallback(DateTime baseInZone, IReadOnlyList<DateTime> expectedUtc, TimeZoneInfo zone)
    {
        if (zone.IsAmbiguousTime(baseInZone))
        {
            return true;
        }

        // Also true when expectedUtc crosses a repeated fall-back hour in zone so the wall-clock goes backwards/repeats.
        var previous = baseInZone;
        foreach (var utc in expectedUtc)
        {
            var wall = ToZone(utc, zone);
            if (wall <= previous)
            {
                return true;
            }

            previous = wall;
        }

        return false;
    }
}
