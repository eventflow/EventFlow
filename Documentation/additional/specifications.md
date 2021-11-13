---
title: Specifications
---

# Specifications

EventFlow ships with an implementation of the 
[specification pattern](https://en.wikipedia.org/wiki/Specification_pattern)
which could be used to e.g. make complex business rules easier to read and test.

To use the specification implementation shipped with EventFlow, simply create a
class that inherits from `Specification<T>`.

```csharp
public class BelowFiveSpecification : Specification<int>
{
  protected override IEnumerable<string> IsNotSatisfiedBecause(int i)
  {
    if (5 <= i)
    {
        yield return string.Format("{0} is not below five", i);
    }
  }
}
```

Note that instead of simply returning a `bool` to indicate whether or not the
specification is satisfied, this implementation requires a reason (or reasons)
why the specification is **not** satisfied.

The `ISpecification<T>` interface has two methods defined, the traditional
`IsSatisfiedBy` as well as `WhyIsNotSatisfiedBy`, which returns an
empty enumerable if the specification was indeed satisfied.

```csharp
public interface ISpecification<in T>
{
  bool IsSatisfiedBy(T obj);

  IEnumerable<string> WhyIsNotSatisfiedBy(T obj);
}
```

Specifications really become powerful when they are combined. EventFlow also
ships with a series of extension methods for the `ISpecification<T>` interface
that allows easy combination of implemented specifications.

```csharp
// Throws a `DomainError` exception if obj doesn't satisfy the specification
spec.ThrowDomainErrorIfNotStatisfied(obj);

// Builds a new specification that requires all input specifications to be
// satified
var allSpec = specEnumerable.All();

// Builds a new specification that requires a predefined amount of the
// input specifications to be satisfied
var atLeastSpec = specEnumerable.AtLeast(4);

// Builds a new specification that requires the two input specifications
// to be satisfied
var andSpec = spec1.And(spec2);

// Builds a new specification that requires one of the two input
// specifications to be satisfied
var orSpec = spec1.Or(spec2);

// Builds a new specification that requires the input specification
// not to be satisfied
var notSpec = spec.Not();
```

If you need a simple expression to combine with other more complex specifications
you can use the bundled `ExpressionSpecification<T>`, which is a specification
wrapper for an expression.

```csharp
var spec = new ExpressionSpecification<int>(i => 1 < i && i < 3);

// 'str' will contain the value "i => ((1 < i) && (i < 3))"
var str = spec.ToString();
```

If the specification isn't satisfied, a string representation of the expression
is returned.
