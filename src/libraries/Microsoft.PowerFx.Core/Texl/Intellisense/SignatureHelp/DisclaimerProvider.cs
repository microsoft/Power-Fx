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
    internal class DisclaimerProvider
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
        public MarkdownString DisclaimerMarkdown
        {
            get
            {
                (var shortMessage, var _) = ErrorUtils.GetLocalizedErrorContent(
                    TexlStrings.IntellisenseAiDisclaimer, _locale, out _);

                return MarkdownString.FromMarkdown(shortMessage);
            }
        }

        public Func<MarkdownString> Getter => () => this.DisclaimerMarkdown;
    }
}
