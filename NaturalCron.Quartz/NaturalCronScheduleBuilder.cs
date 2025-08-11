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
        NaturalCronTriggerImpl triggerImpl = new NaturalCronTriggerImpl(naturalCronExpression);
        triggerImpl.MisfireInstruction = MisfireInstruction.CronTrigger.DoNothing;
        return triggerImpl;
    }
}