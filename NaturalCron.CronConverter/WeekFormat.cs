namespace NaturalCron.CronConverter;

public enum WeekFormat
{
    /// <summary>Sun=0, Mon=1, Tue=2, Wed=3, Thu=4, Fri=5, Sat=6 (Unix/POSIX default)</summary>
    ZeroToSix,

    /// <summary>Mon=1, Tue=2, Wed=3, Thu=4, Fri=5, Sat=6, Sun=7 (ISO 8601)</summary>
    OneToSeven,

    /// <summary>Three-letter names: SUN, MON, TUE, WED, THU, FRI, SAT</summary>
    ThreeLetterName,
}
