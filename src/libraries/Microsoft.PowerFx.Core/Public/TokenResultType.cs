// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// Token result type (this matches formula bar token type defined in PowerAppsTheme.ts).
    /// </summary>
    public enum TokenResultType
    {
        /// <summary>
        /// Represents a Boolean value that can be either <see langword="true"/> or <see langword="false"/>.
        /// </summary>
        /// <remarks>The <see cref="Boolean"/> type is commonly used to represent binary states, such as
        /// on/off, yes/no, or enabled/disabled.</remarks>
        Boolean,

        /// <summary>
        /// Represents a keyword used for categorization, tagging, or search functionality.
        /// </summary>
        /// <remarks>This class can be used to define and manage keywords in various contexts, such as
        /// metadata tagging or search indexing. Keywords are typically short, descriptive terms that help identify or
        /// group related items.</remarks>
        Keyword,

        /// <summary>
        /// Represents a reusable function or operation.
        /// </summary>
        /// <remarks>This is a placeholder for a function or operation.  Additional details about its
        /// purpose and usage should be provided.</remarks>
        Function,

        /// <summary>
        /// Represents a numeric value and provides methods for basic arithmetic operations.
        /// </summary>
        /// <remarks>This class is designed to encapsulate a numeric value and offer functionality for
        /// performing arithmetic operations, comparisons, and other numeric manipulations. It can be used in scenarios
        /// where a strongly-typed numeric abstraction is required.</remarks>
        Number,

        /// <summary>
        /// Represents an operator that performs a specific operation or calculation.
        /// </summary>
        /// <remarks>This class is intended to encapsulate the behavior and properties of an operator. Use
        /// this type to define and execute operations in a structured manner.</remarks>
        Operator,

        /// <summary>
        /// Represents a host symbol, which serves as a reference to a symbol in the host environment.
        /// </summary>
        /// <remarks>This class is typically used to encapsulate metadata or functionality related to
        /// symbols in a host-specific context, such as external systems or environments.</remarks>
        HostSymbol,

        /// <summary>
        /// Represents a variable whose purpose or type is not explicitly defined in the provided context.
        /// </summary>
        /// <remarks>Additional details about the variable's purpose, type, or usage should be provided to
        /// clarify its role in the code.</remarks>
        Variable
    }
}
