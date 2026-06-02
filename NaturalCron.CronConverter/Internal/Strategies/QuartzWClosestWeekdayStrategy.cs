namespace NaturalCron.CronConverter.Internal.Strategies;

internal class QuartzWClosestWeekdayStrategy : IClosestWeekdayStrategy
{
    public string? Format(int dayOfMonth) => $"{dayOfMonth}W";
}
