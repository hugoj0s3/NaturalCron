using Cronos;
using NCrontab;
using QuartzCron = Quartz.CronExpression;
using CronosCron = Cronos.CronExpression;

namespace NaturalCron.CronConverter.Tests;

/// <summary>
/// Verifies that converting a NaturalCron expression to classic cron and parsing it
/// with a real cron library produces the same next occurrences as NaturalCron itself.
/// </summary>
public class CronLibraryAlignmentTests
{
    // Monday 2024-01-01 00:00:00 UTC — clean starting point with known day-of-week.
    private static readonly DateTime BaseTime = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const int OccurrenceCount = 5;

    // ── NCrontab ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every day at 18:00")]
    [InlineData("every 30 minutes")]
    [InlineData("every month on 15th at 09:00")]
    [InlineData("every day between monday and friday at 18:00")]
    [InlineData("every 2 hours")]
    public void NCrontab_NextOccurrences_AlignWithNaturalCron(string expression)
    {
        var ncExpr = NaturalCronExpr.Parse(expression);
        var naturalCronOccurrences = ncExpr.GetNextOccurrencesInUtc(BaseTime, OccurrenceCount)
            .Select(TruncateToMinute)
            .ToList();

        var cronStr = ncExpr.ToCronExpression(CronConverterOptions.ForCrontab());
        var schedule = CrontabSchedule.Parse(cronStr);
        var ncrontabOccurrences = GetNcrontabOccurrences(schedule, OccurrenceCount);

        ncrontabOccurrences.Should().Equal(naturalCronOccurrences);
    }

    // ── Cronos ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every day at 18:00")]
    [InlineData("every 30 minutes")]
    [InlineData("every month on 15th at 09:00")]
    [InlineData("every day between monday and friday at 18:00")]
    [InlineData("every 2 hours")]
    public void Cronos_NextOccurrences_AlignWithNaturalCron(string expression)
    {
        var ncExpr = NaturalCronExpr.Parse(expression);
        var naturalCronOccurrences = ncExpr.GetNextOccurrencesInUtc(BaseTime, OccurrenceCount)
            .Select(TruncateToMinute)
            .ToList();

        var cronStr = ncExpr.ToCronExpression(CronConverterOptions.ForCronos());
        var cronosExpr = CronosCron.Parse(cronStr);
        var cronosOccurrences = GetCronosOccurrences(cronosExpr, OccurrenceCount);

        cronosOccurrences.Should().Equal(naturalCronOccurrences);
    }

    // Cronos supports W and # natively (DOW 0=Sun…6=Sat matches ZeroToSix).
    [Fact]
    public void Cronos_ClosestWeekdayTo15_AlignWithNaturalCron()
    {
        const string expression = "every month on ClosestWeekdayTo 15th at 09:00";
        var ncExpr = NaturalCronExpr.Parse(expression);
        var naturalCronOccurrences = ncExpr.GetNextOccurrencesInUtc(BaseTime, OccurrenceCount)
            .Select(TruncateToMinute)
            .ToList();

        var cronStr = ncExpr.ToCronExpression(CronConverterOptions.ForCronos());
        var cronosExpr = CronosCron.Parse(cronStr, CronFormat.Standard);
        var cronosOccurrences = GetCronosOccurrences(cronosExpr, OccurrenceCount);

        cronosOccurrences.Should().Equal(naturalCronOccurrences);
    }

    // Cronos DOW 1=Monday matches ZeroToSix Mon=1, so 1stMonday → "1#1" aligns.
    [Fact]
    public void Cronos_1stMondayAt9_AlignWithNaturalCron()
    {
        const string expression = "every month on 1stMonday at 09:00";
        var ncExpr = NaturalCronExpr.Parse(expression);
        var naturalCronOccurrences = ncExpr.GetNextOccurrencesInUtc(BaseTime, OccurrenceCount)
            .Select(TruncateToMinute)
            .ToList();

        var cronStr = ncExpr.ToCronExpression(CronConverterOptions.ForCronos());
        var cronosExpr = CronosCron.Parse(cronStr, CronFormat.Standard);
        var cronosOccurrences = GetCronosOccurrences(cronosExpr, OccurrenceCount);

        cronosOccurrences.Should().Equal(naturalCronOccurrences);
    }

    [Fact]
    public void Cronos_LastFridayAt9_AlignWithNaturalCron()
    {
        const string expression = "every month on LastFriday at 09:00";
        var ncExpr = NaturalCronExpr.Parse(expression);
        var naturalCronOccurrences = ncExpr.GetNextOccurrencesInUtc(BaseTime, OccurrenceCount)
            .Select(TruncateToMinute)
            .ToList();

        var cronStr = ncExpr.ToCronExpression(CronConverterOptions.ForCronos());
        var cronosExpr = CronosCron.Parse(cronStr, CronFormat.Standard);
        var cronosOccurrences = GetCronosOccurrences(cronosExpr, OccurrenceCount);

        cronosOccurrences.Should().Equal(naturalCronOccurrences);
    }

    // ── Quartz ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every day at 18:00")]
    [InlineData("every 30 minutes")]
    [InlineData("every month on 15th at 09:00")]
    [InlineData("every 2 hours")]
    public void Quartz_NextOccurrences_AlignWithNaturalCron(string expression)
    {
        var ncExpr = NaturalCronExpr.Parse(expression);
        var naturalCronOccurrences = ncExpr.GetNextOccurrencesInUtc(BaseTime, OccurrenceCount)
            .Select(TruncateToMinute)
            .ToList();

        var cronStr = ncExpr.ToCronExpression(CronConverterOptions.ForQuartz());
        var quartzExpr = new QuartzCron(cronStr) { TimeZone = TimeZoneInfo.Utc };
        var quartzOccurrences = GetQuartzOccurrences(quartzExpr, OccurrenceCount);

        quartzOccurrences.Should().Equal(naturalCronOccurrences);
    }

    // W notation is DOM-based — no DOW numbering involved, so Quartz aligns cleanly.
    [Fact]
    public void Quartz_ClosestWeekdayTo15_AlignWithNaturalCron()
    {
        const string expression = "every month on ClosestWeekdayTo 15th at 09:00";
        var ncExpr = NaturalCronExpr.Parse(expression);
        var naturalCronOccurrences = ncExpr.GetNextOccurrencesInUtc(BaseTime, OccurrenceCount)
            .Select(TruncateToMinute)
            .ToList();

        var cronStr = ncExpr.ToCronExpression(CronConverterOptions.ForQuartz());
        var quartzExpr = new QuartzCron(cronStr) { TimeZone = TimeZoneInfo.Utc };
        var quartzOccurrences = GetQuartzOccurrences(quartzExpr, OccurrenceCount);

        quartzOccurrences.Should().Equal(naturalCronOccurrences);
    }

    // Three-letter weekday names (MON, WED, FRI) are parsed correctly by Quartz
    // without any numbering ambiguity, so comma-separated weekdays align cleanly.
    [Fact]
    public void Quartz_MultipleWeekdays_ThreeLetterName_AlignWithNaturalCron()
    {
        const string expression = "every day at 10:00 on [monday, wednesday, friday]";
        var ncExpr = NaturalCronExpr.Parse(expression);
        var naturalCronOccurrences = ncExpr.GetNextOccurrencesInUtc(BaseTime, OccurrenceCount)
            .Select(TruncateToMinute)
            .ToList();

        // includeClosestAndNthWeekSupport: false → ThreeLetterName format for DOW
        var cronStr = ncExpr.ToCronExpression(CronConverterOptions.ForQuartz(includeClosestAndNthWeekSupport: false));
        var quartzExpr = new QuartzCron(cronStr) { TimeZone = TimeZoneInfo.Utc };
        var quartzOccurrences = GetQuartzOccurrences(quartzExpr, OccurrenceCount);

        quartzOccurrences.Should().Equal(naturalCronOccurrences);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<DateTime> GetNcrontabOccurrences(CrontabSchedule schedule, int count)
    {
        var result = new List<DateTime>(count);
        var current = BaseTime;
        for (var i = 0; i < count; i++)
        {
            current = schedule.GetNextOccurrence(current);
            result.Add(current);
        }
        return result;
    }

    private static List<DateTime> GetCronosOccurrences(CronosCron expr, int count)
    {
        var result = new List<DateTime>(count);
        var current = new DateTimeOffset(BaseTime, TimeSpan.Zero);
        for (var i = 0; i < count; i++)
        {
            var next = expr.GetNextOccurrence(current, TimeZoneInfo.Utc);
            if (next == null) break;
            result.Add(next.Value.UtcDateTime);
            current = next.Value;
        }
        return result;
    }

    private static List<DateTime> GetQuartzOccurrences(QuartzCron expr, int count)
    {
        var result = new List<DateTime>(count);
        var current = new DateTimeOffset(BaseTime, TimeSpan.Zero);
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
}
