// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PowerFx.Core.Utils.FormulaSort
{
    internal static class TopologicalSort
    {
        public static bool TrySort<T>(
            IEnumerable<T> nodes,
            IEnumerable<TopologicalSortEdge<T>> edges,
            out IEnumerable<T> result,
            out IEnumerable<T> cycles)
        {
            // Setup
            var links = new Dictionary<T, List<T>>();
            var counts = new Dictionary<T, int>();
            foreach (var node in nodes)
            {
                counts.Add(node, 0);
                links.Add(node, new List<T>());
            }

            foreach (var edge in edges)
            {
                if (!links.ContainsKey(edge.ProcessFirst) || !counts.ContainsKey(edge.ProcessSecond))
                {
                    result = null;
                    cycles = null;
                    return false;
                }

                links[edge.ProcessFirst].Add(edge.ProcessSecond);
                counts[edge.ProcessSecond] = counts[edge.ProcessSecond] + 1;
            }

            // Main Algorithm
            var workingResult = new List<T>();

            var noIncomingEdges = counts.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key).ToList();

            while (counts.Count != 0 && noIncomingEdges.Count != 0)
            {
                foreach (var item in noIncomingEdges)
                {
                    workingResult.Add(item);
                    counts.Remove(item);

                    var itemLinks = links[item];
                    foreach (var link in itemLinks)
                    {
                        counts[link] = counts[link] - 1;
                    }
                }

                noIncomingEdges = counts.Where(kvp => kvp.Value <= 0).Select(kvp => kvp.Key).ToList();
            }

            // Output
            if (counts.Count == 0)
            {
                result = workingResult;
                cycles = null;
                return true;
            }
            else
            {
                result = null;
                cycles = counts.Keys.ToList();
                return false;
            }
        }
    }
}
