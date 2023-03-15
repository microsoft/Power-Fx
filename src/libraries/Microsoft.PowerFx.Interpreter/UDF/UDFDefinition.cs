// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    internal class UDFDefinition
    {
        internal readonly string Name;

        internal readonly TexlNode Body;

        internal readonly FormulaType ReturnType;

        internal readonly IEnumerable<NamedFormulaType> Parameters;

        internal readonly bool IsImperative;

        public UDFDefinition(string name, TexlNode body, FormulaType returnType, bool isImperative, IEnumerable<NamedFormulaType> parameters)
        {
            Name = name;
            Body = body;
            ReturnType = returnType;
            IsImperative = isImperative;
            Parameters = parameters;
        }

        public UDFDefinition(string name, TexlNode body, FormulaType returnType, bool isImperative, params NamedFormulaType[] parameters)
            : this(name, body, returnType, isImperative, parameters.AsEnumerable())
        {
        }
    }
}
