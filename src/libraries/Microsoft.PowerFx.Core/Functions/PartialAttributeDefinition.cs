// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Errors;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Built-in attribute definition for the "Partial" attribute,
    /// which allows splitting a named formula across multiple definitions.
    /// </summary>
    internal sealed class PartialAttributeDefinition : IAttributeDefinition
    {
        public string Name => "Partial";

        public int MinArgCount => 1;

        public int MaxArgCount => 1;

        public IEnumerable<TexlError> Validate(AttributeValidationContext context) => null;
    }
}
