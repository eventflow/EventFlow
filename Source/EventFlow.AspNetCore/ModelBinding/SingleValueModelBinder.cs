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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EventFlow.AspNetCore.ModelBinding
{
    internal class SingleValueModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            Type modelType = bindingContext.ModelType;
            ConstructorInfo constructor = modelType.GetConstructors().Single();
            Type parameterType = constructor.GetParameters().Single().ParameterType;

            var modelName = bindingContext.ModelName;

            ValueProviderResult valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            object result;
            try
            {
                if (parameterType == typeof(string))
                {
                    result = constructor.Invoke(new object[] {value});
                }
                else
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(parameterType);
                    if (!converter.CanConvertFrom(typeof(string)))
                    {
                        return Task.CompletedTask;
                    }

                    // ReSharper disable once AssignNullToNotNullAttribute
                    var argument = new[] {converter.ConvertFrom(null, valueProviderResult.Culture, value)};
                    result = constructor.Invoke(argument);
                }
            }
            catch (Exception e)
            {
                if (!(e is FormatException) && e.InnerException != null)
                    e = ExceptionDispatchInfo.Capture(e.InnerException).SourceException;
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, e,
                    bindingContext.ModelMetadata);
                return Task.CompletedTask;
            }

            bindingContext.ModelState.MarkFieldValid(modelName);
            bindingContext.Result = ModelBindingResult.Success(result);

            return Task.CompletedTask;
        }
    }
}