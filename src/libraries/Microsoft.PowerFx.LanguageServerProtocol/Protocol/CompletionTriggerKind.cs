// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public enum CompletionTriggerKind
    {
        /// <summary>
        /// Completion was triggered by typing an identifier (24x7 code
        /// complete), manual invocation (e.g Ctrl+Space) or via API.
        /// </summary>
        Invoked = 1,

        /// <summary>
        /// Completion was triggered by a trigger character specified by
        /// the `triggerCharacters` properties of the
        /// `CompletionRegistrationOptions`.
        /// </summary>
        TriggerCharacter = 2,

        /// <summary>
        /// Completion was re-triggered as the current completion list is incomplete.
        /// </summary>
        TriggerForIncompleteCompletions = 3,
    }
}