using NaturalCron.Tokens.Parser.Dtos;

namespace NaturalCron.Tokens.Parser.ParseSpecStrategies;

internal class ParseSpecStrategyFactory
{
    public static ParseRuleSpecStrategy Create(RuleSpecType type)
    {
        switch (type)
        {
            case RuleSpecType.Yearly:
                return new YearlyParseRuleSpecStrategy();
            case RuleSpecType.Monthly:
                return new MonthlyParseRuleSpecStrategy();
            case RuleSpecType.Weekly:
                return new WeeklyParseRuleSpecStrategy();
            case RuleSpecType.Daily:
                return new DailyParseRuleSpecStrategy();
            case RuleSpecType.Hourly:
                return new HourlyParseRuleSpecStrategy();
            case RuleSpecType.Minutely:
                return new MinutelyParseRuleSpecStrategy();
            case RuleSpecType.Secondly:
                return new SecondlyParseRuleSpecStrategy();
            case RuleSpecType.AtInOn:
                return new AtInOnParseRuleSpecStrategy();
            case RuleSpecType.Between:
                return new BetweenParseRuleSpecStrategy();
            case RuleSpecType.TimeZone:
                return new TimeZoneParseRuleSpecStrategy();
            case RuleSpecType.EveryX:
                return new EveryXParseRuleSpecStrategy();
            case RuleSpecType.UptoOrFrom:
                return new UptoFromParseRuleSpecStrategy();
            case RuleSpecType.Except:
                return new ExceptParseRuleSpecStrategy();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}