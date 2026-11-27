namespace NaturalCron.Utils;

internal static class TimeZoneUtil
{
    /// <summary>
    /// True when the wall-clock time does not exist in the zone (the clocks go forward over it).
    /// </summary>
    /// <remarks>
    /// TimeZoneInfo.IsInvalidTime is not enough on Linux for zones with a negative DST, such as Europe/Dublin:
    /// on 2025-03-30 it reports 01:30 as valid, with offset +01:00, which is 00:30 UTC, which is 00:30 in Dublin.
    /// A wall-clock time that does not come back the same after converting it to UTC and back does not exist.
    /// </remarks>
    internal static bool IsInvalidTime(TimeZoneInfo tz, DateTime wallClock)
    {
        var unspecified = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);
        if (tz.IsInvalidTime(unspecified))
        {
            return true;
        }

        if (!tz.SupportsDaylightSavingTime)
        {
            return false;
        }

        // Converting to UTC near the limits of DateTime would overflow. There is no gap there.
        if (unspecified < DateTime.MinValue.AddDays(2) || unspecified > DateTime.MaxValue.AddDays(-2))
        {
            return false;
        }

        var utc = DateTime.SpecifyKind(unspecified - tz.GetUtcOffset(unspecified), DateTimeKind.Utc);
        var backToWallClock = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);

        return backToWallClock != unspecified;
    }
}
