// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IQualifiedValuesInfo : IExternalEntity
    {
        bool IsAsyncAccess { get; }
    }
}
