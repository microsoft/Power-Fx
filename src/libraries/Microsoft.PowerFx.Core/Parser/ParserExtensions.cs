// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerFx.Core.Public;

namespace Microsoft.PowerFx.Core.Parser
{
    /// <summary>
    /// Parser Extensions.
    /// </summary>
    internal static class ParserExtensions
    {
        internal static ParserOptions ToParserOptions(this TexlParser.Flags flags, CultureInfo culture)
        {
            var parserOptions = new ParserOptions(culture);

            if (flags.HasFlag(TexlParser.Flags.EnableExpressionChaining))
            {
                parserOptions.AllowsSideEffects = true; 
            }

            if (flags.HasFlag(TexlParser.Flags.NumberIsFloat))
            {
                parserOptions.NumberIsFloat = true;
            }

            return parserOptions; 
        }
    }
}
