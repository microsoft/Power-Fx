// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    using System;
    using System.Globalization;
    using Microsoft.PowerFx.Intellisense;
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

            if (info.GetDisclaimerMarkdown == null)
            {
                this.SetDocumentation(info.Documentation);
            }
            else 
            { 
                // Append disclaimer to create a final markdown string to send to LSP. 
                MarkdownString disclaimer = info.GetDisclaimerMarkdown();
                string original = info.Documentation;

                MarkdownString final = MarkdownString.FromString(original) + MarkdownString.Newline + disclaimer;

                this.SetDocumentation(final);
            }
                        
            // info doesn't have ActiveParameter
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
        /// If this is a <see cref="MarkupContent"/>, then it's github markdown. 
        /// </summary>
        public object Documentation { get; set; }

        /// <summary>
        /// Helper to set the documentation to plaintext. 
        /// </summary>
        /// <param name="plainText"></param>
        public void SetDocumentation(string plainText)
        {
            this.Documentation = plainText;
        }

        /// <summary>
        /// Helper to set the documentation to markdown. 
        /// </summary>
        /// <param name="markdown"></param>
        public void SetDocumentation(MarkdownString markdown)
        {
            this.Documentation = new MarkupContent
            {
                Value = markdown.Markdown
            };
        }

        /// <summary>
        /// The parameters of this signature.
        /// </summary>
        public ParameterInformation[] Parameters { get; set; }
    }
}
