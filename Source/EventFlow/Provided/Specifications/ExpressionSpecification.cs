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
using System.Linq.Expressions;
using EventFlow.Extensions;
using EventFlow.Specifications;

namespace EventFlow.Provided.Specifications
{
    public class ExpressionSpecification<T> : Specification<T>
    {
        private readonly Func<T, bool> _predicate;
        private readonly Lazy<string> _string;

        public ExpressionSpecification(
            Expression<Func<T, bool>> expression)
        {
            _predicate = expression.Compile();
            _string = new Lazy<string>(() => MakeString(expression));
        }

        public override string ToString()
        {
            return _string.Value;
        }

        protected override IEnumerable<string> IsNotSatisfiedBecause(T obj)
        {
            if (!_predicate(obj))
            {
                yield return $"'{_string.Value}' is not satisfied";
            }
        }

        private static string MakeString(Expression<Func<T, bool>> expression)
        {
            try
            {
                var paramName = expression.Parameters[0].Name;
                var expBody = expression.Body.ToString();

                expBody = expBody
                    .Replace("AndAlso", "&&")
                    .Replace("OrElse", "||");

                return $"{paramName} => {expBody}";
            }
            catch
            {
                return typeof(ExpressionSpecification<T>).PrettyPrint();
            }
        }
    }
}