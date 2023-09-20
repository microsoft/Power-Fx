// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorGlobalContext
    {
        /// <summary>
        /// List of functions in the same swagger file. Used for resolving dynamic schema/property.
        /// </summary>
        internal IReadOnlyList<ConnectorFunction> FunctionList { get; }

        /// <summary>
        /// Dictionary of connector global values like 'connectionId' or predetermined parameters.
        /// </summary>
        internal IReadOnlyDictionary<string, FormulaValue> ConnectorValues { get; }

        internal ConnectorGlobalContext(IReadOnlyList<ConnectorFunction> functionList, IReadOnlyDictionary<string, FormulaValue> connectorValues)
        {
            FunctionList = functionList;
            ConnectorValues = connectorValues;
        }
    }
}
