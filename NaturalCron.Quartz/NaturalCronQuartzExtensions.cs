using NaturalCron.Builder;
using NaturalCron.Builder.Selectors;
using Quartz;

namespace NaturalCron.Quartz;

public static class NaturalCronQuartzExtensions
{
    public static TriggerBuilder WithNaturalCronSchedule(this TriggerBuilder builder, string rawExpression)
    {
        return builder.WithNaturalCronSchedule(NaturalCronExpr.Parse(rawExpression));
    }
    
    public static TriggerBuilder WithNaturalCronSchedule(this TriggerBuilder builder, NaturalCronExpr naturalExpression)
    {
        var schedule = new NaturalCronScheduleBuilder(naturalExpression);
        return builder.WithSchedule(schedule);
    }
    
    public static TriggerBuilder WithNaturalCronSchedule(this TriggerBuilder builder, Func<INaturalCronStarterSelector, INaturalCronBuildSelector> action)
    {
        var initialSelector = NaturalCronBuilder.Start();
        var buildSelector = action(initialSelector);
        var builtExpression = buildSelector.Build();
        return builder.WithNaturalCronSchedule(builtExpression);
    }
}