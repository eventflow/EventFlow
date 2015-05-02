### New in 0.4

* Braking: `ValueObject` now uses public properties instead of both
  private and public fields
* Feature: Release notes added to NuGet packages
* Fixed: Unchecked missing in `ValueObject` when claculating hash
* Fixed: `NullReferenceException` thrown if `null` was stored
  in `SingleValueObject` and `ToString()` was called

### New in 0.3 (released 2015-04-30)

* First stable version of EventFlow
