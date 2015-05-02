### New in 0.4

* New: Release notes added to NuGet packages
* New: Better logging and more descriptive exceptions
* Fixed: Unchecked missing in `ValueObject` when claculating hash
* Fixed: `NullReferenceException` thrown if `null` was stored
  in `SingleValueObject` and `ToString()` was called

**Breaking Changes:**
 - `ValueObject` now uses public properties instead of both private
   and public fields

### New in 0.3 (released 2015-04-30)

* First stable version of EventFlow
