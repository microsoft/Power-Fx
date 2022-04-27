// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Public;

namespace Microsoft.PowerFx.Core.Parser
{
    /// <summary>
    /// Parser Extensions.
    /// </summary>
    internal static class ParserExtensions
    {
        internal static ParserOptions ToParserOptions(this TexlParser.Flags flags)
        {
            var parserOptions = new ParserOptions();

            if ((flags & TexlParser.Flags.EnableExpressionChaining) != 0)
            {
                parserOptions.AllowsSideEffects = true; 
            }

            return parserOptions; 
        }
    }
}
