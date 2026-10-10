using FluentAssertions;

namespace NaturalCron.UnitTests;

/// <summary>
/// When the base time does not satisfy the other rules, "every X" must restart at the beginning of the next period
/// that does, without skipping the first occurrences of that period. The units below the every unit are kept,
/// e.g. "every hour on wednesday" from Tuesday 10:30 continues on Wednesday at 00:30.
/// The fixed cases are in GetNextOcurrencesTestCases.json, section 38.
/// </summary>
public class NaturalCronCadenceFromAnyBaseTimeTests
{
    private static DateTime Utc(string value) => DateTime.SpecifyKind(DateTime.Parse(value), DateTimeKind.Utc);

    // every <count> <unit>, with the rules written as a plain condition.
    private static readonly (string expression, int count, TimeSpan unit, Func<DateTime, bool> matches)[] Cases =
    {
        ("every second at 9am", 1, TimeSpan.FromSeconds(1), d => d.Hour == 9),
        ("every second on wednesday", 1, TimeSpan.FromSeconds(1), d => d.DayOfWeek == DayOfWeek.Wednesday),
        ("every second between 1am and 10am", 1, TimeSpan.FromSeconds(1), d => d.Hour is >= 1 and <= 10),
        ("every second on 25th", 1, TimeSpan.FromSeconds(1), d => d.Day == 25),
        ("every second on wednesday at 9am", 1, TimeSpan.FromSeconds(1), d => d.DayOfWeek == DayOfWeek.Wednesday && d.Hour == 9),
        ("every second on [monday, friday]", 1, TimeSpan.FromSeconds(1), d => d.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Friday),
        ("every second at [09:00, 18:00]", 1, TimeSpan.FromSeconds(1), d => d.Hour is 9 or 18 && d.Minute == 0),
        ("every minute on wednesday", 1, TimeSpan.FromMinutes(1), d => d.DayOfWeek == DayOfWeek.Wednesday),
        ("every minute at 9am", 1, TimeSpan.FromMinutes(1), d => d.Hour == 9),
        ("every minute on 25th", 1, TimeSpan.FromMinutes(1), d => d.Day == 25),
        ("every minute on wednesday at 9am", 1, TimeSpan.FromMinutes(1), d => d.DayOfWeek == DayOfWeek.Wednesday && d.Hour == 9),
        ("every hour on wednesday", 1, TimeSpan.FromHours(1), d => d.DayOfWeek == DayOfWeek.Wednesday),
        ("every hour on 25th", 1, TimeSpan.FromHours(1), d => d.Day == 25),
        ("every hour in january", 1, TimeSpan.FromHours(1), d => d.Month == 1),
        ("every 5 minutes on wednesday", 5, TimeSpan.FromMinutes(1), d => d.DayOfWeek == DayOfWeek.Wednesday),
        ("every 5 minutes at 9am", 5, TimeSpan.FromMinutes(1), d => d.Hour == 9),
        ("every 90 seconds on wednesday", 90, TimeSpan.FromSeconds(1), d => d.DayOfWeek == DayOfWeek.Wednesday),
        ("every 2 hours on wednesday", 2, TimeSpan.FromHours(1), d => d.DayOfWeek == DayOfWeek.Wednesday),
        ("every 7 hours on 25th", 7, TimeSpan.FromHours(1), d => d.Day == 25),
        ("every day in january", 1, TimeSpan.FromDays(1), d => d.Month == 1),
        ("every 3 days in january", 3, TimeSpan.FromDays(1), d => d.Month == 1),
    };

    public static IEnumerable<object[]> CaseIndexes() => Enumerable.Range(0, Cases.Length).Select(i => new object[] { i, Cases[i].expression });

    [Theory]
    [MemberData(nameof(CaseIndexes))]
    public void NextOccurrences_FromRandomBaseTimes_NeverSkipMatchingTimes(int index, string expression)
    {
        var (_, count, unit, matches) = Cases[index];
        var expr = NaturalCronExpr.Parse(expression);
        var random = new Random(1000 + index);
        var failures = new List<string>();

        for (var i = 0; i < 40; i++)
        {
            var baseTime = Utc("2024-12-22 00:00:00").AddSeconds(random.Next(0, 5 * 86400));
            var actual = expr.TryGetNextOccurrencesInUtc(baseTime, 4, baseTime.AddDays(90));

            // Reference: from a time that matches, the next candidate is one "every X" later. From a time that does not
            // match, it is one unit later. Either way, walk unit by unit until the rules are satisfied.
            var expected = new List<DateTime>();
            var current = baseTime;
            var limit = baseTime.AddDays(90);
            while (expected.Count < 4)
            {
                var candidate = current + TimeSpan.FromTicks(unit.Ticks * (matches(current) ? count : 1));
                while (!matches(candidate) && candidate < limit)
                {
                    candidate += unit;
                }

                if (candidate >= limit)
                {
                    break;
                }

                expected.Add(candidate);
                current = candidate;
            }

            if (!actual.SequenceEqual(expected))
            {
                failures.Add($"from {baseTime:yyyy-MM-dd HH:mm:ss}: got [{string.Join(", ", actual.Select(x => x.ToString("MM-dd HH:mm:ss")))}], " +
                             $"expected [{string.Join(", ", expected.Select(x => x.ToString("MM-dd HH:mm:ss")))}]");
            }
        }

        failures.Should().BeEmpty($"'{expression}' must not skip matching times");
    }
}
