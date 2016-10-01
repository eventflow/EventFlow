# Identity

The `Identity<>` value object provides generic functionality to create and
validate the IDs of e.g. aggregate roots. Its basically a wrapper around a `Guid`.

Lets say we want to create a new identity named `TestId` we could do it like
this.

```csharp
public class TestId : Identity<TestId>
{
  public TestId(string value)
   : base(value)
  {
  }
}
```

- The identity follow the form  `{class without "Id"}-{guid}` e.g.
  `test-c93fdb8c-5c9a-4134-bbcd-87c0644ca34f` for the above `TestId` example
- The internal `Guid` can be generated using one of the following
  methods/properties. Note that you can access the `Guid` factories directly by
  accessing the static methods on the `GuidFactories` class
 - `New`: Uses the standard `Guid.NewGuid()`
 - `NewDeterministic(...)`: Creates a name-based `Guid` using the algorithm from
   [RFC 4122](https://www.ietf.org/rfc/rfc4122.txt) §4.3, which allows
   identities to be generated based on known data, e.g. an user e-mail, i.e.,
   it always returns the same identity for the same arguments
 - `NewComb()`: Creates a sequential `Guid` that can be used to e.g. avoid
   database fragmentation
- A `string` can be tested to see if its a valid identity using the static
  `bool IsValid(string)` method
- Any validation errors can be gathered using the static
  `IEnumerable<string> Validate(string)` method

**Note:** Its very important to name the constructor argument `value` as its
significant if you serialize the identity type.

Here's some examples on we can use our newly created `TestId`

```csharp
// Uses the default Guid.NewGuid()
var testId = TestId.New
```

```csharp
// Create a namespace, put this in a constant somewhere
var emailNamespace = Guid.Parse("769077C6-F84D-46E3-AD2E-828A576AAAF3");

// Creates an identity with the value "test-9181a444-af25-567e-a866-c263b6f6119a"
var testId = TestId.NewDeterministic(emailNamespace, "test@example.com");
```

```csharp
// Creates a new identity every time, but an identity when used in e.g.
// database indexes, minimizes fragmentation
var testId = TestId.NewComb()
```

**Note:** Be sure to read the section about [value objects](./ValueObjects.md)
as the `Identity<>` is basically a value object.
