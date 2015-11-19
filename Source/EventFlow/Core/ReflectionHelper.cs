// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
// https://github.com/rasmus/EventFlow
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
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EventFlow.Extensions;

namespace EventFlow.Core
{
    public static class ReflectionHelper
    {
        public static string GetCodeBase(Assembly assembly, bool includeFileName = false)
        {
            var codebase = assembly.GetName().CodeBase;
            var uri = new UriBuilder(codebase);
            var path = Path.GetFullPath(Uri.UnescapeDataString(uri.Path));
            var codeBase = includeFileName ?
                path :
                Path.GetDirectoryName(path);
            return codeBase;
        }

        public static TResult CompileMethodInvocation<TResult>(Type type, string methodName, Type[] methodSignature = null)
        {
            var methodInfo = methodSignature == null
                ? type.GetMethods(BindingFlags.Instance | BindingFlags.Public).SingleOrDefault(m => m.Name == methodName)
                : type.GetMethod(methodName, methodSignature);

            if (methodInfo == null)
            {
                throw new ArgumentException($"Type '{type.PrettyPrint()}' doesn't have a method called '{methodName}'");
            }

            var genericArguments = typeof (TResult).GetGenericArguments();
            var methodArgumentList = methodInfo.GetParameters().Select(p => p.ParameterType).ToList();
            var funcArgumentList = genericArguments.Skip(1).Take(methodArgumentList.Count).ToList();

            if (funcArgumentList.Count != methodArgumentList.Count)
            {
                throw new ArgumentException("Incorrect number of arguments");
            }

            var varCnt = 1;

            var instanceArgument = Expression.Parameter(genericArguments[0], $"arg{varCnt}"); varCnt++;
            var lambdaArgument = new List<ParameterExpression>
                {
                    instanceArgument,
                };
            var instanceVariable = Expression.Variable(type, $"var{varCnt}"); varCnt++;
            var blockVariables = new List<ParameterExpression>
                {
                        instanceVariable,
                };
            var blockExpressions = new List<Expression>
                {
                    Expression.Assign(instanceVariable, Expression.ConvertChecked(instanceArgument, type))
                };
            var callArguments = new List<ParameterExpression>();
            foreach (var a in funcArgumentList.Zip(methodArgumentList, (s, d) => new {Source = s, Destination = d}))
            {
                var sourceParameter = Expression.Parameter(a.Source, $"arg{varCnt}"); varCnt++;
                var destinationVariable = Expression.Variable(a.Destination, $"var{varCnt}"); varCnt++;
                var assignToDestination = Expression.Assign(destinationVariable, Expression.ConvertChecked(sourceParameter, a.Destination));

                lambdaArgument.Add(sourceParameter);
                callArguments.Add(destinationVariable);
                blockVariables.Add(destinationVariable);
                blockExpressions.Add(assignToDestination);
            }

            var callExpression = Expression.Call(instanceVariable, methodInfo, callArguments);
            blockExpressions.Add(callExpression);

            var block = Expression.Block(blockVariables, blockExpressions);

            var lambdaExpression = Expression.Lambda<TResult>(block, lambdaArgument);

            return lambdaExpression.Compile();
        }
    }
}