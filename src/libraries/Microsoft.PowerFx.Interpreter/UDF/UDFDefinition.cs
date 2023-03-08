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

        internal readonly bool IsImperative;

        internal readonly bool NumberIsFloat;

        public UDFDefinition(string name, string body, FormulaType returnType, bool isImperative, bool numberIsFloat, IEnumerable<NamedFormulaType> parameters)
        {
            Name = name;
            Body = body;
            ReturnType = returnType;
            IsImperative = isImperative;
            NumberIsFloat = numberIsFloat;
            Parameters = parameters;
        }

        public UDFDefinition(string name, string body, FormulaType returnType, bool isImperative, bool numberIsFloat, params NamedFormulaType[] parameters)
            : this(name, body, returnType, isImperative, numberIsFloat, parameters.AsEnumerable())
        {
        }
    }
}
