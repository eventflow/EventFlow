// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EventFlow.Extensions;
using JetBrains.Annotations;

namespace EventFlow.Core
{
    public static class ReflectionHelper
    {
        public static string GetCodeBase(Assembly assembly, bool includeFileName = false)
        {
            var codeBase = includeFileName ?
                assembly.Location :
                Path.GetDirectoryName(assembly.Location);
            return codeBase;
        }

        public static Func<T> CompileConstructor<T>()
        {
            var expr = Expression.New(typeof(T));
            return Expression.Lambda<Func<T>>(expr).Compile();
        }

        public static Func<TInterfaceParameter, TResult> CompileConstructor<TInterfaceParameter, TResult>(Type typeOfTInterfaceParameterImpl, Type typeOfTResult)
        {
            var constructor = typeOfTResult.GetConstructor(new[] { typeOfTInterfaceParameterImpl });
            if (constructor == null)
            {
                throw new ArgumentException($"Type {typeOfTResult.PrettyPrint()} doesn't have constructor of {typeOfTResult.Name}({typeOfTInterfaceParameterImpl.Name} typeOfTInterfaceParameterImpl)");
            }
            var parameter = Expression.Parameter(typeof(TInterfaceParameter));

            var body = Expression.New(constructor, Expression.Convert(parameter, typeOfTInterfaceParameterImpl));
            var lambda = Expression.Lambda<Func<TInterfaceParameter, TResult>>(body, parameter);
            return lambda.Compile();
        }


        public static Func<T1, TResult> CompileConstructor<T1, TResult>()
        {
            var typeOfT1 = typeof(T1);
            var typeOfTResult = typeof(TResult);
            var parameter1 = Expression.Parameter(typeOfT1);
            var constructor = typeof(TResult).GetConstructor(new[] { typeOfT1 });
            if (constructor == null)
            {
                throw new ArgumentException($"Type {typeOfTResult.PrettyPrint()} doesn't have constructor of {typeOfTResult.Name}({typeOfT1.Name} arg1)");
            }

            var body = Expression.New(constructor, parameter1);
            var lambda = Expression.Lambda<Func<T1, TResult>>(body, parameter1);
            var method = lambda.Compile();
            return method;
        }

        public static Func<T1, TResult> CompileConstructor<T1, TResult>(Type typeOfTResult)
        {
            var typeOfT1 = typeof(T1);
            var parameter1 = Expression.Parameter(typeOfT1);
            var constructor = typeOfTResult.GetConstructor(new[] { typeOfT1 });
            if (constructor == null)
            {
                throw new ArgumentException($"Type {typeOfTResult.PrettyPrint()} doesn't have constructor of {typeOfTResult.Name}({typeOfT1.Name} arg1)");
            }

            var body = Expression.New(constructor, parameter1);
            var lambda = Expression.Lambda<Func<T1, TResult>>(body, parameter1);
            var method = lambda.Compile();
            return method;
        }

        public static Func<object, TResult> CompileConstructor<TResult>(Type typeOfTResult, Type typeOfT1)
        {
            var constructorArgumentTypes = new[] { typeOfT1 };
            var parameters = constructorArgumentTypes.Select(_ => Expression.Parameter(typeof(object))).ToArray();
            var constructor = typeOfTResult.GetConstructor(constructorArgumentTypes);
            if (constructor == null)
            {
                throw new ArgumentException($"Type {typeOfTResult.PrettyPrint()} doesn't have constructor of {typeOfTResult.Name}({typeOfT1.Name} arg1)");
            }

            var constructorArguments = new Expression[] { Expression.Convert(parameters[0], typeOfT1) };

            var body = Expression.New(constructor, constructorArguments);
            var lambda = Expression.Lambda<Func<object, TResult>>(body, parameters);
            var method = lambda.Compile();

            return method;
        }

        public static Func<object, object, TResult> CompileConstructor<TResult>(Type typeOfTResult, Type typeOfT1, Type typeOfT2)
        {
            var constructorArgumentTypes = new[] { typeOfT1, typeOfT2 };
            var parameters = constructorArgumentTypes.Select(_ => Expression.Parameter(typeof(object))).ToArray();
            var constructor = typeOfTResult.GetConstructor(constructorArgumentTypes);
            if (constructor == null)
            {
                throw new ArgumentException($"Type {typeOfTResult.PrettyPrint()} doesn't have constructor of {typeOfTResult.Name}({typeOfT1.Name} arg1,{typeOfT2.Name} arg2)");
            }

            var constructorArguments = new Expression[] { Expression.Convert(parameters[0], typeOfT1), Expression.Convert(parameters[1], typeOfT2) };

            var body = Expression.New(constructor, constructorArguments);
            var lambda = Expression.Lambda<Func<object, object, TResult>>(body, parameters);
            var method = lambda.Compile();

            return method;
        }

        public static Func<object, object, object, TResult> CompileConstructor<TResult>(Type typeOfTResult, Type typeOfT1, Type typeOfT2, Type typeOfT3)
        {
            var constructorArgumentTypes = new[] { typeOfT1, typeOfT2, typeOfT3 };
            var parameters = constructorArgumentTypes.Select(_ => Expression.Parameter(typeof(object))).ToArray();
            var constructor = typeOfTResult.GetConstructor(constructorArgumentTypes);
            if (constructor == null)
            {
                throw new ArgumentException($"Type {typeOfTResult.PrettyPrint()} doesn't have constructor of {typeOfTResult.Name}({typeOfT1.Name} arg1,{typeOfT2.Name} arg2,{typeOfT3.Name} arg3)");
            }

            var constructorArguments = new Expression[] {
            Expression.Convert(parameters[0], typeOfT1),
            Expression.Convert(parameters[1], typeOfT2) ,
            Expression.Convert(parameters[2], typeOfT3)
        };

            var body = Expression.New(constructor, constructorArguments);
            var lambda = Expression.Lambda<Func<object, object, object, TResult>>(body, parameters);
            var method = lambda.Compile();

            return method;
        }

        public static Func<object, object, object, object, TResult> CompileConstructor<TResult>(Type typeOfTResult, Type typeOfT1, Type typeOfT2, Type typeOfT3, Type typeOfT4)
        {
            var constructorArgumentTypes = new[] { typeOfT1, typeOfT2, typeOfT3, typeOfT4 };
            var parameters = constructorArgumentTypes.Select(_ => Expression.Parameter(typeof(object))).ToArray();
            var constructor = typeOfTResult.GetConstructor(constructorArgumentTypes);
            if (constructor == null)
            {
                throw new ArgumentException($"Type {typeOfTResult.PrettyPrint()} doesn't have constructor of {typeOfTResult.Name}({typeOfT1.Name} arg1,{typeOfT2.Name} arg2,{typeOfT3.Name} arg3,{typeOfT4.Name} arg4)");
            }

            var constructorArguments = new Expression[] {
            Expression.Convert(parameters[0], typeOfT1),
            Expression.Convert(parameters[1], typeOfT2) ,
            Expression.Convert(parameters[2], typeOfT3),
            Expression.Convert(parameters[3], typeOfT4)
        };

            var body = Expression.New(constructor, constructorArguments);
            var lambda = Expression.Lambda<Func<object, object, object, object, TResult>>(body, parameters);
            var method = lambda.Compile();

            return method;
        }

        public static Func<T1, T2, T3, T4, T5, TResult> CompileConstructor<T1, T2, T3, T4, T5, TResult>(
            [CanBeNull] Type typeOfT1Impl = null,
            [CanBeNull] Type typeOfT2Impl = null,
            [CanBeNull] Type typeOfT3Impl = null,
            [CanBeNull] Type typeOfT4Impl = null,
            [CanBeNull] Type typeOfT5Impl = null,
            [CanBeNull] Type typeOfTResultImpl = null
        )
        {
            var inputTypes = new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) };
            var implTypes = new[] { typeOfT1Impl, typeOfT2Impl, typeOfT3Impl, typeOfT4Impl, typeOfT5Impl };
            var constructArgumentTypes = new Type[inputTypes.Length];
            for (var i = 0; i < constructArgumentTypes.Length; i++)
            {
                constructArgumentTypes[i] = implTypes[i] ?? inputTypes[i];
            }

            var typeOfResult = typeOfTResultImpl ?? typeof(TResult);

            var constructor = typeOfResult.GetConstructor(constructArgumentTypes);
            if (constructor == null)
            {
                constructor = typeOfResult.GetConstructors()[0];
            }

            var parameters = inputTypes.Select(Expression.Parameter).ToArray();
            var constructorArguments = new Expression[inputTypes.Length];
            for (var i = 0; i < constructorArguments.Length; i++)
            {
                //constructorArguments[i] =
                //    implTypes[i] == null ? parameters[i] : Expression.Convert(parameters[i], implTypes[i]!);
                if (implTypes[i] == null)
                {
                    constructorArguments[i] = parameters[i];
                }
                else
                {
                    constructorArguments[i] = Expression.Convert(parameters[i], implTypes[i]);
                }
            }

            var body = Expression.New(constructor, constructorArguments);
            var lambda = Expression.Lambda<Func<T1, T2, T3, T4, T5, TResult>>(body, parameters);
            return lambda.Compile();
        }

        /// <summary>
        /// Handles correct upcast. If no upcast was needed, then this could be exchanged to an <c>Expression.Call</c>
        /// and an <c>Expression.Lambda</c>.
        /// </summary>
        public static TResult CompileMethodInvocation<TResult>(Type type, string methodName,
            params Type[] methodSignature)
        {
            var typeInfo = type.GetTypeInfo();
            var methods = typeInfo
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == methodName);

            var methodInfo = methodSignature == null || !methodSignature.Any()
                ? methods.SingleOrDefault()
                : methods.SingleOrDefault(m => m.GetParameters().Select(mp => mp.ParameterType).SequenceEqual(methodSignature));

            if (methodInfo == null)
            {
                throw new ArgumentException($"Type '{type.PrettyPrint()}' doesn't have a method called '{methodName}'");
            }

            return CompileMethodInvocation<TResult>(methodInfo);
        }

        /// <summary>
        /// Handles correct upcast. If no upcast was needed, then this could be exchanged to an <c>Expression.Call</c>
        /// and an <c>Expression.Lambda</c>.
        /// </summary>
        public static TResult CompileMethodInvocation<TResult>(MethodInfo methodInfo)
        {
            var genericArguments = typeof(TResult).GetTypeInfo().GetGenericArguments();
            var methodArgumentList = methodInfo.GetParameters().Select(p => p.ParameterType).ToList();
            var funcArgumentList = genericArguments.Skip(1).Take(methodArgumentList.Count).ToList();

            if (funcArgumentList.Count != methodArgumentList.Count)
            {
                throw new ArgumentException("Incorrect number of arguments");
            }

            var instanceArgument = Expression.Parameter(genericArguments[0]);

            var argumentPairs = funcArgumentList.Zip(methodArgumentList, (s, d) => new { Source = s, Destination = d }).ToList();
            if (argumentPairs.All(a => a.Source == a.Destination))
            {
                // No need to do anything fancy, the types are the same
                var parameters = funcArgumentList.Select(Expression.Parameter).ToList();
                return Expression.Lambda<TResult>(Expression.Call(instanceArgument, methodInfo, parameters), new[] { instanceArgument }.Concat(parameters)).Compile();
            }

            var lambdaArgument = new List<ParameterExpression>
                {
                    instanceArgument,
                };

            var type = methodInfo.DeclaringType;
            var instanceVariable = Expression.Variable(type);
            var blockVariables = new List<ParameterExpression>
                {
                        instanceVariable,
                };
            var blockExpressions = new List<Expression>
                {
                    Expression.Assign(instanceVariable, Expression.ConvertChecked(instanceArgument, type))
                };
            var callArguments = new List<ParameterExpression>();

            foreach (var a in argumentPairs)
            {
                if (a.Source == a.Destination)
                {
                    var sourceParameter = Expression.Parameter(a.Source);
                    lambdaArgument.Add(sourceParameter);
                    callArguments.Add(sourceParameter);
                }
                else
                {
                    var sourceParameter = Expression.Parameter(a.Source);
                    var destinationVariable = Expression.Variable(a.Destination);
                    var assignToDestination = Expression.Assign(destinationVariable, Expression.Convert(sourceParameter, a.Destination));

                    lambdaArgument.Add(sourceParameter);
                    callArguments.Add(destinationVariable);
                    blockVariables.Add(destinationVariable);
                    blockExpressions.Add(assignToDestination);
                }
            }

            var callExpression = Expression.Call(instanceVariable, methodInfo, callArguments);
            blockExpressions.Add(callExpression);

            var block = Expression.Block(blockVariables, blockExpressions);

            var lambdaExpression = Expression.Lambda<TResult>(block, lambdaArgument);

            return lambdaExpression.Compile();
        }
    }
}