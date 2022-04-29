// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// The kind of a suggestion.
    /// </summary>
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
