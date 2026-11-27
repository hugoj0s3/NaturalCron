# Changelog

## [1.1.1]
### Fixed
- Daylight saving time (DST) transitions are now handled across all occurrence methods (`*InUtc`, `*InTz`, and local) ([#10](https://github.com/hugoj0s3/NaturalCron/issues/10)):
  - **Clocks go forward (spring-forward gap):** fixed and daily+ schedules landing in a skipped wall-clock hour no longer throw `ArgumentException`; they move forward to the first valid moment after the gap (e.g. `every day at 02:30 tz America/New_York` runs at `03:00 EDT` on 2025-03-09, and `02:15` runs at `02:30` in 30-minute DST zones like `Australia/Lord_Howe`). Sub-day intervals (`every 30 minutes`, `every hour`) advance past the gap while keeping their real cadence.
  - **Clocks go back (fall-back repeated hour):** fixed and daily+ schedules in a repeated hour run once, in the first pass (e.g. `every day at 01:30 tz America/New_York` runs at `01:30 EDT` / `05:30 UTC`), and do not run again when queried from the second pass (`01:15 EST`). Pure sub-day intervals (`every 30 minutes`, `every hour`, `every 15 minutes`) keep their real cadence across both passes without skipping occurrences (e.g. `every 30 minutes tz America/New_York` runs at 05:00, 05:30, 06:00, 06:30 and 07:00 UTC on 2025-11-02).
  - **Known limitations** (only in the repeated hour, on the night the clocks go back; see [Troubleshooting](docs/api-reference.md#daylight-saving-time-dst)):
    - Sub-day intervals combined with additional rules (e.g. `on sunday`, `between`, `anchoredAt`, `except`) are evaluated on wall-clock time and run only in the first pass of the repeated hour (e.g. `every 30 minutes on sunday tz America/New_York` runs at 05:00 and 05:30 UTC, then 07:00 UTC).
    - The `…InTz` methods take and return wall-clock times, which cannot tell the two passes apart: each repeated wall-clock time is returned once, and chained calls always move forward. Use the `…InUtc` methods to get both passes.

### Changed
- The UTC, local and `…InTz` occurrence methods now share a single internal flow: the base time is converted to the zone the expression is evaluated in (its `tz` rule, otherwise the zone of the method), the next occurrence is searched there, and the result is converted to the zone and `Kind` of the method. Apart from the `Kind` validation below, the results are unchanged, except with a `tz` rule and no max lookahead: the local methods search up to `DateTime.MaxValue`. Previously they stopped a few hours before it, on 9999-12-31.
- The `Kind` of the base time is now read, and the `Kind` of the result chosen, the same way whether the expression has a `tz` rule or not. Adding or removing `tz …` from an expression no longer changes how `Kind` is handled. The changes are listed below.
- **Stricter validation:** the UTC methods (`TryGetNextOccurrenceInUtc()`, `GetNextOccurrenceInUtc()`, `TryGetNextOccurrencesInUtc()`, `GetNextOccurrencesInUtc()`) now throw `ArgumentException` when the base time or the max lookahead is `DateTimeKind.Local`, e.g. `DateTime.Now`. Pass a UTC time instead, e.g. `DateTime.UtcNow` or `DateTime.Now.ToUniversalTime()`.
  - Previously a local time was silently read as if it were UTC when the expression had no `tz` rule, giving occurrences shifted by the machine's offset, and threw when it had one.
  - `DateTimeKind.Unspecified` is still treated as UTC.
- **Stricter validation:** the `…InTz` methods (`TryGetNextOccurrenceInTz()`, `GetNextOccurrenceInTz()`, `TryGetNextOccurrencesInTz()`, `GetNextOccurrencesInTz()`) now throw `ArgumentException` when the `Kind` of the base time or of the max lookahead does not match the zone passed: a `DateTimeKind.Utc` time requires a UTC zone (e.g. `UTC`, `Etc/UTC`) and a `DateTimeKind.Local` time the zone of the machine. Pass a `DateTimeKind.Unspecified` time to give a wall-clock time in the zone, e.g. `DateTime.SpecifyKind(value, DateTimeKind.Unspecified)`.
  - Previously, without a `tz` rule, the `Kind` was ignored: e.g. `DateTime.UtcNow` with `America/New_York` was read as a New York time, giving occurrences shifted by the offset. With a `tz` rule it threw, even when the zones matched.
  - When the zones match, the result keeps the `Kind` of the base time: `DateTimeKind.Utc` or `DateTimeKind.Local`. A `DateTimeKind.Unspecified` base time gives an unspecified result, as before.
- The local methods (e.g. `GetNextOccurrence()`) return the `Kind` of the base time, also with a `tz` rule: a `DateTimeKind.Utc` base time gives a UTC result and a `DateTimeKind.Unspecified` one an unspecified result. Previously the result was always `DateTimeKind.Local` with a `tz` rule.
- `NaturalCron.CronConverter` 1.0.3 now depends on `NaturalCron` 1.1.1. No other changes.

### Deprecated
- `NaturalCronIanaTimeZoneRule.ConvertFromUtc()` and `NaturalCronIanaTimeZoneRule.ConvertToUtc()` marked `[Obsolete]` — no longer used internally and will be removed in 2.0.

## [1.1.0]
### Added
- `every <value>` shorthand for `every <period> on <value>`: `every 9am` (every day), `every 15th` / `every 1stMonday` / `every LastDay` (every month), `every [monday, friday]` (every week), `every january` (every year) and `every holiday` for a custom keyword (every unit of the keyword, e.g. every day).
- Custom keywords: user-defined rules usable in expressions, e.g. `every day on holiday at 09:00`, `every day on [holiday, payday] at 09:00`.
  - Configured once via `NaturalCronCustomKeywords.Configure(cfg => cfg.AddMatchable(...))` with a match function, an `INaturalCronMatchableCustomKeyword` class (parameterless constructor), or a factory (e.g. to resolve the keyword from IoC).
  - Several keywords can share one implementation, e.g. `cfg.AddMatchable(new[] { "holiday", "holidays" }, ...)`.
  - Matchable keywords work with `on`, `at`, `in` and `except`; they cannot be used as a boundary of `between`, `upto` or `from`.
  - Keywords receive the wall-clock time and the effective `TimeZoneInfo` (the expression's `tz` if present, otherwise the zone of the method called).
  - A bracket list can mix custom keywords and built-in values and matches when any entry matches, e.g. `every day on [holiday, 15th] at 09:00`, `every day at 09:00 except [holiday, friday]`.
- `except` rule: accepts the same values as `on`/`at`/`in` (including custom keywords) and skips matching occurrences while keeping the schedule cadence, e.g. `every day at 09:00 except [saturday, sunday]`, `every hour except [12:00, 13:00]`, `every day at 09:00 except holiday`.
- `NaturalCronCustomKeywordRule`, `NaturalCronAnyOfMatchableRule`, `NaturalCronExceptRule`, `NaturalCronRuleSpec.Custom` and `NaturalCronRuleSpec.Except`.

### Changed
- The local occurrence methods (e.g. `GetNextOccurrence()`) return a `DateTime` with the same `Kind` as the base time when the expression has no timezone rule. Previously the result was always `DateTimeKind.Unspecified`.
- `NaturalCron.CronConverter` 1.0.2 now depends on `NaturalCron` 1.1.0. It treats custom keyword, mixed-list and `except` rules as not supported: `Convert` throws `CronConversionException` (default), `TryConvert` returns the error, and `Omit` mode drops the rule.

### Fixed
- Occurrences were skipped when `every` used a smaller unit than another rule and the base time did not satisfy that rule. The next occurrence was a whole unit of that rule after the base time instead of the beginning of the next matching period. `every` now restarts at the beginning of that period, keeping the units below the `every` unit:
  - `every second at 9am` from 08:59:57 returned 09:59:57; it now returns 09:00:00.
  - `every hour on wednesday` from Tuesday 10:30 returned Wednesday 10:30; it now returns Wednesday 00:30.
  - `every day in january` from December 23rd 10:30 now returns January 1st at 10:30.
- `every 15th` (a number with an ordinal suffix) was parsed as `every 15 days`. It now means the 15th of every month.
- `every monday 9am` ignored the time and was parsed as `every monday`. It now means every Monday at 9am.
- `every <weekday>` combined with other rules could return a day that is not that weekday. `every monday` is now exactly `every week on monday`:
  - `every monday at 08:00` from a Sunday at 09:00 returned Tuesday 08:00; it now returns Monday 08:00.
  - `every monday at 08:00` from a Monday at 09:00 returned Tuesday 08:00; it now returns the next Monday at 08:00.
  - `every monday at 10:00 in [jan, feb, mar]` returned Thursday, January 1st when the months started again; it now returns the first Monday of January.
  - `Rules` now has an `every week` rule and an `on monday` rule for it, instead of one `every week` rule with `AnchoredValue = "MONDAY"`.

## [1.0.2]
### Fixed
- UTC occurrence methods (e.g. `GetNextOccurrenceInUtc()`) now always return `DateTime` values with `Kind = DateTimeKind.Utc`. Previously, expressions without a timezone rule returned `DateTimeKind.Unspecified`. Thanks to [@bensynapse](https://github.com/bensynapse) ([#8](https://github.com/hugoj0s3/NaturalCron/pull/8)).

### Changed
- `NaturalCron.CronConverter` 1.0.1 now depends on `NaturalCron` 1.0.2.

## [1.0.1]
### Added
- `ToNaturalExpression()` on `INaturalCronBuildSelector` — returns the natural language expression string (e.g. `"every day at 02:00"`). Replaces the misleadingly named `ToRawExpression()`.

### Deprecated
- `ToRawExpression()` marked `[Obsolete]` — the name implied a classic cron string to many users. Use `ToNaturalExpression()` instead.

## 1.0.0
### Changed
- Stable release. No breaking changes from 0.2.2.

## [0.2.2]
### Added
- New IANA timezone methods for working with specific timezones:
  - `TryGetNextOccurrenceInTz()` - Get next occurrence in a specified IANA timezone (returns null if not found)
  - `GetNextOccurrenceInTz()` - Get next occurrence in a specified IANA timezone (throws if not found)
  - `TryGetNextOccurrencesInTz()` - Get multiple occurrences in a specified IANA timezone (returns available occurrences)
  - `GetNextOccurrencesInTz()` - Get multiple occurrences in a specified IANA timezone (throws if not enough found)
- Case-insensitive IANA timezone ID validation (e.g., "America/New_York" or "america/new_york")
- Comprehensive unit tests for IANA timezone methods
- Updated API reference documentation with IANA timezone methods and usage examples

## [0.2.1] - 2025-09-10
- Optimize performance of GetNextOccurrence for Multiples times/dates rules e.g At [10:00, 11:00, 12:00]
- Optimize the parser code
- Allow week + time and any other combination e.g At [Monday 10:00, Tuesday 11:00, Wednesday 12pm]
- Allow simpler time format e.g 2pm instead of 2:00pm
- Bug fix for multiples times/dates rules e.g At [1st 3:00am, 3rd 10:00pm, 6th 11:00pm] the parser was not work properly.

## [0.2.0] - 2025-07-27
- Include builder feature.
- Improve the documentation

## [0.1.0]
Rename project from RecurlyEx to NaturalCron

## [0.0.3] - 2025-07-15
### Added
- Make @ optional we support both "@every day @at 9:00am" and "every day at 9:00am"
- GetOccurrences in the current thread's timezone

## [0.0.2] - 2025-07-14
### Fixed
- Fixed bug where using @between with a smaller time unit than @every (e.g., @every week @on [friday, tuesday] @between 1:00pm and 3:00pm) would incorrectly match the base time when it already falls within the @between range. (See test case 33.81)
- Allowed combining @every day with @on week rules (e.g., @every day @on [monday, wednesday]).

## [0.0.1] - 2025-07-13
### Added
- Custom cache implementation (`RecurlyExCache`)
- Automatic cache cleanup
- Removed dependency on Microsoft.Extensions.Caching.Memory

### Fixed
- NuGet README and metadata issues

## [0.0.0] - 2025-07-01
- Initial release
