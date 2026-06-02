namespace NaturalCron.CronConverter;

public class CronConverterOptions
{
    public static readonly CronConverterOptions Default = new();

    /// <summary>
    /// When true, the output includes a seconds field as the first field (6-field cron).
    /// Default: false (5-field standard cron).
    /// </summary>
    public bool SupportSeconds { get; set; }

    /// <summary>
    /// Controls how day-of-week values are rendered in the output.
    /// Default: <see cref="WeekFormat.ZeroToSix"/> (Sun=0 … Sat=6).
    /// </summary>
    public WeekFormat WeekFormat { get; set; } = WeekFormat.ZeroToSix;

    /// <summary>
    /// Controls how month values are rendered in the output.
    /// Default: <see cref="MonthFormat.Number"/> (1–12).
    /// </summary>
    public MonthFormat MonthFormat { get; set; } = MonthFormat.Number;

    /// <summary>
    /// Controls how "closest weekday to Nth" expressions are handled.
    /// Default: <see cref="ClosestWeekdaySupport.NotSupported"/>.
    /// </summary>
    public ClosestWeekdaySupport ClosestWeekdaySupport { get; set; } = ClosestWeekdaySupport.NotSupported;

    /// <summary>
    /// Controls how Nth-weekday-of-month expressions (e.g. "1stMonday") are handled.
    /// Default: <see cref="NthWeekdaySupport.NotSupported"/>.
    /// </summary>
    public NthWeekdaySupport NthWeekdaySupport { get; set; } = NthWeekdaySupport.NotSupported;

    /// <summary>
    /// Determines what happens when a NaturalCron feature cannot be represented in cron.
    /// Default: <see cref="NonConvertibleBehavior.ThrowException"/>.
    /// </summary>
    public NonConvertibleBehavior NonConvertibleBehavior { get; set; } = NonConvertibleBehavior.ThrowException;

    /// <summary>
    /// When true, applies Quartz DOM/DOW mutual-exclusion semantics: exactly one of the
    /// day-of-month and day-of-week fields must be <c>?</c> (no specific value).
    /// Required by Quartz.NET's <c>CronExpression</c> parser.
    /// Default: false.
    /// </summary>
    public bool DomDowMutualExclusion { get; set; }

    // ── Presets ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Preset for standard Unix/Linux crontab (5-field, Sun=0…Sat=6).
    /// No seconds, no Quartz extensions.
    /// </summary>
    public static CronConverterOptions ForCrontab(NonConvertibleBehavior nonConvertibleBehavior = NonConvertibleBehavior.ThrowException)
        => new()
        {
            WeekFormat = WeekFormat.ZeroToSix,
            MonthFormat = MonthFormat.Number,
            ClosestWeekdaySupport = ClosestWeekdaySupport.NotSupported,
            NthWeekdaySupport = NthWeekdaySupport.NotSupported,
            NonConvertibleBehavior = nonConvertibleBehavior
        };

    /// <summary>
    /// Preset for the Cronos .NET library (5-field, Sun=0…Sat=6/7).
    /// Cronos supports W (nearest weekday), L (last), and # (Nth occurrence) extensions.
    /// <paramref name="includeClosestAndNthWeekSupport"/> enables W and # when true (default).
    /// </summary>
    public static CronConverterOptions ForCronos(
        NonConvertibleBehavior nonConvertibleBehavior = NonConvertibleBehavior.ThrowException,
        bool includeClosestAndNthWeekSupport = true)
        => new()
        {
            WeekFormat = WeekFormat.ZeroToSix,
            MonthFormat = MonthFormat.Number,
            ClosestWeekdaySupport =
                includeClosestAndNthWeekSupport ? ClosestWeekdaySupport.NearestWeekdayW : ClosestWeekdaySupport.NotSupported,
            NthWeekdaySupport =
                includeClosestAndNthWeekSupport ? NthWeekdaySupport.NthHash : NthWeekdaySupport.NotSupported,
            NonConvertibleBehavior = nonConvertibleBehavior
        };

    /// <summary>
    /// Preset for Quartz.NET (6-field with seconds, three-letter weekday/month names,
    /// W notation for closest weekday, # notation for Nth weekday).
    /// </summary>
    public static CronConverterOptions ForQuartz(
        NonConvertibleBehavior nonConvertibleBehavior = NonConvertibleBehavior.ThrowException,
        bool includeClosestAndNthWeekSupport = true)
        => new()
        {
            SupportSeconds = true,
            WeekFormat = includeClosestAndNthWeekSupport ? WeekFormat.OneToSeven : WeekFormat.ThreeLetterName,
            MonthFormat = MonthFormat.ThreeLetterName,
            ClosestWeekdaySupport =
                includeClosestAndNthWeekSupport ? ClosestWeekdaySupport.NearestWeekdayW : ClosestWeekdaySupport.NotSupported,
            NthWeekdaySupport =
                includeClosestAndNthWeekSupport ? NthWeekdaySupport.NthHash : NthWeekdaySupport.NotSupported,
            DomDowMutualExclusion = true,
            NonConvertibleBehavior = nonConvertibleBehavior
        };
}
