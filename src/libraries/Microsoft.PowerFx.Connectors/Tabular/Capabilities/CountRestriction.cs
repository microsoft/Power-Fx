// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Connectors
{
    internal class CountRestriction
    {
        /// <summary>
        /// Indicates whether table supports $count=true in OData queries.
        /// </summary>
        public readonly bool IsCountable;

        public CountRestriction(bool isCountable)
        {
            IsCountable = isCountable;
        }
    }
}
