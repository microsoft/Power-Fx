// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Types;

namespace Microsoft.AppMagic.Authoring.Texl.Builtins
{
    internal class ConnectionDynamicApi
    {
        public string OperationId;

        // param name to be called, param name of current function
        public Dictionary<string, IConnectorExtensionValue> ParameterMap;

        //public ServiceFunction ServiceFunction;

        public ConnectorFunction ConnectorFunction;
    }

    [DebuggerDisplay("Static: {Value}")]
    internal class StaticConnectorExtensionValue : IConnectorExtensionValue
    {
        public FormulaValue Value;
    }

    [DebuggerDisplay("Dynamic: {Reference}")]
    internal class DynamicConnectorExtensionValue : IConnectorExtensionValue
    {
        public string Reference;
    }

    internal interface IConnectorExtensionValue
    {
    }
}
