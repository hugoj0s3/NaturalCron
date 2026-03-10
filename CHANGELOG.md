# Changelog
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
