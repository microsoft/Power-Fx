// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Base class for all dynamic classes.
    /// </summary>
    internal class ConnectionDynamicApi
    {
        /// <summary>
        /// Normalized operation id.
        /// Defines the connector function to be called.
        /// </summary>
        public string OperationId;

        /// <summary>
        /// Connector function to be called.
        /// </summary>
        public ConnectorFunction ConnectorFunction;

        /// <summary>
        /// Mapping table for parameters, between parameters of current function and the dynamic one being called.
        /// </summary>
        public Dictionary<string, IConnectorExtensionValue> ParameterMap;        
    }

    /// <summary>
    /// Static value used in ConnectionDynamicApi.
    /// </summary>
    [DebuggerDisplay("Static: {Value}")]
    internal class StaticConnectorExtensionValue : IConnectorExtensionValue
    {
        public FormulaValue Value;
    }

    /// <summary>
    /// Dynamic value (reference) used in ConnectionDynamicApi.
    /// </summary>
    [DebuggerDisplay("Dynamic: {Reference}")]
    internal class DynamicConnectorExtensionValue : IConnectorExtensionValue
    {
        public string Reference;
    }

    /// <summary>
    /// Either StaticConnectorExtensionValue or DynamicConnectorExtensionValue used in ConnectionDynamicApi.
    /// </summary>
    internal interface IConnectorExtensionValue
    {
    }
}
