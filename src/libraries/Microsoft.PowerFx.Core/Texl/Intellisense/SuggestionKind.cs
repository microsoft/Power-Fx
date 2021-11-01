// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    // The kind of a suggestion.
    public enum SuggestionKind
    {
        Function,
        KeyWord,
        Global,
        Field,
        Alias,
        Enum,
        BinaryOperator,
        Local,
        ServiceFunctionOption,
        Service,
        ScopeVariable,
    }
}
