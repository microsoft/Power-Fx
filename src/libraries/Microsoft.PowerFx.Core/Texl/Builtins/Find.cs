// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Find(find_text:s, within_text:s, [start_index:n])
    // Equivalent DAX function: Find
    internal sealed class FindFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public FindFunction()
            : base("Find", TexlStrings.AboutFind, FunctionCategories.Text, DType.Number, 0, 2, 3, DType.String, DType.String, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.FindArg1, TexlStrings.FindArg2 };
            yield return new[] { TexlStrings.FindArg1, TexlStrings.FindArg2, TexlStrings.FindArg3 };
        }
    }

    // Find(find_text:s|*[s], within_text:s|*[s], [start_index:n|*[n]])
    internal sealed class FindTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public FindTFunction()
            : base("Find", TexlStrings.AboutFindT, FunctionCategories.Table, DType.EmptyTable, 0, 2, 3)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.FindTArg1, TexlStrings.FindTArg2 };
            yield return new[] { TexlStrings.FindTArg1, TexlStrings.FindTArg2, TexlStrings.FindTArg3 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            var type0 = argTypes[0];
            var type1 = argTypes[1];

            // Arg0 should be either a string or a column of strings.
            if (type0.IsTable)
            {
                // Ensure we have a one-column table of strings.
                fValid &= CheckStringColumnType(type0, args[0], errors, ref nodeToCoercedTypeMap);
            }
            else if (!DType.String.Accepts(type0))
            {
                if (type0.CoercesTo(DType.String))
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[0], DType.String);
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrStringExpected);
                }
            }

            // Arg1 should be either a string or a column of strings.
            if (type1.IsTable)
            {
                fValid &= CheckStringColumnType(type1, args[1], errors, ref nodeToCoercedTypeMap);
            }
            else if (!DType.String.Accepts(type1))
            {
                if (type1.CoercesTo(DType.String))
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[1], DType.String);
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrStringExpected);
                }
            }

            returnType = DType.CreateTable(new TypedName(DType.Number, GetOneColumnTableResultName(context.Features)));

            var hasStartIndex = argTypes.Length == 3;

            if (hasStartIndex)
            {
                var type2 = argTypes[2];

                // Arg2 should be either a number or a column of numbers.
                if (argTypes[2].IsTable)
                {
                    fValid &= CheckNumericColumnType(type2, args[2], errors, ref nodeToCoercedTypeMap);
                }
                else if (!DType.Number.Accepts(type2))
                {
                    if (type2.CoercesTo(DType.Number))
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, args[2], DType.Number);
                    }
                    else
                    {
                        fValid = false;
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrNumberExpected);
                    }
                }
            }

            // At least one arg has to be a table.
            if (!(type0.IsTable || type1.IsTable) && (!hasStartIndex || !argTypes[2].IsTable))
            {
                fValid = false;
            }

            return fValid;
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
