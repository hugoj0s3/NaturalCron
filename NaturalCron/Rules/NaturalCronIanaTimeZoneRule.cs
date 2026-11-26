using TimeZoneConverter;

namespace NaturalCron.Rules;

public class NaturalCronIanaTimeZoneRule : NaturalCronRule
{
    public override NaturalCronRuleSpec Spec => NaturalCronRuleSpec.TimeZone;

    public string IanaId { get; internal set; } = string.Empty;

    internal TimeZoneInfo TimeZoneInfo => TZConvert.GetTimeZoneInfo(IanaId);

    [Obsolete("No longer used internally. Use TimeZoneInfo with IanaId directly; this method will be removed in 2.0.")]
    public DateTime ConvertFromUtc(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TZConvert.GetTimeZoneInfo(IanaId));
    }

    [Obsolete("No longer used internally and does not handle DST transitions. Use TimeZoneInfo with IanaId directly; this method will be removed in 2.0.")]
    public DateTime ConvertToUtc(DateTime localDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, TZConvert.GetTimeZoneInfo(IanaId));
    }
}
