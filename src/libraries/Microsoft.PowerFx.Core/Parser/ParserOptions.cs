// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Options for parsing an expression.
    /// </summary>
    public class ParserOptions
    {
        /// <summary>
        /// If true, allow parsing a chaining operator. This is only used for side-effecting operations.
        /// </summary>
        public bool AllowsSideEffects { get; set; }

        /// <summary>
        /// If true, numbers are treated as float.  By default, numbers are treated as decimal.
        /// </summary>
        public bool NumberIsFloat { get; set; }

        /// <summary>
        /// If true, various words have been reserved and are not available for identifiers.
        /// Will only be set by Canvas apps, all other Power Fx hosts should have reserved keywords always enforced.
        /// </summary>
        internal bool DisableReservedKeywords { get; set; }

        /// <summary>
        /// The culture that an expression will parse with. 
        /// This primarily determines numeric decimal separator character
        /// as well as chaining operator. 
        /// </summary>
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// If greater than 0, enforces a maximum length on a single expression. 
        /// It is an immediate parser error if the expression is too long. 
        /// </summary>
        public int MaxExpressionLength { get; set; }

        /// <summary>
        /// Flag for parse type literals.
        /// </summary>
        internal bool AllowParseAsTypeLiteral { get; set; }

        public ParserOptions()
        {
        }

        public ParserOptions(CultureInfo culture, bool allowsSideEffects = false, int maxExpressionLength = 0, bool allowParseAsTypeLiteral = false)
        {
            Culture = culture;
            AllowsSideEffects = allowsSideEffects;
            MaxExpressionLength = maxExpressionLength;
            AllowParseAsTypeLiteral = allowParseAsTypeLiteral;
        }

        internal ParseResult Parse(string script)
        {
            return Parse(script, Features.None);
        }

        internal ParseResult Parse(string script, Features features)
        {
            if (MaxExpressionLength > 0 && script.Length > MaxExpressionLength)
            {
                // If too long, don't even attempt to lex or parse it. 
                var result2 = ParseResult.ErrorTooLarge(script, MaxExpressionLength);
                result2.Options = this;
                return result2;
            }

            var flags = (AllowsSideEffects ? TexlParser.Flags.EnableExpressionChaining : 0) |
                        (NumberIsFloat ? TexlParser.Flags.NumberIsFloat : 0) |
                        (DisableReservedKeywords ? TexlParser.Flags.DisableReservedKeywords : 0);

            var result = TexlParser.ParseScript(script, features, Culture, flags);
            result.Options = this;
            return result;
        }
    }
}
