// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Concatenate(source1:s|*[s], source2:s|*[s], ...)
    // Corresponding DAX function: Concatenate
    // Note, this performs string/string, string/Table, table/Table concatenation.
    internal sealed class ConcatenateFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;
        public override bool SupportsParamCoercion => true;

        public ConcatenateFunction()
            : base("Concatenate", TexlStrings.AboutConcatenate, FunctionCategories.Table | FunctionCategories.Text, DType.Unknown, 0, 2, int.MaxValue)
        { }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1 };
            yield return new[] { TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1 };
            yield return new[] { TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length >= 2);
            Contracts.AssertValue(errors);

            nodeToCoercedTypeMap = null;

            var count = args.Length;
            var hasTableArg = false;
            var fArgsValid = true;

            // Type check the args
            // If any one input argument is of table type, then the returnType will be table type.
            for (var i = 0; i < count; i++)
            {
                fArgsValid &= CheckParamIsTypeOrSingleColumnTable(DType.String, args[i], argTypes[i], errors, out var isTable, ref nodeToCoercedTypeMap);
                hasTableArg |= isTable;
            }

            returnType = hasTableArg ? DType.CreateTable(new TypedName(DType.String, OneColumnTableResultName)) : DType.String;

            return fArgsValid;
        }
    }
}
