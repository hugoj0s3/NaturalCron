namespace NaturalCron.CronConverter.Internal.Strategies;

internal static class StrategyFactory
{
    internal static IClosestWeekdayStrategy Create(ClosestWeekdaySupport support)
        => support switch
        {
            ClosestWeekdaySupport.NearestWeekdayW => new QuartzWClosestWeekdayStrategy(),
            _ => new NotSupportedClosestWeekdayStrategy()
        };

    internal static INthWeekdayStrategy Create(NthWeekdaySupport support)
        => support switch
        {
            NthWeekdaySupport.NthHash => new QuartzHashNthWeekdayStrategy(),
            _ => new NotSupportedNthWeekdayStrategy()
        };
}
