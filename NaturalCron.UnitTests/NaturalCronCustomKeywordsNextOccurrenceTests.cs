using System.Text.RegularExpressions;
using FluentAssertions;
using NaturalCron.CustomRules;

namespace NaturalCron.UnitTests;

/// <summary>
/// Runs the cases of GetNextOcurrencesCustomKeywordsTestCases.json, with and without @.
/// The keywords of the expressions are defined in <see cref="CustomKeywordsForJsonTests"/>.
/// </summary>
[Collection(CustomKeywordsCollection.Name)]
public class NaturalCronCustomKeywordsNextOccurrenceTests : IDisposable
{
    public NaturalCronCustomKeywordsNextOccurrenceTests()
    {
        NaturalCronCustomKeywords.Reset();
        NaturalCronCustomKeywords.Configure(CustomKeywordsForJsonTests.Configure);
    }

    public void Dispose()
    {
        NaturalCronCustomKeywords.Reset();
    }

    [Theory]
    [MemberData(nameof(LoadCustomKeywordsTestCases))]
    public void GetNextOccurrencesInUtc_CustomKeywordExpressions_Success(
        string description,
        string expression,
        string baseTimeUtcStr,
        string[] expectedDateTimeStrs)
    {
        var (naturalCronExpr, errors) = NaturalCronExpr.TryParse(expression);
        errors.Should().BeEmpty(description);
        naturalCronExpr.Should().NotBeNull();

        foreach (var ruleExpression in naturalCronExpr!.Rules.Select(x => x.FullExpression))
        {
            ruleExpression.Should().NotBeNullOrEmpty();
        }

        var baseTimeUtc = DateTime.SpecifyKind(DateTime.Parse(baseTimeUtcStr), DateTimeKind.Utc);
        var expectedDateTimeUtc = expectedDateTimeStrs
            .Select(x => DateTime.SpecifyKind(DateTime.Parse(x), DateTimeKind.Utc))
            .ToList();

        var nextOccurrences = naturalCronExpr.TryGetNextOccurrencesInUtc(baseTimeUtc, expectedDateTimeStrs.Length);

        nextOccurrences.Should().Equal(expectedDateTimeUtc, description);
        nextOccurrences.Should().OnlyContain(occurrence => occurrence.Kind == DateTimeKind.Utc);
    }

    // The keywords of CustomKeywordsForJsonTests that look at the time zone they are evaluated in.
    private static readonly Regex TimeZoneAwareKeywordRegex =
        new("usHoliday|jpHoliday|localHoliday|nyHoliday|nyMarketOpen", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Every case must give the same occurrences through the UTC methods, the *InTz methods and the local methods.
    /// For the cases with a time zone rule it also removes the rule and passes the zone to the *InTz methods instead,
    /// which checks that the keywords receive the zone of the method.
    /// </summary>
    [Theory]
    [MemberData(nameof(LoadCustomKeywordsTestCases))]
    public void AllOccurrenceMethods_CustomKeywordExpressions_ReturnTheExpectedOccurrences(
        string description,
        string expression,
        string baseTimeUtcStr,
        string[] expectedDateTimeStrs)
    {
        AllOccurrenceMethodsAssert.ShouldReturn(
            expression,
            DateTime.Parse(baseTimeUtcStr),
            expectedDateTimeStrs.Select(x => DateTime.Parse(x)).ToList(),
            dependsOnTheZoneOfTheMethod: TimeZoneAwareKeywordRegex.IsMatch(expression),
            hasCustomKeywords: true,
            description);
    }

    public static IEnumerable<object[]> LoadCustomKeywordsTestCases()
    {
        return TestDataHelper.LoadNextOccurrenceTestCases(fileName: "GetNextOcurrencesCustomKeywordsTestCases.json");
    }
}
