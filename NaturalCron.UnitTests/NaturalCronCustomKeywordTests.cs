using FluentAssertions;
using NaturalCron.CustomRules;
using NaturalCron.Rules;
using NaturalCron.Tokens;
using NaturalCron.Tokens.Parser;
using TimeZoneConverter;

namespace NaturalCron.UnitTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public class CustomKeywordsCollection
{
    public const string Name = "CustomKeywords";
}

[Collection(CustomKeywordsCollection.Name)]
public class NaturalCronCustomKeywordTests : IDisposable
{
    private static readonly HashSet<DateTime> Holidays = new()
    {
        new DateTime(2024, 12, 25),
        new DateTime(2025, 1, 1),
    };

    public NaturalCronCustomKeywordTests()
    {
        NaturalCronCustomKeywords.Reset();
    }

    public void Dispose()
    {
        NaturalCronCustomKeywords.Reset();
    }

    private static void ConfigureDefaults(Action<NaturalCronCustomKeywordsBuilder>? extra = null)
    {
        NaturalCronCustomKeywords.Configure(cfg =>
        {
            cfg.AddMatchable("holiday", (dt, _) => Holidays.Contains(dt.Date));
            cfg.AddMatchable("payday2", (dt, _) => dt.Day == 15);
            extra?.Invoke(cfg);
        });
    }

    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    // ── Configure ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("1abc")]
    [InlineData("ab-c")]
    [InlineData("ab c")]
    [InlineData("abc\n")]
    [InlineData("")]
    [InlineData("mon")]
    [InlineData("Day")]
    [InlineData("last")]
    [InlineData("UTC")]
    [InlineData("except")]
    [InlineData("every")]
    public void Configure_InvalidOrBuiltInKeyword_ThrowsAndAppliesNothing(string keyword)
    {
        var act = () => NaturalCronCustomKeywords.Configure(cfg =>
        {
            cfg.AddMatchable("holiday", (_, _) => true);
            cfg.AddMatchable(keyword, (_, _) => true);
        });

        act.Should().Throw<ArgumentException>();
        NaturalCronCustomKeywords.IsConfigured.Should().BeFalse();
        NaturalCronExpr.TryParse("every day on holiday at 09:00").errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Configure_DuplicateKeywordIgnoringCase_Throws()
    {
        var act = () => NaturalCronCustomKeywords.Configure(cfg =>
        {
            cfg.AddMatchable("holiday", (_, _) => true);
            cfg.AddMatchable("HOLIDAY", (_, _) => true);
        });

        act.Should().Throw<ArgumentException>().WithMessage("*more than once*");
    }

    [Fact]
    public void Configure_CalledTwice_Throws()
    {
        ConfigureDefaults();

        var act = () => NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("other", (_, _) => true));

        act.Should().Throw<InvalidOperationException>();
        NaturalCronCustomKeywords.IsConfigured.Should().BeTrue();
    }

    // ── Tokenizer ─────────────────────────────────────────────────────────────

    [Fact]
    public void Tokenizer_KeywordWithDigits_IsSingleCustomKeywordToken()
    {
        ConfigureDefaults();

        var tokens = TokenParserUtil.Tokenizer("on payday2 at 10:00");

        tokens.Should().ContainSingle(x => x.Type == NaturalCronTokenType.CustomKeyword)
            .Which.Value.Should().Be("payday2");
    }

    [Fact]
    public void Tokenizer_KeywordWithDifferentCase_IsCustomKeywordToken()
    {
        ConfigureDefaults();

        var tokens = TokenParserUtil.Tokenizer("on HOLIDAY");

        tokens.Should().Contain(x => x.Type == NaturalCronTokenType.CustomKeyword && x.Value == "HOLIDAY");
    }

    // ── Occurrences ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every day on holiday at 09:00")]
    [InlineData("@every day @on HOLIDAY @at 09:00")]
    [InlineData("every day in holiday at 09:00")]
    public void On_CustomKeyword_MatchesOnlyThoseDays(string expression)
    {
        ConfigureDefaults();

        var occurrences = NaturalCronExpr.Parse(expression)
            .GetNextOccurrencesInUtc(Utc(2024, 12, 20), 2);

        occurrences.Should().Equal(Utc(2024, 12, 25, 9), Utc(2025, 1, 1, 9));
    }

    [Theory]
    [InlineData("every day at 09:00 except holiday")]
    [InlineData("@every day @at 09:00 @except holiday")]
    public void Except_CustomKeyword_SkipsThoseDays(string expression)
    {
        ConfigureDefaults();

        var occurrences = NaturalCronExpr.Parse(expression)
            .GetNextOccurrencesInUtc(Utc(2024, 12, 24, 10), 7);

        occurrences.Should().Equal(
            Utc(2024, 12, 26, 9), Utc(2024, 12, 27, 9), Utc(2024, 12, 28, 9), Utc(2024, 12, 29, 9),
            Utc(2024, 12, 30, 9), Utc(2024, 12, 31, 9), Utc(2025, 1, 2, 9));
    }

    [Fact]
    public void On_BracketList_MatchesAnyKeyword()
    {
        ConfigureDefaults();

        var occurrences = NaturalCronExpr.Parse("every day on [holiday, payday2] at 09:00")
            .GetNextOccurrencesInUtc(Utc(2024, 12, 10), 4);

        occurrences.Should().Equal(Utc(2024, 12, 15, 9), Utc(2024, 12, 25, 9), Utc(2025, 1, 1, 9), Utc(2025, 1, 15, 9));
    }

    [Fact]
    public void Except_BracketList_SkipsAllKeywords()
    {
        ConfigureDefaults(cfg => cfg.AddMatchable("weekend", (dt, _) => dt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));

        var occurrences = NaturalCronExpr.Parse("every day at 09:00 except [holiday, weekend]")
            .GetNextOccurrencesInUtc(Utc(2024, 12, 23), 4);

        // 2024-12-25 is a holiday, 28/29 are weekend.
        occurrences.Should().Equal(Utc(2024, 12, 23, 9), Utc(2024, 12, 24, 9), Utc(2024, 12, 26, 9), Utc(2024, 12, 27, 9));
    }

    [Fact]
    public void Except_CustomKeywordAndWeekdays_AsSeparateRules()
    {
        ConfigureDefaults();

        var occurrences = NaturalCronExpr.Parse("every day at 09:00 except holiday except [saturday, sunday]")
            .GetNextOccurrencesInUtc(Utc(2024, 12, 23), 4);

        // 2024-12-25 is a holiday, 28/29 are weekend.
        occurrences.Should().Equal(Utc(2024, 12, 23, 9), Utc(2024, 12, 24, 9), Utc(2024, 12, 26, 9), Utc(2024, 12, 27, 9));
    }

    [Fact]
    public void On_HourGranularRule_AdvancesWithinTheDay()
    {
        NaturalCronCustomKeywords.Configure(cfg =>
            cfg.AddMatchable("window", (dt, _) => dt.Hour == 14, NaturalCronTimeUnit.Hour));

        var occurrences = NaturalCronExpr.Parse("every 30 minutes on window")
            .GetNextOccurrencesInUtc(Utc(2025, 1, 15), 3);

        occurrences.Should().Equal(Utc(2025, 1, 15, 14), Utc(2025, 1, 15, 14, 30), Utc(2025, 1, 16, 14));
    }

    // ── Rule instances ────────────────────────────────────────────────────────

    [Fact]
    public void AddMatchableOfT_UsesParameterlessConstructor()
    {
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable<FifteenthRule>("fifteenth"));

        var next = NaturalCronExpr.Parse("every day on fifteenth at 09:00").GetNextOccurrenceInUtc(Utc(2025, 1, 1));

        next.Should().Be(Utc(2025, 1, 15, 9));
    }

    [Fact]
    public void AddFactory_FactoryInvokedOncePerParse()
    {
        var calls = 0;
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("fifteenth", () =>
        {
            calls++;
            return new FifteenthRule();
        }));

        var expr = NaturalCronExpr.Parse("every day on fifteenth at 09:00");
        expr.GetNextOccurrencesInUtc(Utc(2025, 1, 1), 3);
        NaturalCronExpr.Parse("every day at 09:00 except fifteenth");

        calls.Should().Be(2);
    }

    [Fact]
    public void AddMatchWithoutTimeZone_MatchesWallClock()
    {
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("fifteenth", dt => dt.Day == 15));

        var next = NaturalCronExpr.Parse("every day on fifteenth at 09:00").GetNextOccurrenceInUtc(Utc(2025, 1, 1));

        next.Should().Be(Utc(2025, 1, 15, 9));
    }

    // ── every <keyword> ───────────────────────────────────────────────────────

    [Theory]
    // The every unit is the keyword's time unit: holiday is a day, window an hour, quarter a month.
    [InlineData("every holiday at 09:00", "every day on holiday at 09:00")]
    [InlineData("every HOLIDAY at 09:00", "every day on holiday at 09:00")]
    [InlineData("@every holiday @at 09:00 @tz Asia/Tokyo", "@every day @on holiday @at 09:00 @tz Asia/Tokyo")]
    [InlineData("every [holiday, payday2] at 09:00", "every day on [holiday, payday2] at 09:00")]
    [InlineData("every [holiday, 15th] at 09:00", "every day on [holiday, 15th] at 09:00")]
    [InlineData("every [holiday, friday] at 09:00", "every day on [holiday, friday] at 09:00")]
    [InlineData("every holiday at 09:00 except friday", "every day on holiday at 09:00 except friday")]
    [InlineData("every window", "every hour on window")]
    [InlineData("every quarter on 1st at 09:00", "every month on quarter on 1st at 09:00")]
    public void EveryKeyword_IsTheSameAsEveryUnitOnKeyword(string shorthand, string explicitExpression)
    {
        ConfigureDefaults(cfg => cfg
            .AddMatchable("window", dt => dt.Hour == 14, NaturalCronTimeUnit.Hour)
            .AddMatchable("quarter", dt => dt.Month is 1 or 4 or 7 or 10, NaturalCronTimeUnit.Month));

        var shorthandExpr = NaturalCronExpr.Parse(shorthand);
        var explicitExpr = NaturalCronExpr.Parse(explicitExpression);

        foreach (var baseTime in new[] { Utc(2024, 12, 20), new DateTime(2024, 12, 24, 13, 37, 11, DateTimeKind.Utc) })
        {
            var expected = explicitExpr.TryGetNextOccurrencesInUtc(baseTime, 2);

            expected.Should().HaveCount(2);
            shorthandExpr.TryGetNextOccurrencesInUtc(baseTime, 2).Should().Equal(expected);
        }
    }

    [Fact]
    public void EveryKeyword_ReturnsEveryMatchingDay()
    {
        ConfigureDefaults();

        var occurrences = NaturalCronExpr.Parse("every holiday at 09:00").GetNextOccurrencesInUtc(Utc(2024, 12, 20), 2);

        occurrences.Should().Equal(Utc(2024, 12, 25, 9), Utc(2025, 1, 1, 9));
    }

    [Fact]
    public void EveryKeyword_NotConfigured_IsInvalidToken()
    {
        var (expr, errors) = NaturalCronExpr.TryParse("every holiday at 09:00");

        expr.Should().BeNull();
        errors.Should().Contain(x => x.Contains("Invalid token"));
    }

    // ── Several keywords for the same implementation ──────────────────────────

    [Theory]
    [InlineData("every day on holiday at 09:00")]
    [InlineData("every day on holidays at 09:00")]
    [InlineData("every day on HOLIDAYS at 09:00")]
    [InlineData("every day on [holiday, holidays] at 09:00")]
    public void AddMatchable_SeveralKeywords_AllUseTheSameMatch(string expression)
    {
        NaturalCronCustomKeywords.Configure(cfg =>
            cfg.AddMatchable(["holiday", "holidays"], dt => Holidays.Contains(dt.Date)));

        var occurrences = NaturalCronExpr.Parse(expression).GetNextOccurrencesInUtc(Utc(2024, 12, 20), 2);

        occurrences.Should().Equal(Utc(2024, 12, 25, 9), Utc(2025, 1, 1, 9));
    }

    [Theory]
    [InlineData("every day at 09:00 except holiday")]
    [InlineData("every day at 09:00 except holidays")]
    [InlineData("every day at 09:00 except [holiday, holidays]")]
    public void AddMatchable_SeveralKeywords_ExceptWorksWithEitherKeyword(string expression)
    {
        NaturalCronCustomKeywords.Configure(cfg =>
            cfg.AddMatchable(["holiday", "holidays"], dt => Holidays.Contains(dt.Date)));

        var occurrences = NaturalCronExpr.Parse(expression).GetNextOccurrencesInUtc(Utc(2024, 12, 24, 10), 2);

        // 2024-12-25 is a holiday.
        occurrences.Should().Equal(Utc(2024, 12, 26, 9), Utc(2024, 12, 27, 9));
    }

    [Theory]
    [InlineData("every day on [holiday, 20th] at 09:00")]
    [InlineData("every day on [holidays, 20th] at 09:00")]
    public void AddMatchable_SeveralKeywords_MixedListWorksWithEitherKeyword(string expression)
    {
        NaturalCronCustomKeywords.Configure(cfg =>
            cfg.AddMatchable(["holiday", "holidays"], dt => Holidays.Contains(dt.Date)));

        var occurrences = NaturalCronExpr.Parse(expression).GetNextOccurrencesInUtc(Utc(2024, 12, 10), 3);

        occurrences.Should().Equal(Utc(2024, 12, 20, 9), Utc(2024, 12, 25, 9), Utc(2025, 1, 1, 9));
    }

    [Theory]
    [InlineData("holiday")]
    [InlineData("holidays")]
    public void AddMatchable_SeveralKeywords_EachKeywordReceivesTheExpressionZone(string keyword)
    {
        TimeZoneInfo? received = null;
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable(["holiday", "holidays"], (dt, tz) =>
        {
            received = tz;
            return Holidays.Contains(dt.Date);
        }));

        var next = NaturalCronExpr.Parse($"every day on {keyword} at 09:00 tz Asia/Tokyo").GetNextOccurrenceInUtc(Utc(2024, 12, 30));

        // 2025-01-01 09:00 in Tokyo is 2025-01-01 00:00 UTC.
        next.Should().Be(Utc(2025, 1, 1, 0));
        received!.Id.Should().Be(TZConvert.GetTimeZoneInfo("Asia/Tokyo").Id);
    }

    [Fact]
    public void AddMatchable_SeveralKeywords_FactoryInvokedOncePerKeywordUsed()
    {
        var calls = 0;
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable(new[] { "fifteenth", "midmonth" }, () =>
        {
            calls++;
            return new FifteenthRule();
        }));

        NaturalCronExpr.Parse("every day on fifteenth at 09:00");
        calls.Should().Be(1);

        NaturalCronExpr.Parse("every day on [fifteenth, midmonth] at 09:00");
        calls.Should().Be(3);
    }

    [Fact]
    public void AddMatchable_SeveralKeywords_WorksForEveryOverload()
    {
        NaturalCronCustomKeywords.Configure(cfg => cfg
            .AddMatchable<FifteenthRule>(new[] { "fifteenth", "midmonth" })
            .AddMatchable(new List<string> { "payday", "paydays" }, () => new FifteenthRule())
            .AddMatchable(["window", "windows"], (dt, _) => dt.Hour == 14, NaturalCronTimeUnit.Hour));

        foreach (var keyword in new[] { "fifteenth", "midmonth", "payday", "paydays" })
        {
            NaturalCronExpr.Parse($"every day on {keyword} at 09:00").GetNextOccurrenceInUtc(Utc(2025, 1, 1))
                .Should().Be(Utc(2025, 1, 15, 9));
        }

        foreach (var keyword in new[] { "window", "windows" })
        {
            NaturalCronExpr.Parse($"every 30 minutes on {keyword}").GetNextOccurrenceInUtc(Utc(2025, 1, 15))
                .Should().Be(Utc(2025, 1, 15, 14));
        }
    }

    [Fact]
    public void AddMatchable_SeveralKeywords_EmptyOrNullList_Throws()
    {
        var empty = () => NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable(Array.Empty<string>(), _ => true));
        var nullList = () => NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable((IEnumerable<string>)null!, _ => true));

        empty.Should().Throw<ArgumentException>().WithMessage("*At least one keyword*");
        nullList.Should().Throw<ArgumentNullException>();
        NaturalCronCustomKeywords.IsConfigured.Should().BeFalse();
    }

    [Theory]
    [InlineData("holiday", "holiday")]
    [InlineData("holiday", "HOLIDAY")]
    [InlineData("holiday", "mon")]
    [InlineData("holiday", "holi-day")]
    public void AddMatchable_SeveralKeywords_DuplicateOrInvalidKeyword_ThrowsAndAppliesNothing(string first, string second)
    {
        var act = () => NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable(new[] { first, second }, _ => true));

        act.Should().Throw<ArgumentException>();
        NaturalCronCustomKeywords.IsConfigured.Should().BeFalse();
        NaturalCronExpr.TryParse("every day on holiday at 09:00").errors.Should().NotBeEmpty();
    }

    [Fact]
    public void AddFactory_TimeZoneTimeUnit_ReturnsParseError()
    {
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("broken", () => new BrokenTimeUnitRule()));

        var (expr, errors) = NaturalCronExpr.TryParse("every day on broken at 09:00");

        expr.Should().BeNull();
        errors.Should().ContainSingle().Which.Should().Contain("invalid time unit");
    }

    // ── Time zones ────────────────────────────────────────────────────────────

    [Fact]
    public void InUtc_WithTzRule_RuleReceivesWallClockAndZoneOfTheExpression()
    {
        var received = new List<(DateTime dateTime, TimeZoneInfo timeZone)>();
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("holiday", (dt, tz) =>
        {
            received.Add((dt, tz));
            return Holidays.Contains(dt.Date);
        }));

        var next = NaturalCronExpr.Parse("every day on holiday at 09:00 tz Asia/Tokyo")
            .GetNextOccurrenceInUtc(Utc(2024, 12, 30));

        // 2025-01-01 09:00 in Tokyo is 2025-01-01 00:00 UTC.
        next.Should().Be(Utc(2025, 1, 1, 0));
        received.Should().OnlyContain(x => x.timeZone.Id == TZConvert.GetTimeZoneInfo("Asia/Tokyo").Id);
        received.Should().Contain(x => x.dateTime == new DateTime(2025, 1, 1, 9, 0, 0));
    }

    [Fact]
    public void InUtc_WithoutTzRule_RuleReceivesUtc()
    {
        TimeZoneInfo? received = null;
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("holiday", (dt, tz) =>
        {
            received = tz;
            return Holidays.Contains(dt.Date);
        }));

        NaturalCronExpr.Parse("every day on holiday at 09:00").GetNextOccurrenceInUtc(Utc(2024, 12, 20));

        received.Should().Be(TimeZoneInfo.Utc);
    }

    [Fact]
    public void InTz_WithoutTzRule_RuleReceivesProvidedZone()
    {
        TimeZoneInfo? received = null;
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("holiday", (dt, tz) =>
        {
            received = tz;
            return Holidays.Contains(dt.Date);
        }));

        var next = NaturalCronExpr.Parse("every day on holiday at 09:00")
            .GetNextOccurrenceInTz(new DateTime(2024, 12, 20), "Asia/Tokyo");

        next.Should().Be(new DateTime(2024, 12, 25, 9, 0, 0));
        received!.Id.Should().Be(TZConvert.GetTimeZoneInfo("Asia/Tokyo").Id);
    }

    [Fact]
    public void Local_WithoutTzRule_RuleReceivesLocalZone()
    {
        TimeZoneInfo? received = null;
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("holiday", (dt, tz) =>
        {
            received = tz;
            return Holidays.Contains(dt.Date);
        }));

        NaturalCronExpr.Parse("every day on holiday at 09:00")
            .GetNextOccurrence(new DateTime(2024, 12, 20, 0, 0, 0, DateTimeKind.Local));

        received.Should().Be(TimeZoneInfo.Local);
    }

    [Fact]
    public void SameExpression_DifferentZones_DoesNotReuseCachedMatch()
    {
        // Matches only when evaluated in UTC, to detect a stale cached result across zones.
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("utconly", (_, tz) => tz.Equals(TimeZoneInfo.Utc)));
        var expr = NaturalCronExpr.Parse("every day on utconly at 09:00");

        expr.TryGetNextOccurrenceInUtc(Utc(2025, 1, 1)).Should().Be(Utc(2025, 1, 1, 9));
        expr.TryGetNextOccurrenceInTz(new DateTime(2025, 1, 1), "Asia/Tokyo", new DateTime(2025, 1, 10))
            .Should().BeNull();
    }

    [Fact]
    public void SameExpression_DifferentZones_ExceptDoesNotReuseCachedMatch()
    {
        // Matches only when evaluated in UTC, to detect a stale cached result across zones.
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("utconly", (_, tz) => tz.Equals(TimeZoneInfo.Utc)));
        var expr = NaturalCronExpr.Parse("every day at 09:00 except utconly");

        // In UTC every occurrence is excluded, the last one checked is 2025-01-01 09:00.
        expr.TryGetNextOccurrenceInUtc(Utc(2025, 1, 1), Utc(2025, 1, 1, 12)).Should().BeNull();
        expr.TryGetNextOccurrenceInTz(new DateTime(2025, 1, 1), "Asia/Tokyo")
            .Should().Be(new DateTime(2025, 1, 1, 9, 0, 0));
    }

    // ── Lists mixing custom keywords and built-in values ──────────────────────

    [Fact]
    public void On_MixedList_MatchesKeywordOrBuiltInValue()
    {
        ConfigureDefaults();

        var occurrences = NaturalCronExpr.Parse("every day on [holiday, 20th] at 09:00")
            .GetNextOccurrencesInUtc(Utc(2024, 12, 10), 4);

        occurrences.Should().Equal(Utc(2024, 12, 20, 9), Utc(2024, 12, 25, 9), Utc(2025, 1, 1, 9), Utc(2025, 1, 20, 9));
    }

    [Fact]
    public void On_MixedList_WithWeekday()
    {
        ConfigureDefaults();

        var occurrences = NaturalCronExpr.Parse("every day on [holiday, friday] at 09:00")
            .GetNextOccurrencesInUtc(Utc(2024, 12, 23), 4);

        // 2024-12-25 and 2025-01-01 are holidays (Wednesdays), 12-27 and 01-03 are Fridays.
        occurrences.Should().Equal(Utc(2024, 12, 25, 9), Utc(2024, 12, 27, 9), Utc(2025, 1, 1, 9), Utc(2025, 1, 3, 9));
    }

    [Fact]
    public void On_MixedList_SeveralKeywordsAndBuiltInValues()
    {
        ConfigureDefaults();

        var occurrences = NaturalCronExpr.Parse("every day on [20th, holiday, 22nd, payday2] at 09:00")
            .GetNextOccurrencesInUtc(Utc(2024, 12, 10), 5);

        occurrences.Should().Equal(
            Utc(2024, 12, 15, 9), Utc(2024, 12, 20, 9), Utc(2024, 12, 22, 9), Utc(2024, 12, 25, 9), Utc(2025, 1, 1, 9));
    }

    [Fact]
    public void Except_MixedList_SkipsKeywordAndBuiltInValue()
    {
        ConfigureDefaults();

        var occurrences = NaturalCronExpr.Parse("every day at 09:00 except [holiday, friday]")
            .GetNextOccurrencesInUtc(Utc(2024, 12, 23), 4);

        // 2024-12-25 is a holiday, 12-27 is a Friday.
        occurrences.Should().Equal(Utc(2024, 12, 23, 9), Utc(2024, 12, 24, 9), Utc(2024, 12, 26, 9), Utc(2024, 12, 28, 9));
    }

    [Fact]
    public void On_MixedList_RuleReceivesExpressionZone()
    {
        TimeZoneInfo? received = null;
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("holiday", (dt, tz) =>
        {
            received = tz;
            return Holidays.Contains(dt.Date);
        }));

        NaturalCronExpr.Parse("every day on [holiday, 20th] at 09:00 tz Asia/Tokyo").GetNextOccurrenceInUtc(Utc(2024, 12, 10));

        received!.Id.Should().Be(TZConvert.GetTimeZoneInfo("Asia/Tokyo").Id);
    }

    [Fact]
    public void On_MixedList_AcrossLines()
    {
        ConfigureDefaults();

        var occurrences = NaturalCronExpr.Parse("every day at 09:00 on\n[holiday,\n20th]")
            .GetNextOccurrencesInUtc(Utc(2024, 12, 10), 2);

        occurrences.Should().Equal(Utc(2024, 12, 20, 9), Utc(2024, 12, 25, 9));
    }

    [Fact]
    public void Add_UndefinedTimeUnit_ReturnsParseError()
    {
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("broken", _ => true, (NaturalCronTimeUnit)42));

        var (expr, errors) = NaturalCronExpr.TryParse("every day on broken at 09:00");

        expr.Should().BeNull();
        errors.Should().ContainSingle().Which.Should().Contain("invalid time unit");
    }

    [Fact]
    public void Except_EverythingExcludedWithoutLookahead_ReturnsNullAtEndOfCalendar()
    {
        NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable("always", _ => true));
        var nearEnd = DateTime.SpecifyKind(DateTime.MaxValue.AddDays(-3), DateTimeKind.Utc);

        NaturalCronExpr.Parse("every day at 09:00 except always").TryGetNextOccurrenceInUtc(nearEnd).Should().BeNull();
    }

    [Fact]
    public void Parse_MixedList_ExposesAnyOfRule()
    {
        ConfigureDefaults();

        var expr = NaturalCronExpr.Parse("every day on [holiday, 20th] at 09:00");

        var anyOfRule = expr.Rules.OfType<NaturalCronAnyOfRule>().Should().ContainSingle().Subject;
        anyOfRule.Rules.Should().HaveCount(2);
        anyOfRule.Rules.OfType<NaturalCronCustomKeywordRule>().Should().ContainSingle()
            .Which.Keywords.Should().Equal("holiday");
    }

    // ── Errors ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("every day on holiday 15th at 09:00", "Custom keywords can only be combined with other values inside brackets")]
    [InlineData("every day on [holiday 15th] at 09:00", "A custom keyword cannot be combined with other values in the same list entry")]
    [InlineData("every day on holiday [15th, 20th] at 09:00", "Custom keywords must be inside the brackets")]
    [InlineData("every day on [holiday, foo] at 09:00", "Invalid token")]
    [InlineData("every day on holiday payday2 at 09:00", "Multiple custom keywords must be inside brackets")]
    [InlineData("every day on foo at 09:00", "Invalid token")]
    public void InvalidExpressions_ReturnErrors(string expression, string expectedError)
    {
        ConfigureDefaults();

        var (expr, errors) = NaturalCronExpr.TryParse(expression);

        expr.Should().BeNull();
        errors.Should().Contain(x => x.Contains(expectedError));
    }

    [Fact]
    public void NotConfigured_KeywordIsInvalidToken()
    {
        var (expr, errors) = NaturalCronExpr.TryParse("every day on holiday at 09:00");

        expr.Should().BeNull();
        errors.Should().Contain(x => x.Contains("Invalid token"));
    }

    private sealed class FifteenthRule : INaturalCronMatchableCustomKeyword
    {
        public NaturalCronTimeUnit TimeUnit => NaturalCronTimeUnit.Day;
        public bool Match(DateTime dateTimeInTz, TimeZoneInfo timeZone) => dateTimeInTz.Day == 15;
    }

    private sealed class BrokenTimeUnitRule : INaturalCronMatchableCustomKeyword
    {
        public NaturalCronTimeUnit TimeUnit => NaturalCronTimeUnit.TimeZone;
        public bool Match(DateTime dateTimeInTz, TimeZoneInfo timeZone) => true;
    }
}
