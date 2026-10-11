using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace NaturalCron.UnitTests;

public static class TestDataHelper
{
    /// <summary>
    /// Loads test cases from the embedded JSON resource and returns them with both @ and without @ variations
    /// </summary>
    /// <param name="filterExpression">Optional filter to apply to expressions (e.g., x => x.Expression.Contains("@tz"))</param>
    /// <param name="fileName">The embedded JSON file with the test cases</param>
    /// <returns>Enumerable of test case object arrays for xUnit Theory tests</returns>
    public static IEnumerable<object[]> LoadNextOccurrenceTestCases(
        Func<NextOccurrenceTestCase, bool>? filterExpression = null,
        string fileName = "GetNextOcurrencesTestCases.json")
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "NaturalCron.UnitTests." + fileName;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new Exception($"Resource {resourceName} not found");
        }
        
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var cases = JsonSerializer.Deserialize<List<NextOccurrenceTestCase>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
        
        if (cases == null)
        {
            throw new Exception($"Failed to deserialize {resourceName}");
        }

        // Apply filter if provided
        if (filterExpression != null)
        {
            cases = cases.Where(filterExpression).ToList();
        }
        
        // Test both with and without @ symbols
        var testCasesWithoutAmpersat = cases
            .Select(x => new object[] { x.Description.Replace("@", " ") + " Without ampersat", x.Expression.Replace("@", " "), x.BaseTimeUtcStr, x.ExpectedDateTimeStrs });
        var testCasesWithAmpersat = cases
            .Select(x => new object[] { x.Description + " With ampersat", x.Expression, x.BaseTimeUtcStr, x.ExpectedDateTimeStrs });
        
        return testCasesWithoutAmpersat.Concat(testCasesWithAmpersat);
    }
}

public class NextOccurrenceTestCase
{
    public string Description { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public string BaseTimeUtcStr { get; set; } = string.Empty;
    public string[] ExpectedDateTimeStrs { get; set; } = Array.Empty<string>();
}
