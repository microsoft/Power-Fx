// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Additional information about the context in which a signature help request
    /// was triggered.
    /// </summary>
    public class SignatureHelpContext
    {
        /// <summary>
        /// Action that caused signature help to be triggered.
        /// </summary>
        public SignatureHelpTriggerKind TriggerKind { get; set; }

        /// <summary>
        /// Character that caused signature help to be triggered.
        ///
        /// This is undefined when triggerKind !==
        /// SignatureHelpTriggerKind.TriggerCharacter.
        /// </summary>
        public string TriggerCharacter { get; set; }

        /// <summary>
        /// `true` if signature help was already showing when it was triggered.
        /// 
        /// Retriggers occur when the signature help is already active and can be
        /// caused by actions such as typing a trigger character, a cursor move, or
        /// document content changes.
        /// </summary>
        public bool IsRetrigger { get; set; }

        /// <summary>
        /// The currently active `SignatureHelp`.
        ///
        /// The `activeSignatureHelp` has its `SignatureHelp.activeSignature` field
        /// updated based on the user navigating through available signatures.
        /// </summary>
        public SignatureHelp ActiveSignatureHelp { get; set; }
    }
}