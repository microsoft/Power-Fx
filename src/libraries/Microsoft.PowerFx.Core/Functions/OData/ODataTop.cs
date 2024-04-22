// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core.Functions.OData
{
    internal class ODataTop : ODataCommand
    {
        public override ODataCommandType CommandType => ODataCommandType.Top;

        internal int Count { get; }

        public ODataTop(int count)
        {
            Count = count;
        }
    }
}
