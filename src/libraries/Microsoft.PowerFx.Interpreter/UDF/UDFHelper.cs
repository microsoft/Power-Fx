// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    internal static class UDFHelper
    {
        internal static IEnumerable<NamedValue> Zip(NamedFormulaType[] parameters, FormulaValue[] args)
        {
            if (parameters.Length != args.Length)
            {
                throw new ArgumentException();
            }

            var result = new NamedValue[args.Length];
            for (var i = 0; i < args.Length; i++)
            {
                result[i] = new NamedValue(parameters[i].Name, args[i]);
            }

            return result;
        }
    }
}
