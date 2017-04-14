.. _specifications:

Specifications
==============

EventFlow ships with an implementation of the 
`specification pattern <https://en.wikipedia.org/wiki/Specification_pattern>`_
which could be used to e.g. make complex business rules easier to read and test.

To use the specification implementation shipped with EventFlow, simply create a
class that inherits from ``Specification<T>``.

.. code-block:: c#
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


Note that instead of simply returning a ``bool`` to indicate whether or not the
specification is satisfied, this implementation requires a reason (or reasons)
why **not** the specification is satisfied.

The ``ISpecification<T>`` interface has two methods defined, the traditional
``IsSatisfiedBy`` and the addition ``WhyIsNotSatisfiedBy``, which returns an
empty enumerable if the specification was indeed satisfied.

.. code-block:: c#
    public interface ISpecification<in T>
    {
        bool IsSatisfiedBy(T obj);

        IEnumerable<string> WhyIsNotSatisfiedBy(T obj);
    }


As specifications really become powerful when they are combined, EventFlow also
ships with a series of extension methods for the ``ISpecification<T>`` interface
that allows easy combination of implemented specifications.

.. code-block:: c#
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
