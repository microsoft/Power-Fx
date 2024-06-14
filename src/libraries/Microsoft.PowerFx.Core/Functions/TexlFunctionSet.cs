// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.PowerFx.Core.Annotations;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions
{
    [DebuggerDisplay("Composed {_composed} - Funcs {Count()}")]
    internal class TexlFunctionSet
    {
        private delegate bool FuncOut(TexlFunctionSet tfs, out List<TexlFunction> result);

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
        private int _count;

        internal bool _canremove;

        internal IEnumerable<string> FunctionNames => Sets.SelectMany(tfs => tfs._functions.Keys).Distinct();

        internal IEnumerable<string> InvariantFunctionNames => Sets.SelectMany(tfs => tfs._functionsInvariant.Keys).Distinct();

        internal IEnumerable<DPath> Namespaces => Sets.SelectMany(tfs => tfs._namespaces.Keys).Distinct();

        private IEnumerable<TexlFunctionSet> Sets => (!_composed ? new[] { this } : new[] { this }.Union(_texlFunctionSets)).Where(t => t._count > 0);

        internal IEnumerable<TexlFunction> WithName(string name) => WithNameInternal(name);

        private IEnumerable<TexlFunction> WithNameInternal(string name) => Get(Sets, (TexlFunctionSet tfs, out List<TexlFunction> result) => tfs._functions.TryGetValue(name, out result));

        internal IEnumerable<TexlFunction> WithName(string name, DPath ns) => WithNameInternal(name).Where(f => f.Namespace == ns);

        internal IEnumerable<TexlFunction> WithInvariantName(string name) => WithInvariantNameInternal(name);

        private IEnumerable<TexlFunction> WithInvariantNameInternal(string name) => Get(Sets, (TexlFunctionSet tfs, out List<TexlFunction> result) => tfs._functionsInvariant.TryGetValue(name, out result));

        internal IEnumerable<TexlFunction> WithInvariantName(string name, DPath ns) => WithInvariantNameInternal(name).Where(f => f.Namespace == ns);

        internal IEnumerable<TexlFunction> WithNamespace(DPath ns) => WithNamespaceInternal(ns);

        private IEnumerable<TexlFunction> WithNamespaceInternal(DPath ns) => Get(Sets, (TexlFunctionSet tfs, out List<TexlFunction> result) => tfs._namespaces.TryGetValue(ns, out result));

        internal IEnumerable<string> Enums => _enums;

        internal bool _composed = false;

        internal List<TexlFunctionSet> _texlFunctionSets = null;

        private static IEnumerable<TexlFunction> Get(IEnumerable<TexlFunctionSet> sets, FuncOut filter)
        {
            foreach (TexlFunctionSet set in sets)
            {                
                if (filter(set, out List<TexlFunction> fs))
                {
                    foreach (TexlFunction func in fs)
                    {
                        yield return func;
                    }
                }
            }
        }

        // Return an empty TexlFucntion set that can never be added to.
        internal static TexlFunctionSet Empty()
        {
            var set = new TexlFunctionSet(canRemove: false);
            set._guard.ForbidWriters();            
            return set;
        }

        internal TexlFunctionSet(bool canRemove = true)
        {
            _functions = new Dictionary<string, List<TexlFunction>>(StringComparer.Ordinal);
            _functionsInvariant = new Dictionary<string, List<TexlFunction>>(StringComparer.OrdinalIgnoreCase);
            _namespaces = new Dictionary<DPath, List<TexlFunction>>();
            _enums = new List<string>();
            _count = 0;
            _canremove = canRemove;
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
            _canremove = true;
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

            List<TexlFunctionSet> tfsList = functionSets.SelectMany(fs => fs.Sets).Where(fs => fs._count > 0).ToList();

            // keep local (empty) set for additional functions that could be added later
            if (tfsList.Any())
            {
                _composed = true;
                _texlFunctionSets = tfsList;
            }
        }

        // Slow API, only use for backward compatibility
        [Obsolete("Slow API. Prefer using With* functions for identifying functions you need.")]
        internal IEnumerable<TexlFunction> Functions
        {
            get
            {
                foreach (var set in Sets)
                {
                    foreach (var kvp in set._functions)
                    {
                        foreach (var func in kvp.Value)
                        {
                            yield return func;
                        }
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

            IEnumerable<TexlFunction> fList = WithNameInternal(function.Name);
            IEnumerable<TexlFunction> fInvariantList = WithInvariantNameInternal(function.LocaleInvariantName);
            IEnumerable<TexlFunction> fNsList = WithNamespaceInternal(function.Namespace);

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

            if (_functions.TryGetValue(function.Name, out List<TexlFunction> listN))
            {
                listN.Add(function);
            }
            else
            {
                _functions.Add(function.Name, new List<TexlFunction>() { function });
            }

            if (_functionsInvariant.TryGetValue(function.LocaleInvariantName, out List<TexlFunction> listIN))
            {
                listIN.Add(function);
            }
            else
            {
                _functionsInvariant.Add(function.LocaleInvariantName, new List<TexlFunction>() { function });
            }

            if (_namespaces.TryGetValue(function.Namespace, out List<TexlFunction> listNS))
            {
                listNS.Add(function);
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

            if (functionSet.Count() == 0)
            {
                return this;
            }

            if (_composed)
            {
                _texlFunctionSets.Add(functionSet);
            }
            else
            {
                _composed = true;
                _texlFunctionSets = new List<TexlFunctionSet>() { functionSet };
            }

            if (functionSet._composed)
            {
                foreach (TexlFunctionSet inner in functionSet._texlFunctionSets)
                {
                    _texlFunctionSets.Add(inner);
                }
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

        internal TexlFunctionSet Clone()
        {
            TexlFunctionSet tfs = new TexlFunctionSet(canRemove: true)
            {
                _functions = new Dictionary<string, List<TexlFunction>>(_functions, StringComparer.Ordinal),
                _functionsInvariant = new Dictionary<string, List<TexlFunction>>(_functionsInvariant, StringComparer.OrdinalIgnoreCase),
                _namespaces = new Dictionary<DPath, List<TexlFunction>>(_namespaces),
                _enums = new List<string>(_enums),
                _count = _count,
                _composed = _composed                
            };

            if (_composed)
            {
                tfs._texlFunctionSets = _texlFunctionSets.Select(tfs => tfs.Clone()).ToList();
            }

            return tfs;
        }

        internal int Count() => Sets.Sum(t => t._count);

        internal bool Any() => Sets.Any();

        internal bool AnyWithName(string name) => Sets.Any(set => set._functions.ContainsKey(name));

        internal void RemoveAll(string name)
        {
            using var guard = _guard.Enter(); // Region is single threaded.            

            foreach (var set in Sets)
            {
                if (!set._canremove)
                {
                    throw new InvalidOperationException("Cannot remove functions from this function set.");
                }

                if (set._functions.TryGetValue(name, out List<TexlFunction> removed))
                {
                    set._count -= removed.Count();
                    set._functions.Remove(name);
                }

                if (set._functionsInvariant.TryGetValue(name, out _))
                {
                    set._functionsInvariant.Remove(name);
                }

                if (removed != null && removed.Any())
                {
                    foreach (TexlFunction f in removed)
                    {
                        set._namespaces.TryGetValue(f.Namespace, out List<TexlFunction> fnsList);

                        if (fnsList.Count == 1)
                        {
                            set._namespaces.Remove(f.Namespace);
                        }
                        else
                        {
                            fnsList.Remove(f);
                        }

                        f.GetRequiredEnumNames().ToList().ForEach(removedEnum => set._enums.Remove(removedEnum));
                    }
                }
            }

            // If functions with the given name aren't found, this is ok.
        }

        internal void RemoveAll(TexlFunction function)
        {
            using var guard = _guard.Enter(); // Region is single threaded.            

            foreach (var set in Sets)
            {
                if (!set._canremove)
                {
                    throw new InvalidOperationException("Cannot remove functions from this function set.");
                }

                if (set._functions.TryGetValue(function.Name, out List<TexlFunction> funcs))
                {
                    set._count--;
                    funcs.Remove(function);

                    if (!funcs.Any())
                    {
                        set._functions.Remove(function.Name);
                    }
                }

                if (set._functionsInvariant.TryGetValue(function.Name, out List<TexlFunction> funcs2))
                {
                    funcs2.Remove(function);

                    if (!funcs2.Any())
                    {
                        set._functionsInvariant.Remove(function.Name);
                    }
                }

                if (set._namespaces.TryGetValue(function.Namespace, out List<TexlFunction> funcs3))
                {
                    funcs3.Remove(function);

                    if (!funcs3.Any())
                    {
                        set._namespaces.Remove(function.Namespace);
                    }
                }

                IEnumerable<string> enums = function.GetRequiredEnumNames();

                if (enums.Any())
                {
                    foreach (string enumName in enums)
                    {
                        set._enums.Remove(enumName);
                    }
                }
            }
        }
    }
}
