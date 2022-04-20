// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public
{
    /// <summary>
    /// A set of name/formula paris, which creates and updates a topologically-sorted list
    /// of name/formula pairs.
    /// </summary>
    public class FormulaSet
    {
        private readonly IDependencyFinder _dependencyFinder;

        private readonly Dictionary<string, FormulaWithParameters> _formulas = new Dictionary<string, FormulaWithParameters>();

        /// <summary>
        /// A topologically-sorted list of name/formula pairs. This property is regenerated every time
        /// the formula set is modified.
        /// </summary>
        public IEnumerable<KeyValuePair<string, FormulaWithParameters>> SortedFormulas { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaSet"/> class.
        /// </summary>
        public FormulaSet(IDependencyFinder dependencyFinder)
        {
            _dependencyFinder = dependencyFinder;
            SortFormulas();
        }

        private void SortFormulas()
        {
            var nodes = _formulas.Keys.ToList();
            var edges = new List<TopologicalSortEdge<string>>();

            foreach (var kvp in _formulas)
            {
                var dependencies = _dependencyFinder.FindDependencies(kvp.Value);
                foreach (var processFirst in dependencies)
                {
                    if (_formulas.ContainsKey(processFirst))
                    {
                        edges.Add(new TopologicalSortEdge<string>(processFirst, kvp.Key));
                    }
                }
            }

            if (TopologicalSort.TrySort(nodes, edges, out var result, out var cycles))
            {
                SortedFormulas = result.Select(x => new KeyValuePair<string, FormulaWithParameters>(x, _formulas[x]));
            }
            else
            {
                throw new InvalidOperationException($"Circular dependencies detected: {string.Join(", ", cycles)}");
            }
        }

        /// <summary>
        /// Add an item and regenerate the sorted list. Throws InvalidOperationException for circular dependencies.
        /// </summary>
        public void Add(string name, FormulaWithParameters formula)
        {
            _formulas.Add(name, formula);
            SortFormulas();
        }

        /// <summary>
        /// Add a range of items and regenerate the sorted list. Throws InvalidOperationException for circular dependencies.
        /// </summary>
        public void Add(IEnumerable<KeyValuePair<string, FormulaWithParameters>> items)
        {
            foreach (var item in items)
            {
                _formulas.Add(item.Key, item.Value);
            }

            SortFormulas();
        }

        /// <summary>
        /// Remove an item and regenerate the sorted list.
        /// </summary>
        public void Remove(string name)
        {
            _formulas.Remove(name);
            SortFormulas();
        }

        /// <summary>
        /// Remove a range of items and regenerate the sorted list.
        /// </summary>
        public void Remove(IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                _formulas.Remove(name);
            }

            SortFormulas();
        }
    }
}
