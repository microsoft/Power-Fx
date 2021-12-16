// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public enum DiagnosticSeverity
    {
        /// <summary>
        /// Reports an error.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Reports a warning.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Reports information.
        /// </summary>
        Information = 3,

        /// <summary>
        /// Reports a hint.
        /// </summary>
        Hint = 4,
    }
}