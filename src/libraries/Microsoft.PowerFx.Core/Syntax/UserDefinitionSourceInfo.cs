// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    internal class UserDefinitionSourceInfo
    {
        internal int Index;
        internal UserDefinitionType Type;
        internal IdentToken Name;

        // Substring of Formulas script from definition identifier up to the assignment operator
        internal string Declaration;

        // Trivia before the definition body (after assignment operator)
        internal readonly SourceList Before;

        // Extra trivia after the definition body (before closing semicolon)
        internal readonly SourceList After;

        internal UserDefinitionSourceInfo(int index, UserDefinitionType type, IdentToken name, string declaration, SourceList before, SourceList after = null)
        {
            Index = index;
            Type = type;
            Name = name;
            Declaration = declaration;
            Before = before;
            After = after;
        }
    }
    
    internal enum UserDefinitionType
    {
        NamedFormula,
        UDF,
        DefinedType,
        Error,
    }
}
