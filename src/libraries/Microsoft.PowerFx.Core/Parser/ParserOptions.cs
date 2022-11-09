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
        /// If true, This would allow users to do partial type checking by enabling support for Unknown type in input.
        /// </summary>
        public bool AllowDeferredType { get; set; }

        /// <summary>
        /// If true, allow parsing a chaining operator. This is only used for side-effecting operations.
        /// </summary>
        public bool AllowsSideEffects { get; set; }

        public CultureInfo Culture { get; set; }

        internal ParseResult Parse(string script)
        {
            return Parse(script, Features.None);
        }

        internal ParseResult Parse(string script, Features features)
        {
            var flags = AllowsSideEffects ? TexlParser.Flags.EnableExpressionChaining : TexlParser.Flags.None;

            return TexlParser.ParseScript(script, features, Culture, flags);
        }
    }
}
