// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// Code action kind constants.
    /// </summary>
    public class CodeActionKind
    {
        /// <summary>
        /// Code action kind quickfix.
        /// </summary>
        public const string QuickFix = "quickfix";
    }

    /// <summary>
    /// Command name constants.
    /// </summary>
    public class CommandName
    {
        /// <summary>
        /// Code action applied command.
        /// </summary>
        public const string CodeActionApplied = "codeActionApplied";
    }
}
