namespace NaturalCron;

public enum NaturalCronRuleSpec
{
    Every = 1,
    Between,
    AtInOn,
    TimeZone,
    Except,
    Custom = 999,
}