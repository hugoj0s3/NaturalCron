using TimeZoneConverter;

namespace NaturalCron.Rules;

public class NaturalCronIanaTimeZoneRule : NaturalCronRule
{
    public override NaturalCronRuleSpec Spec => NaturalCronRuleSpec.TimeZone;

    public string IanaId { get; internal set; } = string.Empty;

    internal TimeZoneInfo TimeZoneInfo => TZConvert.GetTimeZoneInfo(IanaId);

    public DateTime ConvertFromUtc(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TZConvert.GetTimeZoneInfo(IanaId));
    }
    
    public DateTime ConvertToUtc(DateTime localDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, TZConvert.GetTimeZoneInfo(IanaId));
    }
}