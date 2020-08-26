// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Exceptions;
using EventFlow.Provided.Specifications;
using EventFlow.Specifications;

namespace EventFlow.Extensions
{
    public static class SpecificationExtensions
    {
        [Obsolete("Use 'ThrowDomainErrorIfNotSatisfied' instead")]
        public static void ThrowDomainErrorIfNotStatisfied<T>(
            this ISpecification<T> specification,
            T obj)
        {
            specification.ThrowDomainErrorIfNotSatisfied(obj);
        }

        public static void ThrowDomainErrorIfNotSatisfied<T>(
            this ISpecification<T> specification,
            T obj)
        {
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            var whyIsNotStatisfiedBy = specification.WhyIsNotSatisfiedBy(obj).ToList();
            if (whyIsNotStatisfiedBy.Any())
            {
                throw DomainError.With(
                    $"'{specification.GetType().PrettyPrint()}' is not satisfied because of {string.Join(" and ", whyIsNotStatisfiedBy)}");
            }
        }

        public static IExecutionResult IsNotSatisfiedByAsExecutionResult<T>(
            this ISpecification<T> specification,
            T obj)
        {
            var whyIsNotStatisfiedBy = specification
                .WhyIsNotSatisfiedBy(obj)
                .ToList();
            
            return whyIsNotStatisfiedBy.Any()
                ? ExecutionResult.Failed(whyIsNotStatisfiedBy)
                : ExecutionResult.Success();
        }

        public static ISpecification<T> All<T>(
            this IEnumerable<ISpecification<T>> specifications)
        {
            return new AllSpecifications<T>(specifications);
        }

        public static ISpecification<T> AtLeast<T>(
            this IEnumerable<ISpecification<T>> specifications,
            int requiredSpecifications)
        {
            return new AtLeastSpecification<T>(requiredSpecifications, specifications);
        }

        public static ISpecification<T> And<T>(
            this ISpecification<T> specification1,
            ISpecification<T> specification2)
        {
            return new AndSpeficication<T>(specification1, specification2);
        }

        public static ISpecification<T> And<T>(
            this ISpecification<T> specification,
            Expression<Func<T, bool>> expression)
        {
            return specification.And(new ExpressionSpecification<T>(expression));
        }

        public static ISpecification<T> Or<T>(
            this ISpecification<T> specification1,
            ISpecification<T> specification2)
        {
            return new OrSpecification<T>(specification1, specification2);
        }
        public static ISpecification<T> Or<T>(
            this ISpecification<T> specification,
            Expression<Func<T, bool>> expression)
        {
            return specification.Or(new ExpressionSpecification<T>(expression));
        }

        public static ISpecification<T> Not<T>(
            this ISpecification<T> specification)
        {
            return new NotSpecification<T>(specification);
        }
    }
}