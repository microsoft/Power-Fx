// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Sqrt(number:n)
    // Equivalent DAX function: Sqrt
    internal sealed class SqrtFunction : MathOneArgFunction
    {
        public SqrtFunction()
            : base("Sqrt", TexlStrings.AboutSqrt, FunctionCategories.MathAndStat)
        {
        }      
    }

    // Sqrt(E:*[n])
    // Table overload that computes the square root values of each item in the input table.
    internal sealed class SqrtTableFunction : MathOneArgTableFunction
    {
        public SqrtTableFunction()
            : base("Sqrt", TexlStrings.AboutSqrtT, FunctionCategories.Table)
        {
        }

        public override bool CheckInvocation(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var fValid = base.CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            
            if (returnType.IsTable && returnType != DType.EmptyRecord.Add(new TypedName(DType.Number, ColumnName_Value)).ToTable())
            {
                var arg = args[0];
                var argType = argTypes[0];
                fValid &= CheckStringColumnType(argType, arg, errors, ref nodeToCoercedTypeMap);

                var rowType = DType.EmptyRecord.Add(new TypedName(DType.Number, ColumnName_Value));
                returnType = rowType.ToTable();
            }

            return fValid;
        }
    }
}
