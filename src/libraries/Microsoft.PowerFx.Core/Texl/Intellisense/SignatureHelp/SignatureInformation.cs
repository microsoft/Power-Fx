// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;

namespace Microsoft.PowerFx.Intellisense.SignatureHelp
{
    /// <summary>
    /// Get Markdown for AI disclaimer. 
    /// </summary>
    public class DisclaimerProvider
    {
        private readonly CultureInfo _locale;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisclaimerProvider"/> class.
        /// </summary>
        /// <param name="locale">locale for language to show messages in.</param>
        public DisclaimerProvider(CultureInfo locale)
        {
            _locale = locale;
        }

        /// <summary>
        /// Get the disclaimer in markdown. This may contain bold, hyperlinks, etc. 
        /// </summary>
        /// <returns></returns>
        public string DisclaimerMarkdown 
        {
            get
            {
                (var shortMessage, var _) = ErrorUtils.GetLocalizedErrorContent(
                    TexlStrings.IntellisenseAiDisclaimer, _locale, out _);

                return shortMessage;
            }
        }
    }

    public class SignatureInformation : IEquatable<SignatureInformation>
    {
        public string Label { get; set; }

        public string Documentation { get; set; }

        public ParameterInformation[] Parameters { get; set; }

        // If non-null, then append an AI disclaimer to the function. 
        // $$$ Better name? Not just AI... Generic getter? Func<MarkdownString>
        public DisclaimerProvider ShowAIDisclaimer { get; set; }

        public bool Equals(SignatureInformation other)
        {
            if (other == null)
            {
                return false;
            }

            // Label is not enough to compare two signature information
            if (Label != other.Label)
            {
                return false;
            }

            // Functions like Boolean, and Left have similar signature/label but different documentation
            // For example Boolean(text) => "Converts a 'text' that represents a boolean to a boolean value
            //                        And
            //             Boolean(text) =>  "Converts a 'number' to a boolean value."
            // This requires us to consider documentation as well
            if (Documentation != other.Documentation)
            {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is SignatureInformation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Label, Documentation).GetHashCode();
        }
    }
}
