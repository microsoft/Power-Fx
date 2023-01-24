// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class TexlFunctionSet<T>
        where T : ITexlFunction
    {
        private Dictionary<string, List<T>> _functions;
        private Dictionary<string, List<T>> _functionsInvariant;
        private Dictionary<DPath, List<T>> _namespaces;
        private List<string> _enums;
        private int _count;

        internal TexlFunctionSet()
        {
            _functions = new Dictionary<string, List<T>>();
            _functionsInvariant = new Dictionary<string, List<T>>();
            _namespaces = new Dictionary<DPath, List<T>>();
            _enums = new List<string>();
            _count = 0;
        }

        internal TexlFunctionSet(T function)
            : this()
        {
            if (function == null)
            {
                throw new ArgumentNullException($"{nameof(function)} cannot be null", nameof(function));
            }

            _functions.Add(function.Name, new List<T>() { function });
            _functionsInvariant.Add(function.LocaleInvariantName, new List<T>() { function });
            _namespaces.Add(function.Namespace, new List<T> { function });
            _enums = function.GetRequiredEnumNames().ToList();
            _count = 1;
        }

        internal TexlFunctionSet(IEnumerable<T> functions)
            : this()
        {
            if (functions == null)
            {
                throw new ArgumentNullException($"{nameof(functions)} cannot be null", nameof(functions));
            }

            foreach (var func in functions)
            {
                Add(func);
            }
        }

        private TexlFunctionSet(Dictionary<string, List<T>> functions, Dictionary<string, List<T>> functionsInvariant, Dictionary<DPath, List<T>> functionNamespaces, List<string> enums, int count)
        {
            _functions = functions ?? throw new ArgumentNullException($"{nameof(functions)} cannot be null", nameof(functions));
            _functionsInvariant = functionsInvariant ?? throw new ArgumentNullException($"{nameof(functionsInvariant)} cannot be null", nameof(functionsInvariant));
            _namespaces = functionNamespaces ?? throw new ArgumentNullException($"{nameof(functionNamespaces)} cannot be null", nameof(functionNamespaces));
            _enums = enums ?? throw new ArgumentNullException($"{nameof(enums)} cannot be null", nameof(enums));
            _count = count;
        }

        internal TexlFunctionSet(TexlFunctionSet<T> other)
        {
            _functions = new Dictionary<string, List<T>>(other._functions);
            _functionsInvariant = new Dictionary<string, List<T>>(other._functionsInvariant);
            _namespaces = new Dictionary<DPath, List<T>>(other._namespaces);
            _enums = new List<string>(other._enums);
            _count = other._count;
        }

        internal TexlFunctionSet(IEnumerable<TexlFunctionSet<T>> functionSets)
            : this()
        {
            if (functionSets == null)
            {
                throw new ArgumentNullException($"{nameof(functionSets)} cannot be null", nameof(functionSets));
            }

            foreach (var functionSet in functionSets)
            {
                Add(functionSet);
            }
        }

        // Slow API, only use for backward compatibility
        [Obsolete("Slow API. Prefer using With* functions for idenfitying functions you need.")]
        internal IEnumerable<T> Functions
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

        internal T Add(T function)
        {
            if (function == null)
            {
                throw new ArgumentNullException($"{nameof(function)} cannot be null", nameof(function));
            }

            var fList = WithName(function.Name);

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
                _functions.Add(function.Name, new List<T>() { function });
            }

            var fInvariantList = WithInvariantName(function.LocaleInvariantName);

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
                _functionsInvariant.Add(function.LocaleInvariantName, new List<T>() { function });
            }

            var fnsList = WithNamespace(function.Namespace);

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
                _namespaces.Add(function.Namespace, new List<T>() { function });
            }

            _enums.AddRange(function.GetRequiredEnumNames());

            _count++;

            return function;
        }

        internal T Append(T function) => Add(function);

        internal TexlFunctionSet<T> Add(TexlFunctionSet<T> functionSet)
        {
            if (functionSet == null)
            {
                throw new ArgumentNullException($"{nameof(functionSet)} cannot be null", nameof(functionSet));
            }

            if (_count == 0)
            {
                _functions = new Dictionary<string, List<T>>(functionSet._functions);
                _functionsInvariant = new Dictionary<string, List<T>>(functionSet._functionsInvariant);
                _namespaces = new Dictionary<DPath, List<T>>(functionSet._namespaces);
                _enums = new List<string>(functionSet._enums);
                _count = functionSet._count;
            }
            else
            {
                foreach (var key in functionSet.Keys)
                {
                    var fList = WithName(key);
                    var newFuncs = functionSet.WithName(key);

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

                foreach (var key in functionSet.InvariantKeys)
                {
                    var fInvariantList = WithInvariantName(key);
                    var newFuncs = functionSet.WithInvariantName(key);

                    if (fInvariantList.Any())
                    {                        
                        fInvariantList.AddRange(newFuncs);
                    }
                    else
                    {
                        _functionsInvariant.Add(key, newFuncs);
                    }
                }

                foreach (var key in functionSet.NamespaceKeys)
                {
                    var fnsList = WithNamespace(key);
                    var newFuncs = functionSet.WithNamespace(key);

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

        internal void Add(IEnumerable<T> functions)
        {
            foreach (var t in functions)
            {
                Add(t);
            }
        }

        internal Dictionary<string, List<T>>.KeyCollection Keys => _functions.Keys;

        internal Dictionary<string, List<T>>.KeyCollection InvariantKeys => _functionsInvariant.Keys;

        internal Dictionary<DPath, List<T>>.KeyCollection NamespaceKeys => _namespaces.Keys;

        internal List<T> WithName(string name) => _functions.ContainsKey(name) ? _functions[name] : new List<T>();

        internal List<T> WithName(string name, DPath ns) => _functions.ContainsKey(name) ? new List<T>(_functions[name].Where(f => f.Namespace == ns)) : new List<T>();

        internal List<T> WithInvariantName(string name) => _functionsInvariant.ContainsKey(name) ? _functionsInvariant[name] : new List<T>();

        internal List<T> WithInvariantName(string name, DPath ns) => _functionsInvariant.ContainsKey(name) ? new List<T>(_functionsInvariant[name].Where(f => f.Namespace == ns)) : new List<T>();

        internal List<T> WithNamespace(DPath ns) => _namespaces.ContainsKey(ns) ? _namespaces[ns] : new List<T>();

        internal List<string> Enums => _enums;

        internal TexlFunctionSet<TexlFunction> ToTexlFunctions()
        {
            if (typeof(T) == typeof(TexlFunction))
            {
                return this as TexlFunctionSet<TexlFunction>;
            }

            var functions = new Dictionary<string, List<TexlFunction>>();

            foreach (KeyValuePair<string, List<T>> t in _functions)
            {
                functions.Add(t.Key, new List<TexlFunction>(t.Value.Select(f => f.ToTexlFunctions())));
            }

            var invariantFunctions = new Dictionary<string, List<TexlFunction>>();

            foreach (KeyValuePair<string, List<T>> t in _functionsInvariant)
            {
                invariantFunctions.Add(t.Key, new List<TexlFunction>(t.Value.Select(f => f.ToTexlFunctions())));
            }

            var namespaces = new Dictionary<DPath, List<TexlFunction>>();

            foreach (KeyValuePair<DPath, List<T>> t in _namespaces)
            {
                namespaces.Add(t.Key, new List<TexlFunction>(t.Value.Select(f => f.ToTexlFunctions())));
            }
            
            return new TexlFunctionSet<TexlFunction>(functions, invariantFunctions, namespaces, _enums, _count);
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
            List<T> removed = null;

            if (_functions.ContainsKey(name))
            {
                removed = WithName(name);
                _count -= removed.Count();
                _functions.Remove(name);
            }

            if (_functionsInvariant.ContainsKey(name))
            {
                _functionsInvariant.Remove(name);
            }

            if (removed != null && removed.Any())
            {
                foreach (T f in removed)
                {
                    List<T> fnsList = WithNamespace(f.Namespace);

                    if (fnsList.Count == 1)
                    {
                        _namespaces.Remove(f.Namespace);
                    }
                    else
                    {
                        fnsList.Remove(f);
                        _namespaces[f.Namespace] = fnsList;
                    }

                    f.GetRequiredEnumNames().ToList().ForEach(removedEnum => _enums.Remove(removedEnum));                    
                }
            }

            // If functions with the given name aren't found, this is ok.
        }

        internal void RemoveAll(T function)
        {
            RemoveAll(function.Name);
        }

        internal TexlFunctionSet<T> Clone()
        {
            return new TexlFunctionSet<T>(this);
        }
    }
}
