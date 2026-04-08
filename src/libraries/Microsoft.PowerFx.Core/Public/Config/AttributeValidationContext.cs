// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Provides context about the definition an attribute is attached to,
    /// for use by custom attribute validation callbacks.
    /// </summary>
    [ThreadSafeImmutable]
    internal class AttributeValidationContext
    {
        /// <summary>
        /// Gets the name of the UDF this attribute is on.
        /// </summary>
        public string DefinitionName { get; }

        /// <summary>
        /// Gets the string arguments from the attribute syntax.
        /// </summary>
        public IReadOnlyList<string> AttributeArguments { get; }

        /// <summary>
        /// Gets the return type of the UDF.
        /// </summary>
        public FormulaType ReturnType { get; }

        /// <summary>
        /// Gets the parameters of the UDF in order.
        /// </summary>
        public IReadOnlyList<NamedFormulaType> Parameters { get; }

        internal AttributeValidationContext(
            string definitionName,
            IReadOnlyList<string> attributeArguments,
            FormulaType returnType,
            IReadOnlyList<NamedFormulaType> parameters)
        {
            DefinitionName = definitionName;
            AttributeArguments = attributeArguments;
            ReturnType = returnType;
            Parameters = parameters;
        }
    }
}
