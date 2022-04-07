// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class CompletionContext
    {
        public CompletionContext()
        {
            TriggerKind = CompletionTriggerKind.Invoked;
            TriggerCharacter = string.Empty;
        }

        /// <summary>
        /// How the completion was triggered.
        /// </summary>
        public CompletionTriggerKind TriggerKind { get; set; }

        /// <summary>
        /// The trigger character (a single character) that has trigger code
        /// complete. Is undefined if
        /// `triggerKind !== CompletionTriggerKind.TriggerCharacter`.
        /// </summary>
        public string TriggerCharacter { get; set; }
    }
}