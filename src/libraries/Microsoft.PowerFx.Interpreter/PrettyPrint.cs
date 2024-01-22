// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Interpreter
{
    public class PrettyPrint
    {
        public static string RemoveNonNecessaryCharacters(string expression)
        {
            var parseResult = TexlParser.ParseScript(expression);
            var node = parseResult.Root;
            return TexlPretty.PrettyPrint(node);
        }
    }
}
