using System;
using System.Collections.Generic;
using System.Linq;
using NaturalCron;
using FluentAssertions;
using TimeZoneConverter;
using Xunit.Abstractions;

namespace NaturalCron.UnitTests;

[TestCaseOrderer("NaturalCron.UnitTests.NumberCaseOrderer", "NaturalCron.UnitTests")]
public class NaturalCronNextOccurrenceInIanaTimeZoneIdProvidedTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public NaturalCronNextOccurrenceInIanaTimeZoneIdProvidedTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Theory]
    [MemberData(nameof(LoadNextOccurrenceTestCasesWithTimeZone))]
    public void GetNextOccurrencesInTz_ConsistentWithUtcMethods(
        string description, 
        string expression, 
        string baseTimeUtcStr,
        string[] expectedDateTimeStrs)
    {
        var (naturalCronExpr, errors) = NaturalCronExpr.TryParse(expression);
        errors.Should().BeEmpty();
        naturalCronExpr.Should().NotBeNull();
        
        var baseTimeUtc = DateTime.Parse(baseTimeUtcStr);
        var ianaTzId = "America/New_York";
        var tz = TZConvert.GetTimeZoneInfo(ianaTzId);
        var baseTimeInTz = TimeZoneInfo.ConvertTimeFromUtc(baseTimeUtc, tz);
        
        var expectedCount = expectedDateTimeStrs.Where(x => !string.IsNullOrEmpty(x)).Count();
        
        var utcResults = naturalCronExpr.TryGetNextOccurrencesInUtc(baseTimeUtc, expectedCount);
        var tzResults = naturalCronExpr.TryGetNextOccurrencesInTz(baseTimeInTz, expectedCount, ianaTzId);
        
        var utcResultsConvertedToTz = utcResults.Select(utc => TimeZoneInfo.ConvertTimeFromUtc(utc, tz)).ToList();
        
        tzResults.Should().Equal(utcResultsConvertedToTz);
        
        testOutputHelper.WriteLine($"UTC results converted to {ianaTzId}: {string.Join(", ", utcResultsConvertedToTz.Select(d => d.ToString("F")))}");
        testOutputHelper.WriteLine($"TZ results: {string.Join(", ", tzResults.Select(d => d.ToString("F")))}");
    }

    [Fact]
    public void GetNextOccurrenceInTz_SingleResult_ReturnsInSpecifiedTimeZone()
    {
        var naturalCronExpr = NaturalCronExpr.Parse("daily at 14:00");
        var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var ianaTzId = "America/Los_Angeles";
        
        var result = naturalCronExpr.GetNextOccurrenceInTz(baseTime, ianaTzId);
        
        result.Hour.Should().Be(14);
        
        testOutputHelper.WriteLine($"TimeZone: {ianaTzId}");
        testOutputHelper.WriteLine($"Result: {result:F}");
    }

    [Fact]
    public void TryGetNextOccurrenceInTz_NoResult_ReturnsNull()
    {
        var naturalCronExpr = NaturalCronExpr.Parse("daily at 14:00");
        var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var maxLookahead = baseTime.AddHours(1);
        var ianaTzId = "Europe/London";
        
        var result = naturalCronExpr.TryGetNextOccurrenceInTz(baseTime, ianaTzId, maxLookahead);
        
        result.Should().BeNull();
    }

    [Fact]
    public void GetNextOccurrenceInTz_WithTokyoTimezone_CalculatesInTokyoReturnsInProvidedTz()
    {
        var naturalCronExpr = NaturalCronExpr.Parse("daily at 09:00 tz Asia/Tokyo");
        var baseTime = new DateTime(2024, 1, 1, 15, 0, 0);
        var ianaTzId = "America/New_York";
        
        var result = naturalCronExpr.GetNextOccurrenceInTz(baseTime, ianaTzId);
        
        var tokyoTimeZone = TZConvert.GetTimeZoneInfo("Asia/Tokyo");
        var nyTimeZone = TZConvert.GetTimeZoneInfo(ianaTzId);
        var tokyo9am = new DateTime(2024, 1, 2, 9, 0, 0);
        var expectedNyTime = TimeZoneInfo.ConvertTime(tokyo9am, tokyoTimeZone, nyTimeZone);
        
        result.Should().Be(expectedNyTime);
        
        testOutputHelper.WriteLine($"Provided TimeZone: {ianaTzId}");
        testOutputHelper.WriteLine($"Tokyo 9:00 AM = {expectedNyTime:F} in {ianaTzId}");
        testOutputHelper.WriteLine($"Actual result: {result:F}");
    }

    [Fact]
    public void GetNextOccurrencesInTz_ThrowsWhenNotEnoughOccurrences()
    {
        var naturalCronExpr = NaturalCronExpr.Parse("daily at 14:00");
        var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var maxLookahead = baseTime.AddHours(1);
        var ianaTzId = "Europe/Paris";
        
        var action = () => naturalCronExpr.GetNextOccurrencesInTz(baseTime, 5, ianaTzId, maxLookahead);
        
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void TryGetNextOccurrenceInTz_InvalidIanaId_ThrowsArgumentException()
    {
        var naturalCronExpr = NaturalCronExpr.Parse("daily at 14:00");
        var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var invalidIanaTzId = "Invalid/TimeZone";
        
        Action action = () => naturalCronExpr.TryGetNextOccurrenceInTz(baseTime, invalidIanaTzId);
        
        ArgumentException exception = Assert.Throws<ArgumentException>(action);
        exception.Message.Should().Contain("Invalid IANA time zone ID");
    }

    [Fact]
    public void TryGetNextOccurrenceInTz_CaseInsensitiveIanaId_Works()
    {
        var naturalCronExpr = NaturalCronExpr.Parse("daily at 14:00");
        var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var ianaTzId = "aMeRiCa/NeW_YoRk";
        
        var result = naturalCronExpr.TryGetNextOccurrenceInTz(baseTime, ianaTzId);
        
        result.Should().NotBeNull();
        result.Value.Hour.Should().Be(14);
        
        testOutputHelper.WriteLine($"Case-insensitive TZ ID: {ianaTzId}");
        testOutputHelper.WriteLine($"Result: {result:F}");
    }

    [Fact]
    public void GetNextOccurrencesInTz_MultipleResults_ReturnsCorrectCount()
    {
        var naturalCronExpr = NaturalCronExpr.Parse("every 2 hours");
        var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var ianaTzId = "Europe/Lisbon";
        var count = 5;
        
        var results = naturalCronExpr.GetNextOccurrencesInTz(baseTime, count, ianaTzId);
        
        results.Should().HaveCount(count);
        results[0].Should().Be(new DateTime(2024, 1, 1, 12, 0, 0));
        results[1].Should().Be(new DateTime(2024, 1, 1, 14, 0, 0));
        results[2].Should().Be(new DateTime(2024, 1, 1, 16, 0, 0));
        
        testOutputHelper.WriteLine($"TimeZone: {ianaTzId}");
        testOutputHelper.WriteLine($"Results: {string.Join(", ", results.Select(d => d.ToString("F")))}");
    }

    [Fact]
    public void TryGetNextOccurrencesInTz_PartialResults_ReturnsAvailableOccurrences()
    {
        var naturalCronExpr = NaturalCronExpr.Parse("daily at 14:00");
        var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var maxLookahead = baseTime.AddDays(2);
        var ianaTzId = "Asia/Tokyo";
        var requestedCount = 10;
        
        var results = naturalCronExpr.TryGetNextOccurrencesInTz(baseTime, requestedCount, ianaTzId, maxLookahead);
        
        results.Should().HaveCountLessThan(requestedCount);
        results.Should().HaveCount(2);
        
        testOutputHelper.WriteLine($"TimeZone: {ianaTzId}");
        testOutputHelper.WriteLine($"Requested: {requestedCount}, Got: {results.Count}");
        testOutputHelper.WriteLine($"Results: {string.Join(", ", results.Select(d => d.ToString("F")))}");
    }

    public static IEnumerable<object[]> LoadNextOccurrenceTestCasesWithTimeZone()
    {
        return TestDataHelper.LoadNextOccurrenceTestCases(x => x.Expression.Contains("@tz") || x.Expression.Contains("@timezone"));
    }
}
