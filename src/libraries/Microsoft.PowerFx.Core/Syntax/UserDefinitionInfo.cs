// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

internal class UserDefinitionInfo
{
    internal int Index;
    internal UserDefinitionType Type;
    internal IdentToken Name;
    internal string Declaration;
    internal string Script;
    internal SourceList Before;
    internal SourceList After;

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
