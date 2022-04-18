// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Utils.FormulaSort;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class TopologicalSortTests : PowerFxTest
    {
        [Fact]
        public void BasicOrderTest()
        {
            var nodes = new List<string>() { "f", "a", "d", "c", "b", "e" };
            var edges = new List<TopologicalSortEdge<string>>()
            {
                new TopologicalSortEdge<string>("d", "f"),
                new TopologicalSortEdge<string>("b", "d"),
                new TopologicalSortEdge<string>("a", "b"),
                new TopologicalSortEdge<string>("c", "e"),
                new TopologicalSortEdge<string>("e", "f"),
                new TopologicalSortEdge<string>("a", "c")
            };

            var success = TopologicalSort.TrySort(nodes, edges, out var result, out var cycles);
            Assert.True(success);
            Assert.NotNull(result);
            Assert.Null(cycles);

            var indexMap = new Dictionary<string, int>();
            var index = 0;

            foreach (var item in result)
            {
                indexMap[item] = index;
                index += 1;
            }

            Assert.True(indexMap["a"] < indexMap["b"]);
            Assert.True(indexMap["a"] < indexMap["c"]);
            Assert.True(indexMap["b"] < indexMap["d"]);
            Assert.True(indexMap["c"] < indexMap["e"]);
            Assert.True(indexMap["d"] < indexMap["f"]);
            Assert.True(indexMap["e"] < indexMap["f"]);
        }

        [Fact]
        public void CycleTest()
        {
            var nodes = new List<string>() { "f", "a", "d", "c", "b", "e" };
            var edges = new List<TopologicalSortEdge<string>>()
            {
                new TopologicalSortEdge<string>("d", "f"),
                new TopologicalSortEdge<string>("b", "d"),
                new TopologicalSortEdge<string>("a", "b"),
                new TopologicalSortEdge<string>("c", "e"),
                new TopologicalSortEdge<string>("e", "f"),
                new TopologicalSortEdge<string>("a", "c"),
                new TopologicalSortEdge<string>("f", "d")
            };

            var success = TopologicalSort.TrySort(nodes, edges, out var result, out var cycles);
            Assert.False(success);
            Assert.Null(result);
            Assert.NotNull(cycles);

            var set = new HashSet<string>(cycles);
            Assert.True(set.Count == 2);
            Assert.Contains("f", set);
            Assert.Contains("d", set);
        }

        [Fact]
        public void EmptyTest()
        {
            var nodes = new List<string>();
            var edges = new List<TopologicalSortEdge<string>>();

            var success = TopologicalSort.TrySort(nodes, edges, out var result, out var cycles);
            Assert.True(success);
            Assert.NotNull(result);
            Assert.Null(cycles);
            Assert.True(result.Count() == 0);
        }

        [Fact]
        public void MissingNodeTest1()
        {
            var nodes = new List<string>() { "c", "a", "b" };
            var edges = new List<TopologicalSortEdge<string>>()
            {
                new TopologicalSortEdge<string>("e", "a")
            };

            var success = TopologicalSort.TrySort(nodes, edges, out var result, out var cycles);
            Assert.False(success);
            Assert.Null(result);
            Assert.Null(cycles);
        }

        [Fact]
        public void MissingNodeTest2()
        {
            var nodes = new List<string>() { "c", "a", "b" };
            var edges = new List<TopologicalSortEdge<string>>()
            {
                new TopologicalSortEdge<string>("a", "e")
            };

            var success = TopologicalSort.TrySort(nodes, edges, out var result, out var cycles);
            Assert.False(success);
            Assert.Null(result);
            Assert.Null(cycles);
        }
    }
}
