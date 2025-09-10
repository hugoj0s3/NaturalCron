using NaturalCron;
using FluentAssertions;

namespace NaturalCron.UnitTests;

public class NaturalCronValidationTests
{
    [Theory]
    [InlineData("@every 2 days @on 5th", "You cannot use an At/On/In rules at the same time unit as an 'every X' rule except for week.")]
    [InlineData("@every Week AnchoredOn Friday @on Monday ", "You cannot combine an 'every week' + anchored rule with an At/On/In rule for weeks.")]
    [InlineData("@on monday @on tuesday", "Duplicate At/On/In rules for the same time unit are not allowed.")]
    [InlineData("@on monday @between 1st and 5th @between 10th and 15th", "Between rules is not allowed without Every/Secondly/Minutely/Hourly/Daily/Weekly/Monthly/Yearly rule.")]
    [InlineData("@every 0 days", "Every value cannot be less than or equal to 0.")]
    public void TryParse_InvalidExpressions_ReturnsExpectedError(string expression, string expectedError)
    {
        var (naturalCronExpr, errors) = NaturalCronExpr.TryParse(expression);
        naturalCronExpr.Should().BeNull();
        errors.Should().Contain(expectedError);
    }
    
    [Theory]
    [InlineData("@every week @foo bar")]
    public void TryParse_InvalidExpressions_General(string expression)
    {
        var (naturalCronExpr, errors) = NaturalCronExpr.TryParse(expression);
        naturalCronExpr.Should().BeNull();
        errors.Should().NotBeEmpty();
    }
}