// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IQualifiedValuesInfo : IExternalEntity
    {
        bool IsAsyncAccess { get; }
    }
}
