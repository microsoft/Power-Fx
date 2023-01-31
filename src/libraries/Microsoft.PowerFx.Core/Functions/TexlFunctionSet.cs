// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class TexlFunctionSet
    {
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

        internal IEnumerable<TexlFunction> WithName(string name) => WithNameInternal(name);

        private List<TexlFunction> WithNameInternal(string name) => _functions.TryGetValue(name, out List<TexlFunction> result) ? result : new List<TexlFunction>();                

        internal IEnumerable<TexlFunction> WithName(string name, DPath ns) => _functions.TryGetValue(name, out List<TexlFunction> result) ? result.Where(f => f.Namespace == ns) : new List<TexlFunction>();

        internal IEnumerable<TexlFunction> WithInvariantName(string name) => WithInvariantNameInternal(name);

        private List<TexlFunction> WithInvariantNameInternal(string name) => _functionsInvariant.TryGetValue(name, out List<TexlFunction> result) ? result : new List<TexlFunction>();

        internal IEnumerable<TexlFunction> WithInvariantName(string name, DPath ns) => _functionsInvariant.TryGetValue(name, out List<TexlFunction> result) ? result.Where(f => f.Namespace == ns) : new List<TexlFunction>();

        internal IEnumerable<TexlFunction> WithNamespace(DPath ns) => WithNamespaceInternal(ns);

        private List<TexlFunction> WithNamespaceInternal(DPath ns) => _namespaces.TryGetValue(ns, out List<TexlFunction> result) ? result : new List<TexlFunction>();

        internal IEnumerable<string> Enums => _enums;

        internal TexlFunctionSet()
        {
            _functions = new Dictionary<string, List<TexlFunction>>();
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

        internal TexlFunctionSet(IEnumerable<TexlFunctionSet> functionSets)
            : this()
        {
            if (functionSets == null)
            {
                throw new ArgumentNullException(nameof(functionSets));
            }

            foreach (var functionSet in functionSets)
            {
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
                foreach (var kvp in _functions)
                {
                    foreach (var func in kvp.Value)
                    {
                        yield return func;
                    }
                }
            }
        }

        internal TexlFunction Append(TexlFunction function) => Add(function);

        internal TexlFunction Add(TexlFunction function)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            var fList = WithNameInternal(function.Name);

            if (fList.Any())
            {
                if (fList.Contains(function))
                {
                    throw new ArgumentException($"Function {function.Name} is already part of core or extra functions");
                }

                fList.Add(function);
            }
            else
            {
                _functions.Add(function.Name, new List<TexlFunction>() { function });
            }

            var fInvariantList = WithInvariantNameInternal(function.LocaleInvariantName);

            if (fInvariantList.Any())
            {
                if (fInvariantList.Contains(function))
                {
                    throw new ArgumentException($"Function {function.Name} is already part of core or extra functions (invariant)");
                }

                fInvariantList.Add(function);
            }
            else
            {
                _functionsInvariant.Add(function.LocaleInvariantName, new List<TexlFunction>() { function });
            }

            var fnsList = WithNamespaceInternal(function.Namespace);

            if (fnsList.Any())
            {
                if (fnsList.Contains(function))
                {
                    throw new ArgumentException($"Function {function.Name} is already part of core or extra functions (namespace)");
                }

                fnsList.Add(function);
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
            if (functionSet == null)
            {
                throw new ArgumentNullException(nameof(functionSet));
            }

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
                    var fList = WithNameInternal(key);
                    var newFuncs = functionSet.WithNameInternal(key);

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
                    var fInvariantList = WithInvariantNameInternal(key);
                    var newFuncs = functionSet.WithInvariantNameInternal(key);

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
                    var fnsList = WithNamespaceInternal(key);
                    var newFuncs = functionSet.WithNamespaceInternal(key);

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
            foreach (var t in functions)
            {
                Add(t);
            }
        }

        internal int Count()
        {
            return _count;
        }

        internal bool Any()
        {
            return _count != 0;
        }

        internal bool AnyWithName(string name) => _functions.ContainsKey(name);

        internal void RemoveAll(string name)
        {
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
