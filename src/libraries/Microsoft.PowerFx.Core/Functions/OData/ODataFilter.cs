// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Core.Functions.OData
{
    internal class ODataFilter : ODataCommand
    {
        public override ODataCommandType CommandType => ODataCommandType.Filter;

        public ODataFilter(IntermediateNode lambdaTree)
        { 
        }
    }
}
