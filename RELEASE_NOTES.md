### New in 0.4

* Breaking: `ValueObject` now uses public properties instead of both
  private and public fields
- Braking: Aggregate IDs are no longer `string` but objects implementing
  `IAggregateId`
* New: Release notes added to NuGet packages
* New: Better logging and more descriptive exceptions
* Fixed: Unchecked missing in `ValueObject` when claculating hash
* Fixed: `NullReferenceException` thrown if `null` was stored
  in `SingleValueObject` and `ToString()` was called

### New in 0.3.292 (released 2015-04-30)

* First stable version of EventFlow
