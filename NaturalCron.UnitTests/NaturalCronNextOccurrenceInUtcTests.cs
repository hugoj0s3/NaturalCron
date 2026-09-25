using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NaturalCron;
using FluentAssertions;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace NaturalCron.UnitTests;

[TestCaseOrderer("NaturalCron.UnitTests.NumberCaseOrderer", "NaturalCron.UnitTests")]
public class NaturalCronNextOccurrenceInUtcTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public NaturalCronNextOccurrenceInUtcTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("daily at 09:00", 9)]
    [InlineData("daily at 09:00 tz UTC", 9)]
    [InlineData("daily at 09:00 tz America/New_York", 14)]
    public void UtcOccurrenceMethods_ReturnUtcKind(string expression, int expectedHour)
    {
        var schedule = NaturalCronExpr.Parse(expression);
        var baseTime = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var first = new DateTime(2025, 1, 15, expectedHour, 0, 0, DateTimeKind.Utc);

        var occurrences = new[]
            {
                schedule.TryGetNextOccurrenceInUtc(baseTime)!.Value,
                schedule.GetNextOccurrenceInUtc(baseTime)
            }
            .Concat(schedule.TryGetNextOccurrencesInUtc(baseTime, 2))
            .Concat(schedule.GetNextOccurrencesInUtc(baseTime, 2))
            .ToList();

        occurrences.Should().Equal(first, first, first, first.AddDays(1), first, first.AddDays(1));
        occurrences.Should().OnlyContain(occurrence => occurrence.Kind == DateTimeKind.Utc);
        occurrences.Select(occurrence => occurrence.ToUniversalTime()).Should().Equal(occurrences);
    }

    [Theory]
    [MemberData(nameof(LoadNextOccurrenceTestCases))]
    public void GetNextOccurrencesInUtc_ValidExpressions_Success(
        string description, 
        string expression, 
        string baseTimeUtcStr,
        string[] expectedDateTimeStrs)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var (naturalCronExpr, errors) = NaturalCronExpr.TryParse(expression);
        errors.Should().BeEmpty();
        naturalCronExpr.Should().NotBeNull();
        
        foreach (var ruleExpression in naturalCronExpr.Rules.Select(x => x.FullExpression))
        {
            ruleExpression.Should().NotBeNullOrEmpty();
        }

        var baseTimeUtc = DateTime.Parse(baseTimeUtcStr);

        var expectedDateTimeUtc = expectedDateTimeStrs.Where(x => !string.IsNullOrEmpty(x)).Select(x => DateTime.Parse(x)).ToList();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        
        IList<DateTime> nextOccurrences = new List<DateTime>();
        var task = Task.Run(() => nextOccurrences = naturalCronExpr.TryGetNextOccurrencesInUtc(baseTimeUtc, expectedDateTimeStrs.Length), cts.Token);
        task.Wait(cts.Token);

        nextOccurrences.Should().HaveCount(expectedDateTimeUtc.Count);
        nextOccurrences.Should().Equal(expectedDateTimeUtc);
        nextOccurrences.Should().OnlyContain(occurrence => occurrence.Kind == DateTimeKind.Utc);
        
        stopwatch.Stop();
        testOutputHelper.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
        stopwatch.Elapsed.TotalMilliseconds.Should().BeLessThan(2000);
    }
    
    public static IEnumerable<object[]> LoadNextOccurrenceTestCases()
    {
        return TestDataHelper.LoadNextOccurrenceTestCases();
    }
}
