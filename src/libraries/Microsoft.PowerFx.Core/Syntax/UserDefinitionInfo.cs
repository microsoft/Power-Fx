// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    internal class UserDefinitionInfo
    {
        internal int Index;
        internal UserDefinitionType Type;
        internal IdentToken Name;
        internal string Declaration;
        internal string Script;
        internal readonly SourceList Before;
        internal readonly SourceList After;

        internal UserDefinitionInfo(int index, UserDefinitionType type, IdentToken name, string declaration, SourceList before, SourceList after = null)
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