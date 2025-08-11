using Quartz;

namespace NaturalCron.Quartz;

public interface INaturalCronTrigger : ITrigger
{
    string? NaturalCronExpression { get;  }
}