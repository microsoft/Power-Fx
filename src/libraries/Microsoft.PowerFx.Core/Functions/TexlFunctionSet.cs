// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class TexlFunctionSet<T>
        where T : ITexlFunction
    {
        private Dictionary<string, List<T>> _functions;
        private Dictionary<string, List<T>> _functionsInvariant;
        private int _count;

        internal TexlFunctionSet()
        {
            _functions = new Dictionary<string, List<T>>();
            _functionsInvariant = new Dictionary<string, List<T>>();
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

        internal TexlFunctionSet(Dictionary<string, List<T>> functions, Dictionary<string, List<T>> functionsInvariant, int count)
        {
            _functions = functions ?? throw new ArgumentNullException($"{nameof(functions)} cannot be null", nameof(functions));
            _functionsInvariant = functionsInvariant ?? throw new ArgumentNullException($"{nameof(functionsInvariant)} cannot be null", nameof(functionsInvariant));
            _count = count;
        }

        internal TexlFunctionSet(TexlFunctionSet<T> other)
        {
            _functions = new Dictionary<string, List<T>>(other._functions);
            _functionsInvariant = new Dictionary<string, List<T>>(other._functionsInvariant);
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
        [Obsolete]
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
                // Need to check duplicate function
                // throw new ArgumentException($"Function {function.Name} is already part of core or extra functions");
                fInvariantList.Add(function);
            }
            else
            {
                _functionsInvariant.Add(function.LocaleInvariantName, new List<T>() { function });
            }

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
                        // Need to check duplicate function
                        // throw new ArgumentException($"Function {function.Name} is already part of core or extra functions");
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
                        // Need to check duplicate function
                        // throw new ArgumentException($"Function {function.Name} is already part of core or extra functions");
                        fInvariantList.AddRange(newFuncs);
                    }
                    else
                    {
                        _functionsInvariant.Add(key, newFuncs);
                    }
                }
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

        internal List<T> WithName(string name) => _functions.ContainsKey(name) ? _functions[name] : new List<T>();
        internal List<T> WithName(string name, DPath ns) => _functions.ContainsKey(name) ? new List<T>(_functions[name].Where(f => f.Namespace == ns)) : new List<T>();
        internal List<T> WithInvariantName(string name) => _functionsInvariant.ContainsKey(name) ? _functionsInvariant[name] : new List<T>();
        internal List<T> WithInvariantName(string name, DPath ns) => _functionsInvariant.ContainsKey(name) ? new List<T>(_functionsInvariant[name].Where(f => f.Namespace == ns)) : new List<T>();

        internal TexlFunctionSet<TexlFunction> ToTexlFunctions()
        {
            if (typeof(T) == typeof(TexlFunction))
            {
                return this as TexlFunctionSet<TexlFunction>;
            }

            var _f = new Dictionary<string, List<TexlFunction>>();

            foreach (var kvp in _functions)
            {
                _f.Add(kvp.Key, new List<TexlFunction>(kvp.Value.Select(f => f.ToTexlFunctions())));
            }

            var _fi = new Dictionary<string, List<TexlFunction>>();

            foreach (var t in _functionsInvariant)
            {
                _fi.Add(t.Key, new List<TexlFunction>(t.Value.Select(f => f.ToTexlFunctions())));
            }

            return new TexlFunctionSet<TexlFunction>(_f, _fi, _count);
        }

        internal int Count()
        {
            return _count;
        }

        internal bool Any()
        {
            return _count != 0;
        }

        internal bool Any(string name) => _functions.ContainsKey(name);

        internal void RemoveAll(string name)
        {
            if (_functions.ContainsKey(name))
            {
                _count -= WithName(name).Count();
                _functions.Remove(name);
            }

            if (_functionsInvariant.ContainsKey(name))
            {
                _functionsInvariant.Remove(name);
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
