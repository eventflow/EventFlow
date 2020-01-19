# Breaking changes in EventFlow

**THIS IS A DRAFT**

This document describes any breaking changes in EventFlow, starting
from version 0.x to 1.x.

## 1.0

* **No .NET Framework support**
  * **Reason:** With the announcement of .NET 5, Microsoft stated that
    .NET Framework is basically legacy, or will be in the very near future.
    This, and the fact that maintaining boot .NET (Core) and .NET Framework
    will clutter the codebase significantly, going forward EventFlow will
    no longer support .NET Framework.

* **Custom IoC container deleted and replace with
  `Microsoft.Extensions.DependencyInjection`**
  * **Reason:** The custom IoC container was only ever to be used as
    an easy starting point for basic tests and should never be used
    in a production environment. At the time of the creation of EventFlow,
    there was no standard dependency injection, while there were many
    good candidates, using a custom interface seem the least intrusive
    for any developer using EventFlow. Going forward EventFlow will not
    have any custom IoC container, but focus on its core concepts and
    rely on the standard defined by Microsoft 

* **Remove all non-async methods**
  * **Reason:** ...


