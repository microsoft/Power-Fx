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
                
        internal CultureInfo Culture { get; set; }

        internal ParseResult Parse(string script)
        {
            var flags = AllowsSideEffects ? TexlParser.Flags.EnableExpressionChaining : TexlParser.Flags.None;

            return TexlParser.ParseScript(script, Culture, flags);
        }
    }
}
