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

        internal readonly string BodyScript;

        internal readonly TexlNode Body;

        internal readonly FormulaType ReturnType;

        internal readonly IEnumerable<NamedFormulaType> Parameters;

        internal readonly bool IsImperative;

        public UDFDefinition(string name, string bodyExpression, TexlNode body, FormulaType returnType, bool isImperative, IEnumerable<NamedFormulaType> parameters)
        {
            Name = name;
            BodyScript = bodyExpression;
            Body = body;
            ReturnType = returnType;
            IsImperative = isImperative;
            Parameters = parameters;
        }

        public UDFDefinition(string name, string bodyExpression, TexlNode body, FormulaType returnType, bool isImperative, params NamedFormulaType[] parameters)
            : this(name, bodyExpression, body, returnType, isImperative, parameters.AsEnumerable())
        {
        }
    }
}
