namespace NaturalCron.Builder.Selectors;

/// <summary>
/// Build operations interface for the NaturalCron fluent builder API.
/// Provides methods to finalize and output the constructed expression,
/// either as a validated NaturalCronExpr or as a natural language string.
/// </summary>
public interface INaturalCronBuildSelector
{
    /// <summary>
    /// Builds and validates the constructed expression, returning a NaturalCronExpr instance.
    /// Throws an ArgumentException if the expression syntax is invalid.
    /// </summary>
    /// <returns>A validated NaturalCronExpr instance ready for scheduling</returns>
    /// <exception cref="ArgumentException">Thrown when the expression is invalid</exception>
    NaturalCronExpr Build();

    /// <summary>
    /// Returns the natural language expression string without parsing or validation.
    /// Useful for debugging and logging (e.g. "every day at 18:00").
    /// </summary>
    /// <returns>Natural language expression string (may be invalid)</returns>
    string ToNaturalExpression();

    /// <summary>
    /// Returns the natural language expression string without parsing or validation.
    /// Useful for debugging and logging (e.g. "every day at 18:00").
    /// </summary>
    [Obsolete("Use ToNaturalExpression() instead. 'Raw' is misleading — some users expect it to return a classic cron string.")]
    string ToRawExpression();
}