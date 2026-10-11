namespace NaturalCron.Tokens.Parser.Dtos;

internal enum RuleSpecType
{
    EveryX = 1,
    AtInOn,
    Between,
    UptoOrFrom,
    TimeZone,
    Secondly,
    Minutely,
    Hourly,
    Daily,
    Monthly,
    Yearly,
    Weekly,
    Except
}