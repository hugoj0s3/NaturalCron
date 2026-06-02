namespace NaturalCron.CronConverter.Internal;

internal class CronFields
{
    public string Second = "0";
    public string Minute = "*";
    public string Hour = "*";
    public string Dom = "*";
    public string Month = "*";
    public string Dow = "*";

    public bool SecondSet = false;
    public bool MinuteSet = false;
    public bool HourStepSet = false;
    public bool DomSet = false;
    public bool DowSet = false;

    public string Build(bool supportSeconds, bool quartzDomDowSemantics = false)
    {
        var minute = Minute;
        if (HourStepSet && !MinuteSet)
            minute = "0";

        var dom = Dom;
        var dow = Dow;

        if (quartzDomDowSemantics)
        {
            // Quartz requires exactly one of DOM/DOW to be "?".
            // If DOW is explicitly set → DOM becomes "?"; otherwise DOW becomes "?".
            dom = DowSet ? "?" : dom;
            dow = DowSet ? dow : "?";
        }

        return supportSeconds
            ? $"{Second} {minute} {Hour} {dom} {Month} {dow}"
            : $"{minute} {Hour} {dom} {Month} {dow}";
    }
}
