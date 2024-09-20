// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    public class DelegationHost
    {
        internal IExternalTabularDataSource TabularDataSource;

        public virtual void SetType(RecordType type)
        {
            throw new NotImplementedException();
        }
    }
}
