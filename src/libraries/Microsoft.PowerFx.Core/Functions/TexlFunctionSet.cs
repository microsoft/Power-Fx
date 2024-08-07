// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Annotations;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class TexlFunctionSet
    {
        private readonly GuardSingleThreaded _guard = new GuardSingleThreaded();

        // Dictionary key: function.Name
        private Dictionary<string, List<TexlFunction>> _functions;

        // Dictionary key: function.LocaleInvariantName
        private Dictionary<string, List<TexlFunction>> _functionsInvariant;

        // Dictionary key: function.Namespace
        private Dictionary<DPath, List<TexlFunction>> _namespaces;

        // List of all function.GetRequiredEnumNames()
        private List<string> _enums;

        // Count of functions
        internal int _count;

        internal Dictionary<string, List<TexlFunction>>.KeyCollection FunctionNames => _functions.Keys;

        internal Dictionary<string, List<TexlFunction>>.KeyCollection InvariantFunctionNames => _functionsInvariant.Keys;

        internal Dictionary<DPath, List<TexlFunction>>.KeyCollection Namespaces => _namespaces.Keys;

        internal IEnumerable<TexlFunction> WithName(string name) => _guard.VerifyNoWriters(this).WithNameInternal(name);

        private List<TexlFunction> WithNameInternal(string name) => _functions.TryGetValue(name, out List<TexlFunction> result) ? result : new List<TexlFunction>();

        internal IEnumerable<TexlFunction> WithName(string name, DPath ns) => _functions.TryGetValue(name, out List<TexlFunction> result) ? result.Where(f => f.Namespace == ns) : new List<TexlFunction>();

        internal IEnumerable<TexlFunction> WithInvariantName(string name) => _guard.VerifyNoWriters(this).WithInvariantNameInternal(name);

        private List<TexlFunction> WithInvariantNameInternal(string name) => _functionsInvariant.TryGetValue(name, out List<TexlFunction> result) ? result : new List<TexlFunction>();

        internal IEnumerable<TexlFunction> WithInvariantName(string name, DPath ns) => _functionsInvariant.TryGetValue(name, out List<TexlFunction> result) ? result.Where(f => f.Namespace == ns) : new List<TexlFunction>();

        internal IEnumerable<TexlFunction> WithNamespace(DPath ns) => _guard.VerifyNoWriters(this).WithNamespaceInternal(ns);

        private List<TexlFunction> WithNamespaceInternal(DPath ns) => _namespaces.TryGetValue(ns, out List<TexlFunction> result) ? result : new List<TexlFunction>();

        internal IEnumerable<string> Enums => _enums;

        // Return an singleton for an empty TexlFunction set that can never be added to. 
        internal static readonly TexlFunctionSet _empty = MakeEmpty();

        internal static TexlFunctionSet Empty() => _empty;

        // Create an empty function set that can never be added to.
        private static TexlFunctionSet MakeEmpty()
        {
            var set = new TexlFunctionSet();
            set._guard.ForbidWriters();
            return set;
        }

        internal TexlFunctionSet()
        {
            _functions = new Dictionary<string, List<TexlFunction>>(StringComparer.Ordinal);
            _functionsInvariant = new Dictionary<string, List<TexlFunction>>(StringComparer.OrdinalIgnoreCase);
            _namespaces = new Dictionary<DPath, List<TexlFunction>>();
            _enums = new List<string>();
            _count = 0;
        }

        internal TexlFunctionSet(TexlFunction function)
            : this()
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            _functions.Add(function.Name, new List<TexlFunction>() { function });
            _functionsInvariant.Add(function.LocaleInvariantName, new List<TexlFunction>() { function });
            _namespaces.Add(function.Namespace, new List<TexlFunction> { function });
            _enums = new List<string>(function.GetRequiredEnumNames());
            _count = 1;
        }

        internal TexlFunctionSet(IEnumerable<TexlFunction> functions)
            : this()
        {
            if (functions == null)
            {
                throw new ArgumentNullException(nameof(functions));
            }

            foreach (var func in functions)
            {
                Add(func);
            }
        }

        // This will compose multiple function sets into a single one. 
        // The assumption is that all incoming function sets are unchanged for the lifetime of the returned set.
        // Hence it doesn't matter where this shares references or copies the values. 
        internal static TexlFunctionSet Compose(IEnumerable<TexlFunctionSet> functionSets)
        {
            TexlFunctionSet nonEmpty = null;
            int countNonEmpty = 0;

            foreach (var set in functionSets)
            {
                if (set.Count() > 0)
                {
                    nonEmpty = set;
                    countNonEmpty++;
                }
            }

            if (countNonEmpty == 0)
            {
                return Empty();
            }
            else if (countNonEmpty == 1)
            {
                return nonEmpty;
            }

            return new TexlFunctionSet(functionSets);
        }            

        internal TexlFunctionSet(IEnumerable<TexlFunctionSet> functionSets)
            : this()
        {
            if (functionSets == null)
            {
                throw new ArgumentNullException(nameof(functionSets));
            }

            foreach (var functionSet in functionSets)
            {
                functionSet._guard.VerifyNoWriters();

                if (functionSet._count > 0)
                {
                    Add(functionSet);
                }
            }
        }

        // Slow API, only use for backward compatibility
        [Obsolete("Slow API. Prefer using With* functions for identifying functions you need.")]
        internal IEnumerable<TexlFunction> Functions
        {
            get
            {
                foreach (var kvp in _functions.ToList())
                {
                    foreach (var func in kvp.Value)
                    {
                        yield return func;
                    }
                }
            }
        }        

        internal TexlFunction Add(TexlFunction function)
        {
            using var guard = _guard.Enter(); // Region is single threaded.

            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            List<TexlFunction> fList = WithNameInternal(function.Name);
            List<TexlFunction> fInvariantList = WithInvariantNameInternal(function.LocaleInvariantName);
            List<TexlFunction> fNsList = WithNamespaceInternal(function.Namespace);

            bool fListAny = fList.Any();
            bool fInvariantListAny = fInvariantList.Any();
            bool fNsListAny = fNsList.Any();

            if (fListAny && fList.Contains(function))
            {
                throw new ArgumentException($"Function {function.Name} is already part of core or extra functions");
            }

            if (fInvariantListAny && fInvariantList.Contains(function))
            {
                throw new ArgumentException($"Function {function.Name} is already part of core or extra functions (invariant)");
            }

            if (fNsListAny && fNsList.Contains(function))
            {
                throw new ArgumentException($"Function {function.Name} is already part of core or extra functions (namespace)");
            }

            if (fListAny)
            {
                fList.Add(function);
            }
            else
            {
                _functions.Add(function.Name, new List<TexlFunction>() { function });
            }

            if (fInvariantListAny)
            {
                fInvariantList.Add(function);
            }
            else
            {
                _functionsInvariant.Add(function.LocaleInvariantName, new List<TexlFunction>() { function });
            }

            if (fNsListAny)
            {
                fNsList.Add(function);
            }
            else
            {
                _namespaces.Add(function.Namespace, new List<TexlFunction>() { function });
            }

            _enums.AddRange(function.GetRequiredEnumNames());
            _count++;

            return function;
        }

        internal TexlFunctionSet Add(TexlFunctionSet functionSet)
        {
            using var guard = _guard.Enter(); // Region is single threaded.

            if (functionSet == null)
            {
                throw new ArgumentNullException(nameof(functionSet));
            }
            
            functionSet._guard.VerifyNoWriters();

            // Notice that this code path is a critical performance gain versus the various loops in the 'else' part
            if (_count == 0)
            {
                _functions = functionSet._functions.ToDictionary(kvp => kvp.Key, kvp => new List<TexlFunction>(kvp.Value));
                _functionsInvariant = functionSet._functionsInvariant.ToDictionary(kvp => kvp.Key, kvp => new List<TexlFunction>(kvp.Value));
                _namespaces = functionSet._namespaces.ToDictionary(kvp => kvp.Key, kvp => new List<TexlFunction>(kvp.Value));
                _enums = new List<string>(functionSet._enums);
                _count = functionSet._count;
            }
            else
            {
                foreach (var key in functionSet.FunctionNames)
                {
                    var newFuncs = functionSet.WithNameInternal(key);
                    var fList = WithNameInternal(key);

                    if (fList.Any())
                    {
                        fList.AddRange(newFuncs);
                    }
                    else
                    {
                        _functions.Add(key, newFuncs);
                    }

                    _count += newFuncs.Count();
                }

                foreach (var key in functionSet.InvariantFunctionNames)
                {
                    var newFuncs = functionSet.WithInvariantNameInternal(key);
                    var fInvariantList = WithInvariantNameInternal(key);

                    if (fInvariantList.Any())
                    {
                        fInvariantList.AddRange(newFuncs);
                    }
                    else
                    {
                        _functionsInvariant.Add(key, newFuncs);
                    }
                }

                foreach (var key in functionSet.Namespaces)
                {
                    var newFuncs = functionSet.WithNamespaceInternal(key);
                    var fnsList = WithNamespaceInternal(key);

                    if (fnsList.Any())
                    {
                        fnsList.AddRange(newFuncs);
                    }
                    else
                    {
                        _namespaces.Add(key, newFuncs);
                    }
                }

                _enums.AddRange(functionSet._enums);
            }

            return this;
        }

        internal void Add(IEnumerable<TexlFunction> functions)
        {
            if (functions == null)
            {
                throw new ArgumentNullException(nameof(functions));
            }

            foreach (var t in functions)
            {
                Add(t);
            }
        }

        internal int Count()
        {
            _guard.VerifyNoWriters();
            return _count;
        }

        internal bool Any()
        {
            _guard.VerifyNoWriters();
            return _count != 0;
        }

        internal bool AnyWithName(string name) => _guard.VerifyNoWriters(_functions).ContainsKey(name);

        internal void RemoveAll(string name)
        {
            using var guard = _guard.Enter(); // Region is single threaded.

            if (_functions.TryGetValue(name, out List<TexlFunction> removed))
            {
                _count -= removed.Count();
                _functions.Remove(name);
            }

            if (_functionsInvariant.TryGetValue(name, out _))
            {
                _functionsInvariant.Remove(name);
            }

            if (removed != null && removed.Any())
            {
                foreach (TexlFunction f in removed)
                {
                    List<TexlFunction> fnsList = WithNamespaceInternal(f.Namespace);

                    if (fnsList.Count == 1)
                    {
                        _namespaces.Remove(f.Namespace);
                    }
                    else
                    {
                        fnsList.Remove(f);
                    }

                    f.GetRequiredEnumNames().ToList().ForEach(removedEnum => _enums.Remove(removedEnum));
                }
            }

            // If functions with the given name aren't found, this is ok.
        }

        internal void RemoveAll(TexlFunction function)
        {
            using var guard = _guard.Enter(); // Region is single threaded.

            if (_functions.TryGetValue(function.Name, out List<TexlFunction> funcs))
            {
                _count--;
                funcs.Remove(function);

                if (!funcs.Any())
                {
                    _functions.Remove(function.Name);
                }
            }

            if (_functionsInvariant.TryGetValue(function.Name, out List<TexlFunction> funcs2))
            {
                funcs2.Remove(function);

                if (!funcs2.Any())
                {
                    _functionsInvariant.Remove(function.Name);
                }
            }

            if (_namespaces.TryGetValue(function.Namespace, out List<TexlFunction> funcs3))
            {
                funcs3.Remove(function);

                if (!funcs3.Any())
                {
                    _namespaces.Remove(function.Namespace);
                }
            }

            IEnumerable<string> enums = function.GetRequiredEnumNames();

            if (enums.Any())
            {
                foreach (string enumName in enums)
                {
                    _enums.Remove(enumName);
                }
            }
        }
    }
}
