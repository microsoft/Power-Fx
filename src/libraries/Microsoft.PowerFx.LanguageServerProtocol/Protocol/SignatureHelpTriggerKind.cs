// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public enum SignatureHelpTriggerKind
    {
        /// <summary>
        /// Signature help was invoked manually by the user or by a command.
        /// </summary>
        Invoked = 1,

        /// <summary>
        /// Signature help was triggered by a trigger character.
        /// </summary>
        TriggerCharacter = 2,

        /// <summary>
        /// Signature help was triggered by the cursor moving or by the document
        /// content changing.
        /// </summary>
        ContentChange = 3,
    }
}
