// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Defines a known attribute that can be applied to UDFs and NamedFormulas.
    /// </summary>
    [ThreadSafeImmutable]
    public class AttributeDefinition
    {
        /// <summary>
        /// Gets the name of the attribute.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the minimum number of arguments expected.
        /// </summary>
        public int MinArgCount { get; }

        /// <summary>
        /// Gets the maximum number of arguments expected.
        /// </summary>
        public int MaxArgCount { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeDefinition"/> class.
        /// </summary>
        /// <param name="name">The attribute name.</param>
        /// <param name="minArgCount">Minimum number of string arguments.</param>
        /// <param name="maxArgCount">Maximum number of string arguments.</param>
        public AttributeDefinition(string name, int minArgCount = 0, int maxArgCount = 0)
        {
            Contracts.AssertNonEmpty(name);
            Contracts.Assert(minArgCount >= 0);
            Contracts.Assert(maxArgCount >= minArgCount);

            Name = name;
            MinArgCount = minArgCount;
            MaxArgCount = maxArgCount;
        }
    }
}
