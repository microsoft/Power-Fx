// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Defines a known attribute that can be applied to UDFs and NamedFormulas.
    /// </summary>
    [ThreadSafeImmutable]
    internal interface IAttributeDefinition
    {
        /// <summary>
        /// Gets the name of the attribute.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the minimum number of arguments expected.
        /// </summary>
        int MinArgCount { get; }

        /// <summary>
        /// Gets the maximum number of arguments expected.
        /// </summary>
        int MaxArgCount { get; }

        /// <summary>
        /// Validates the attribute usage against the UDF definition.
        /// Returns TexlError instances (empty or null = valid).
        /// </summary>
        IEnumerable<TexlError> Validate(AttributeValidationContext context);
    }
}
