# Changelog

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
- `NaturalCron.CronConverter` treats custom keyword, mixed-list and `except` rules as not supported: `Convert` throws `CronConversionException` (default), `TryConvert` returns the error, and `Omit` mode drops the rule.

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
