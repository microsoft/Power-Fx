// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Utils
{
    internal class TopologicalSortEdge<T>
    {
        public readonly T ProcessFirst;
        public readonly T ProcessSecond;

        public TopologicalSortEdge(T processFirst, T processSecond)
        {
            ProcessFirst = processFirst;
            ProcessSecond = processSecond;
        }
    }
}
