using Quartz;
using Quartz.Spi;

namespace NaturalCron.Quartz;

public class NaturalCronScheduleBuilder : ScheduleBuilder<INaturalCronTrigger>
{
    private readonly NaturalCronExpr naturalCronExpression;
    
    public NaturalCronScheduleBuilder(NaturalCronExpr naturalCronExpression)
    {
        this.naturalCronExpression = naturalCronExpression;
    }
    
    public override IMutableTrigger Build()
    {
        NaturalCronTrigger trigger = new NaturalCronTrigger(naturalCronExpression);
        trigger.MisfireInstruction = MisfireInstruction.CronTrigger.DoNothing;
        return trigger;
    }
}