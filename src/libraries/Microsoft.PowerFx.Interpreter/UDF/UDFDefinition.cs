// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    internal class UDFDefinition
    {
        internal readonly string Name;

        internal readonly string Body;

        internal readonly FormulaType ReturnType;

        internal readonly IEnumerable<NamedFormulaType> Parameters;

        public UDFDefinition(string name, string body, FormulaType returnType, IEnumerable<NamedFormulaType> parameters)
        {
            Name = name;
            Body = body;
            ReturnType = returnType;
            Parameters = parameters;
        }

        public UDFDefinition(string name, string body, FormulaType returnType, params NamedFormulaType[] parameters)
            : this(name, body, returnType, parameters.AsEnumerable())
        {
        }
    }
}
