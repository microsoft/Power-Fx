// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// Specifies the kind of an intellisense suggestion.
    /// </summary>
    public enum SuggestionKind
    {
        /// <summary>
        /// Suggestion for a function.
        /// </summary>
        Function,

        /// <summary>
        /// Suggestion for a keyword.
        /// </summary>
        KeyWord,

        /// <summary>
        /// Suggestion for a global symbol.
        /// </summary>
        Global,

        /// <summary>
        /// Suggestion for a field.
        /// </summary>
        Field,

        /// <summary>
        /// Suggestion for an alias.
        /// </summary>
        Alias,

        /// <summary>
        /// Suggestion for an enum value.
        /// </summary>
        Enum,

        /// <summary>
        /// Suggestion for a binary operator.
        /// </summary>
        BinaryOperator,

        /// <summary>
        /// Suggestion for a local symbol.
        /// </summary>
        Local,

        /// <summary>
        /// Suggestion for a service function option.
        /// </summary>
        ServiceFunctionOption,

        /// <summary>
        /// Suggestion for a service.
        /// </summary>
        Service,

        /// <summary>
        /// Suggestion for a scope variable.
        /// </summary>
        ScopeVariable,

        /// <summary>
        /// Suggestion for a type.
        /// </summary>
        Type,
    }
}
