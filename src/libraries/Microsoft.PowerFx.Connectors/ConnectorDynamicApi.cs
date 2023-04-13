// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.AppMagic.Authoring.Texl.Builtins;

namespace Microsoft.AppMagic.Authoring.Texl.Builtins
{
    internal class ConnectionDynamicApi
    {
        public string OperationId;

        // param name to be called, param name of current function
        public Dictionary<string, string> ParameterMap;

        public ServiceFunction ServiceFunction;
    }
}
