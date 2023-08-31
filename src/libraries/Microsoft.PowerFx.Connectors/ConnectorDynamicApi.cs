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
        public Dictionary<string, IValue> ParameterMap;

        //public ServiceFunction ServiceFunction;

        public ConnectorFunction ConnectorFunction;
    }

    [DebuggerDisplay("Static: {Value}")]
    internal class StaticValue : IValue
    {
        public FormulaValue Value;
    }

    [DebuggerDisplay("Dynamic: {Reference}")]
    internal class DynamicValue : IValue
    {
        public string Reference;
    }

    internal interface IValue
    {
    }
}
