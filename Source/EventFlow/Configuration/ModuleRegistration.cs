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
using EventFlow.Extensions;

namespace EventFlow.Configuration
{
    public class ModuleRegistration : IModuleRegistration
    {
        private readonly Dictionary<Type, IModule> _modules = new Dictionary<Type, IModule>();
        private readonly IEventFlowOptions _eventFlowOptions;

        public ModuleRegistration(
            IEventFlowOptions eventFlowOptions)
        {
            _eventFlowOptions = eventFlowOptions;
        }

        public void Register<TModule>()
            where TModule : IModule, new()
        {
            var module = new TModule();
            Register(module);
        }

        public void Register<TModule>(TModule module)
            where TModule : IModule
        {
            var moduleType = typeof(TModule);
            if (_modules.ContainsKey(moduleType))
            {
                throw new ArgumentException($"Module '{moduleType.PrettyPrint()}' has already been registered");
            }

            module.Register(_eventFlowOptions);
            _modules.Add(moduleType, module);
        }

        public TModule GetModule<TModule>()
            where TModule : IModule
        {
            TModule module;
            if (!TryGetModule(out module))
            {
                throw new ArgumentException($"Module '{typeof(TModule).PrettyPrint()}' is not registered");
            }

            return module;
        }

        public bool TryGetModule<TModule>(out TModule module)
            where TModule : IModule
        {
            var moduleType = typeof(TModule);
            IModule iModule;
            if (!_modules.TryGetValue(moduleType, out iModule))
            {
                module = default(TModule);
                return false;
            }

            module = (TModule)iModule;
            return true;
        }
    }
}