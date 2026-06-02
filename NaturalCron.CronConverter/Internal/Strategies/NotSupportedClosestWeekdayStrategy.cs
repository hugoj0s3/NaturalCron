namespace NaturalCron.CronConverter.Internal.Strategies;

internal class NotSupportedClosestWeekdayStrategy : IClosestWeekdayStrategy
{
    public string? Format(int dayOfMonth) => null;
}
