// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using static Microsoft.PowerFx.Intellisense.ArgumentSuggestions;

namespace Microsoft.PowerFx.Core.Functions
{
    // Used for Intellisense in connectors
    internal interface ISupportsArgumentSuggestions
    {
        IEnumerable<KeyValuePair<string, DType>> GetArgumentSuggestions(TryGetEnumSymbol tryGetEnumSymbol, bool suggestUnqualifiedEnums, DType scopeType, int argumentIndex, out bool requiresSuggestionEscaping);
    }
}
