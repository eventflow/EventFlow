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

using ClearFlow.FluentValidation.Commands;
using EventFlow;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClearFlow.FluentValidation.Extensions;
public static class EventFlowOptionsFluentValidationExtensions
{
    public static IServiceCollection UseEventFlowFluentValidation(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddTransient<IValidatedCommandBus, ValidatedCommandBus>();

        return serviceCollection;
    }

    public static IEventFlowOptions UseEventFlowFluentValidation(this IEventFlowOptions eventFlowOptions)
    {
        eventFlowOptions.ServiceCollection.UseEventFlowFluentValidation();

        return eventFlowOptions;
    }
    public static IServiceCollection UseEventFlowFluentValidation<FromAssemblyType>(this IServiceCollection serviceCollection)
    {
        serviceCollection.UseEventFlowFluentValidation();
        serviceCollection.AddEventFlowCommandValidators<FromAssemblyType>();

        return serviceCollection;
    }

    public static IEventFlowOptions UseEventFlowFluentValidation<FromAssemblyType>(this IEventFlowOptions eventFlowOptions)
    {
        eventFlowOptions.UseEventFlowFluentValidation();
        eventFlowOptions.ServiceCollection.AddEventFlowCommandValidators<FromAssemblyType>();

        return eventFlowOptions;
    }

    public static IEventFlowOptions AddEventFlowCommandValidators<FromAssemblyType>(this IEventFlowOptions eventFlowOptions, ServiceLifetime validationLifetime = ServiceLifetime.Scoped)
    {
        var assemblyType = typeof(FromAssemblyType);

        return eventFlowOptions.AddEventFlowCommandValidators(assemblyType, validationLifetime);
    }

    public static IEventFlowOptions AddEventFlowCommandValidators(this IEventFlowOptions eventFlowOptions, Type assemblyType, ServiceLifetime validationLifetime = ServiceLifetime.Scoped)
    {
        eventFlowOptions.ServiceCollection.AddEventFlowCommandValidators(assemblyType, validationLifetime);

        return eventFlowOptions;
    }

    public static IServiceCollection AddEventFlowCommandValidators<FromAssemblyType>(this IServiceCollection serviceCollection, ServiceLifetime validationLifetime = ServiceLifetime.Scoped)
    {
        var assemblyType = typeof(FromAssemblyType);

        return serviceCollection.AddEventFlowCommandValidators(assemblyType, validationLifetime);
    }

    public static IServiceCollection AddEventFlowCommandValidators(this IServiceCollection serviceCollection, Type assemblyType, ServiceLifetime validationLifetime = ServiceLifetime.Scoped)
    {
        serviceCollection.AddValidatorsFromAssemblyContaining(assemblyType, validationLifetime);

        return serviceCollection;
    }
}