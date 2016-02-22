/*
The MIT License (MIT)

Copyright (c) 2014 Maksim Volkau

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

namespace DryIoc
{
    using System;
    using System.Threading;

    /// <summary>Stores scopes propagating through async-await boundaries.</summary>
    public sealed class AsyncExecutionFlowScopeContext : IScopeContext, IDisposable
    {
        /// <summary>Statically known name of root scope in this context.</summary>
        public static readonly string ScopeContextName = typeof(AsyncExecutionFlowScopeContext).FullName;

#if COREFX
        private static readonly AsyncLocal<IScope> _ambientScope = new AsyncLocal<IScope>();
#else
        [Serializable]
        internal sealed class ScopeEntry<T> : MarshalByRefObject
        {
            public readonly T Value;
            public ScopeEntry(T value) { Value = value; }
        }

        private static int _seedKey;
        private readonly string _scopeEntryKey = ScopeContextName + Interlocked.Increment(ref _seedKey);
#endif

        /// <summary>Name associated with context root scope - so the reuse may find scope context.</summary>
        public string RootScopeName { get { return ScopeContextName; } }

        /// <summary>Returns current scope or null if no ambient scope available at the moment.</summary>
        /// <returns>Current scope or null.</returns>
        public IScope GetCurrentOrDefault()
        {
#if COREFX
            return _ambientScope.Value;
#else
            var scopeEntry = (ScopeEntry<IScope>)System.Runtime.Remoting.Messaging.CallContext.LogicalGetData(_scopeEntryKey);
            return scopeEntry == null ? null : scopeEntry.Value;
#endif
        }

        /// <summary>Changes current scope using provided delegate. Delegate receives current scope as input and  should return new current scope.</summary>
        /// <param name="changeCurrentScope">Delegate to change the scope.</param>
        /// <remarks>Important: <paramref name="changeCurrentScope"/> may be called multiple times in concurrent environment.
        /// Make it predictable by removing any side effects.</remarks>
        /// <returns>New current scope. It is convenient to use method in "using (var newScope = ctx.SetCurrent(...))".</returns>
        public IScope SetCurrent(SetCurrentScopeHandler changeCurrentScope)
        {
            var oldScope = GetCurrentOrDefault();
            var newScope = changeCurrentScope(oldScope);
#if COREFX
            _ambientScope.Value = newScope;
#else
            var scopeEntry = newScope == null ? null : new ScopeEntry<IScope>(newScope);
            System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(_scopeEntryKey, scopeEntry);
#endif
            return newScope;
        }

        /// <summary>Nothing to dispose.</summary>
        public void Dispose() { }
    }
}
