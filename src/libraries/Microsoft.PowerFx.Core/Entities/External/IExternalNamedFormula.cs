// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    internal interface IExternalNamedFormula : IExternalPageableSymbol, IExternalDelegable
    {
        bool TryGetExternalDataSource(out IExternalDataSource dataSource);
    }
}
