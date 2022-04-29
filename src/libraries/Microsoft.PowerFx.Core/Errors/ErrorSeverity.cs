// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Severity of errors provided.
    /// </summary>
    public enum ErrorSeverity
    {
        // These values are a mirror of Microsoft.PowerFx.Core.Errors.DocumentErrorSeverity, 
        // which is an internal type with [TransportType] attribute.

#pragma warning disable SA1300 // Element should begin with upper-case letter
        _Min = Verbose,
#pragma warning restore SA1300 // Element should begin with upper-case letter

        /// <summary>
        /// A suggestion about possible high-level improvements or refactoring that may help the user
        /// get a better app experience.
        /// Examples: performance changes
        /// Verbose messages will not be part of 'ChoreUpdateRulesWithErrors' as they would be analysed when the dependency changes.
        /// </summary>
        Verbose = 0,

        /// <summary>
        /// A suggestion about possible improvements or refactoring that may help the user
        /// get a better app experience.
        /// Examples: refactoring suggestions.
        /// Suggestions will not be part of 'ChoreUpdateRulesWithErrors' as they would be analysed when the dependency changes.
        /// </summary>
        Suggestion,

        /// <summary>
        /// A warning about a potential problem. These will typically not prevent normal rule execution.
        /// Examples: certain type errors/warnings.
        /// Warnings will not be part of 'ChoreUpdateRulesWithErrors' as they would be analysed when the dependency changes.
        /// </summary>
        Warning,

        /// <summary>
        /// A moderate error that may prevent rules from executing properly.
        /// Examples: Service unavailable, service schema changed.
        /// </summary>
        Moderate,

        /// <summary>
        /// A severe error that will likely prevent rules from executing properly.
        /// This type of errors prevents generation of code and publishing.
        /// Examples: invocation of unknown functions, invalid names, certain type errors.
        /// </summary>
        Severe,

        /// <summary>
        /// A critical error, e.g. an error that prevents rules from executing properly.
        /// This type of errors prevent generation of code and publishing.
        /// Example: syntax errors.
        /// </summary>
        Critical,

#pragma warning disable SA1300 // Element should begin with upper-case letter
        _Lim = Critical,
#pragma warning restore SA1300 // Element should begin with upper-case letter
    }
}
