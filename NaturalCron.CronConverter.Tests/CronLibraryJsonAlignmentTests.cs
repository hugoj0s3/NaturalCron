using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NCrontab;
using Xunit.Abstractions;
using CronosCron = Cronos.CronExpression;
using QuartzCron = Quartz.CronExpression;

namespace NaturalCron.CronConverter.Tests;

/// <summary>
/// Data-driven alignment tests against GetNextOcurrencesTestCases.json.
/// Each convertible case is run through Cronos and compared with NaturalCron's own expected output.
/// Cases are skipped automatically when:
///   - TryConvert reports errors (non-convertible features)
///   - The minute field is "*" — no explicit time means cron fires every minute,
///     while NaturalCron fires at the interval granularity (once per day/week/month)
///   - ClosestWeekdayTo day >= 29 — Cronos skips months where that day doesn't exist,
///     while NaturalCron computes the nearest available day
/// </summary>
public sealed class CronLibraryJsonAlignmentTests(ITestOutputHelper output)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Regex HighDayW = new(@"\b(29|30|31)W\b", RegexOptions.Compiled);

    // ── Main alignment test ───────────────────────────────────────────────────

    [Fact]
    public void Cronos_JsonTestCases_AlignWithNaturalCron()
    {
        var testCases = LoadTestCases();
        var options = CronConverterOptions.ForCronos();

        var tested = 0;
        var skipped = 0;
        var failures = new List<string>();

        foreach (var tc in testCases)
        {
            var convertResult = NaturalCronToCronConverter.TryConvert(tc.Expression, options);
            if (!convertResult.IsSuccess || convertResult.CronExpression == null)
            {
                skipped++;
                continue;
            }

            // Skip when minute field is "*": cron fires every minute but NaturalCron
            // fires at interval granularity (once per day, per week, per month, etc.)
            var minuteField = convertResult.CronExpression.Split(' ')[0];
            if (minuteField == "*")
            {
                skipped++;
                continue;
            }

            // Skip ClosestWeekdayTo day >= 29: semantic difference between Cronos (skips
            // months without that day) and NaturalCron (finds nearest available day)
            if (HighDayW.IsMatch(convertResult.CronExpression))
            {
                skipped++;
                continue;
            }

            CronosCron cronosExpr;
            try
            {
                cronosExpr = CronosCron.Parse(convertResult.CronExpression);
            }
            catch
            {
                skipped++;
                continue;
            }

            var baseTime = ParseUtc(tc.BaseTimeUtcStr);
            var expected = tc.ExpectedDateTimeStrs
                .Select(ParseUtc)
                .Select(TruncateToMinute)
                .ToList();

            var actual = GetCronosOccurrences(cronosExpr, expected.Count, new DateTimeOffset(baseTime, TimeSpan.Zero))
                .Select(TruncateToMinute)
                .ToList();

            if (!actual.SequenceEqual(expected))
            {
                failures.Add(
                    $"[{tc.Description}]\n" +
                    $"  Expression : {tc.Expression}\n" +
                    $"  Cron       : {convertResult.CronExpression}\n" +
                    $"  Expected   : {string.Join(", ", expected)}\n" +
                    $"  Got        : {string.Join(", ", actual)}");
            }
            else
            {
                tested++;
            }
        }

        failures.Should().BeEmpty(because: $"\n{string.Join("\n\n", failures)}");

        output.WriteLine($"JSON alignment: {tested} tested, {skipped} skipped (of {testCases.Count} total)");

        tested.Should().BeGreaterThan(0,
            because: $"Expected at least some aligned cases (skipped: {skipped})");
    }

    // ── Converter-specific test cases ────────────────────────────────────────

    /// <summary>
    /// All cases in CronAlignmentTestCases.json are intentionally designed to convert
    /// cleanly. For each case, NaturalCron's own occurrences are compared against Cronos
    /// — no hardcoded expected values. assertCount drives how many to verify, and
    /// cross-boundary cases use base times near the end of a constrained period.
    /// </summary>
    [Fact]
    public void Cronos_ConverterSpecificCases_AlignWithNaturalCron()
    {
        var testCases = LoadConverterTestCases();
        var options = CronConverterOptions.ForCronos();
        var failures = new List<string>();

        foreach (var tc in testCases)
        {
            var ncExpr = NaturalCronExpr.Parse(tc.Expression);
            var cronStr = ncExpr.ToCronExpression(options);
            var cronosExpr = CronosCron.Parse(cronStr);

            var baseTime = ParseUtc(tc.BaseTimeUtcStr);

            var naturalCronOccurrences = ncExpr.GetNextOccurrencesInUtc(baseTime, tc.AssertCount)
                .Select(TruncateToMinute)
                .ToList();

            var cronosOccurrences = GetCronosOccurrences(cronosExpr, tc.AssertCount, new DateTimeOffset(baseTime, TimeSpan.Zero))
                .Select(TruncateToMinute)
                .ToList();

            if (!cronosOccurrences.SequenceEqual(naturalCronOccurrences))
                failures.Add(
                    $"[{tc.Description}]\n" +
                    $"  Expression  : {tc.Expression}\n" +
                    $"  Cron        : {cronStr}\n" +
                    $"  NaturalCron : {string.Join(", ", naturalCronOccurrences.Select(d => d.ToString("s")))}\n" +
                    $"  Cronos      : {string.Join(", ", cronosOccurrences.Select(d => d.ToString("s")))}");
        }

        output.WriteLine($"Converter cases: {testCases.Count - failures.Count} passed, {failures.Count} failed");
        failures.Should().BeEmpty(because: "\n" + string.Join("\n\n", failures));
    }

    [Fact]
    public void NCrontab_ConverterSpecificCases_AlignWithNaturalCron()
    {
        var testCases = LoadConverterTestCases();
        var options = CronConverterOptions.ForCrontab();
        var failures = new List<string>();

        foreach (var tc in testCases)
        {
            var ncExpr = NaturalCronExpr.Parse(tc.Expression);
            var cronStr = ncExpr.ToCronExpression(options);
            var schedule = CrontabSchedule.Parse(cronStr);

            var baseTime = ParseUtc(tc.BaseTimeUtcStr);

            var naturalCronOccurrences = ncExpr.GetNextOccurrencesInUtc(baseTime, tc.AssertCount)
                .Select(TruncateToMinute)
                .ToList();

            var ncrontabOccurrences = GetNcrontabOccurrences(schedule, tc.AssertCount, baseTime)
                .Select(TruncateToMinute)
                .ToList();

            if (!ncrontabOccurrences.SequenceEqual(naturalCronOccurrences))
                failures.Add(
                    $"[{tc.Description}]\n" +
                    $"  Expression  : {tc.Expression}\n" +
                    $"  Cron        : {cronStr}\n" +
                    $"  NaturalCron : {string.Join(", ", naturalCronOccurrences.Select(d => d.ToString("s")))}\n" +
                    $"  NCrontab    : {string.Join(", ", ncrontabOccurrences.Select(d => d.ToString("s")))}");
        }

        output.WriteLine($"NCrontab converter cases: {testCases.Count - failures.Count} passed, {failures.Count} failed");
        failures.Should().BeEmpty(because: "\n" + string.Join("\n\n", failures));
    }

    [Fact]
    public void Quartz_ConverterSpecificCases_AlignWithNaturalCron()
    {
        var testCases = LoadConverterTestCases();
        // Three-letter DOW names (MON, TUE, …) avoid Quartz's numeric DOW ambiguity.
        var options = CronConverterOptions.ForQuartz(includeClosestAndNthWeekSupport: false);
        var failures = new List<string>();

        foreach (var tc in testCases)
        {
            var ncExpr = NaturalCronExpr.Parse(tc.Expression);
            var cronStr = ncExpr.ToCronExpression(options);
            var quartzExpr = new QuartzCron(cronStr) { TimeZone = TimeZoneInfo.Utc };

            var baseTime = ParseUtc(tc.BaseTimeUtcStr);

            var naturalCronOccurrences = ncExpr.GetNextOccurrencesInUtc(baseTime, tc.AssertCount)
                .Select(TruncateToMinute)
                .ToList();

            var quartzOccurrences = GetQuartzOccurrences(quartzExpr, tc.AssertCount, new DateTimeOffset(baseTime, TimeSpan.Zero))
                .Select(TruncateToMinute)
                .ToList();

            if (!quartzOccurrences.SequenceEqual(naturalCronOccurrences))
                failures.Add(
                    $"[{tc.Description}]\n" +
                    $"  Expression  : {tc.Expression}\n" +
                    $"  Cron        : {cronStr}\n" +
                    $"  NaturalCron : {string.Join(", ", naturalCronOccurrences.Select(d => d.ToString("s")))}\n" +
                    $"  Quartz      : {string.Join(", ", quartzOccurrences.Select(d => d.ToString("s")))}");
        }

        output.WriteLine($"Quartz converter cases: {testCases.Count - failures.Count} passed, {failures.Count} failed");
        failures.Should().BeEmpty(because: "\n" + string.Join("\n\n", failures));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<DateTime> GetCronosOccurrences(CronosCron expr, int count, DateTimeOffset baseTime)
    {
        var result = new List<DateTime>(count);
        var current = baseTime;
        for (var i = 0; i < count; i++)
        {
            var next = expr.GetNextOccurrence(current, TimeZoneInfo.Utc);
            if (next == null) break;
            result.Add(next.Value.UtcDateTime);
            current = next.Value;
        }
        return result;
    }

    private static List<DateTime> GetNcrontabOccurrences(CrontabSchedule schedule, int count, DateTime baseTime)
    {
        var result = new List<DateTime>(count);
        var current = baseTime;
        for (var i = 0; i < count; i++)
        {
            current = schedule.GetNextOccurrence(current);
            result.Add(current);
        }
        return result;
    }

    private static List<DateTime> GetQuartzOccurrences(QuartzCron expr, int count, DateTimeOffset baseTime)
    {
        var result = new List<DateTime>(count);
        var current = baseTime;
        for (var i = 0; i < count; i++)
        {
            var next = expr.GetNextValidTimeAfter(current);
            if (next == null) break;
            result.Add(next.Value.UtcDateTime);
            current = next.Value;
        }
        return result;
    }

    private static DateTime TruncateToMinute(DateTime dt)
        => new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);

    private static DateTime ParseUtc(string s)
        => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
            .ToUniversalTime();

    private static List<ConverterTestCase> LoadConverterTestCases()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CronAlignmentTestCases.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<ConverterTestCase>>(json, JsonOptions)!;
    }

    private static List<TestCase> LoadTestCases()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "NaturalCron.sln")))
            dir = Path.GetDirectoryName(dir);

        var path = Path.Combine(dir!, "NaturalCron.UnitTests", "GetNextOcurrencesTestCases.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<TestCase>>(json, JsonOptions)!;
    }

    // For GetNextOcurrencesTestCases.json — has expectedDateTimeStrs, no assertCount.
    private sealed class TestCase
    {
        public string Description { get; init; } = string.Empty;
        public string Expression { get; init; } = string.Empty;
        public string BaseTimeUtcStr { get; init; } = string.Empty;

        // Get-only: System.Text.Json populates the existing list rather than replacing it.
        [JsonPropertyName("expectedDateTimeStrs")]
        public List<string> ExpectedDateTimeStrs { get; } = [];
    }

    // For CronAlignmentTestCases.json — has assertCount, no expectedDateTimeStrs.
    // Positional record: constructor parameters are always recognised as used by the IDE.
    private sealed record ConverterTestCase(
        string Description,
        string Expression,
        string BaseTimeUtcStr,
        int AssertCount
    );
}
