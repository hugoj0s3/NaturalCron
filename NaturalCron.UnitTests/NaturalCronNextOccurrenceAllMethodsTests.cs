namespace NaturalCron.UnitTests;

/// <summary>
/// Runs every case of GetNextOcurrencesTestCases.json, with and without @, through the UTC methods,
/// the *InTz methods and the local methods.
/// </summary>
public class NaturalCronNextOccurrenceAllMethodsTests
{
    [Theory]
    [MemberData(nameof(LoadNextOccurrenceTestCases))]
    public void AllOccurrenceMethods_ReturnTheExpectedOccurrences(
        string description,
        string expression,
        string baseTimeUtcStr,
        string[] expectedDateTimeStrs)
    {
        var expectedUtc = expectedDateTimeStrs.Where(x => !string.IsNullOrEmpty(x)).Select(x => DateTime.Parse(x)).ToList();

        AllOccurrenceMethodsAssert.ShouldReturn(
            expression,
            DateTime.Parse(baseTimeUtcStr),
            expectedUtc,
            dependsOnTheZoneOfTheMethod: false,
            hasCustomKeywords: false,
            description);
    }

    public static IEnumerable<object[]> LoadNextOccurrenceTestCases()
    {
        return TestDataHelper.LoadNextOccurrenceTestCases();
    }
}
