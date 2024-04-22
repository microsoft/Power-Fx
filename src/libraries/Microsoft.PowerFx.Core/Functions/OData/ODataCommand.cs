// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Texl.Builtins;

namespace Microsoft.PowerFx.Core.Functions.OData
{
    public abstract class ODataCommand
    {
        public abstract ODataCommandType CommandType { get; }        
    }
}
