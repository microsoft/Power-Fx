// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    using System;
    using SignatureInformationCore = Microsoft.PowerFx.Intellisense.SignatureHelp.SignatureInformation;

    /// <summary>
    /// Represents the signature of something callable. A signature
    /// can have a label, like a function-name, a doc-comment, and
    /// a set of parameters.
    /// </summary>
    public class SignatureInformation
    {
        public SignatureInformation() 
        { 
        }

        public SignatureInformation(SignatureInformationCore info)
        {
            if (info.Parameters != null)
            {
                this.Parameters = Array.ConvertAll(info.Parameters, x => new ParameterInformation(x));
            }

            this.Label = info.Label;
            this.Documentation = info.Documentation;    
            if (info.ShowAIDisclaimer)
            {
                this.Documentation = GetDisclaimer(info.Documentation);
            }
                        
            // info doesn't have ActiveParameter
        }

        // Given a string, get the AI disclaimer. 
        private static MarkdownString GetDisclaimer(string original)
        {
            // $$$ Get these from resources. 
            var link = "https://go.microsoft.com/fwlink/?linkid=2225491";
            var msg = $"**Disclaimer:** AI-generated content can have mistakes. Make sure it's accurate and appropriate before using it. [See terms]({link})";

            return new MarkdownString
            {
                Value = original + "\r\n" + msg
            };
        }

        /// <summary>
        /// The label of this signature. Will be shown in
        /// the UI.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The human-readable doc-comment of this signature. Will be shown
        /// in the UI but can be omitted.
        /// If this is a string, it's plain text. 
        /// If this is a <see cref="MarkdownString"/>, then it's github markdown. 
        /// </summary>
        public object Documentation { get; set; }

        /// <summary>
        /// The parameters of this signature.
        /// </summary>
        public ParameterInformation[] Parameters { get; set; }

        /// <summary>
        /// The index of the active parameter.
        ///
        /// If provided, this is used in place of `SignatureHelp.activeParameter`.
        /// </summary>
        public uint ActiveParameter { get; set; }
    }

    /// <summary>
    /// Github flavored Markdown string.
    /// See https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#markupContent.
    /// </summary>
    public class MarkdownString
    {
        public string MarkupKind = "markdown"; // "plaintext"

        public string Value { get; set; }
    }
}
