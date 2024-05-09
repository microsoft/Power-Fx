// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Concatenate(source1:s, source2:s, ...)
    // Corresponding DAX function: Concatenate
    // This only performs string/string concatenation.
    internal sealed class ConcatenateFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public ConcatenateFunction()
            : base("Concatenate", TexlStrings.AboutConcatenate, FunctionCategories.Text, DType.String, 0, 1, int.MaxValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new TexlStrings.StringGetter[] { };
            yield return new[] { TexlStrings.ConcatenateArg1 };
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

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length >= 0);
            Contracts.AssertValue(errors);

            var count = args.Length;
            DType argOptionSet = null;
            TexlNode argOptionSetNode = null;
            var fArgsValid = true;
            var nonOptionSetArg = false;
            var concatAsString = false;
            nodeToCoercedTypeMap = null;

            for (var i = 0; i < count; i++)
            { 
                var argType = argTypes[i];

                if (argType == DType.OptionSetValue && context.Features.PowerFxV1CompatibilityRules)
                {
                    if (argOptionSet == null)
                    {
                        argOptionSet = argType;
                        argOptionSetNode = args[i];
                    }
                    
                    if (argOptionSet.OptionSetInfo.EntityName != argType.OptionSetInfo.EntityName ||
                        !argType.OptionSetInfo.CanConcatenateStronglyTyped)
                    {
                        concatAsString = true;
                    }
                }
                else
                {
                    nonOptionSetArg = true;
                    fArgsValid &= CheckType(context, args[i], argTypes[i], DType.String, errors, ref nodeToCoercedTypeMap);
                }
            }

            if (nonOptionSetArg && argOptionSet != null && !argOptionSet.OptionSetInfo.CanCoerceFromBackingKind)
            {
                concatAsString = true;
            }

            if (!fArgsValid)
            {
                nodeToCoercedTypeMap = null;
            }

            // All option sets can coerce to string and be concatenated as strings, which is the default returnType.
            // If there was only one option set type seen, and it has CanConcatenatedStronglyTyped set,
            // and either no string values were present or CanCoerceFromBackingKind is set, then Concatenate returns the enum type.
            // This behavior should mach that of the & operator (BinderUtils, CheckBinaryOpCore).
            returnType = argOptionSet != null && !concatAsString ? argOptionSet : DType.String;

            return fArgsValid;
        }
    }

    // Concatenate(source1:s|*[s], source2:s|*[s], ...)
    // Corresponding DAX function: Concatenate
    // Note, this performs string/table, table/table, table/string concatenation, but not string/string
    // Tables will be expanded to be the same size as the largest table. For each scalar, a new empty table
    // will be created, and the scalar value will be used to fill the table to be the same size as the largest table
    internal sealed class ConcatenateTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public ConcatenateTableFunction()
            : base("Concatenate", TexlStrings.AboutConcatenateT, FunctionCategories.Table | FunctionCategories.Text, DType.EmptyTable, 0, 1, int.MaxValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ConcatenateTArg1, TexlStrings.ConcatenateTArg1 };
            yield return new[] { TexlStrings.ConcatenateTArg1, TexlStrings.ConcatenateTArg1, TexlStrings.ConcatenateTArg1 };
            yield return new[] { TexlStrings.ConcatenateTArg1, TexlStrings.ConcatenateTArg1, TexlStrings.ConcatenateTArg1, TexlStrings.ConcatenateTArg1 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length >= 1);
            Contracts.AssertValue(errors);

            nodeToCoercedTypeMap = null;

            var count = args.Length;
            var hasTableArg = false;
            var fArgsValid = true;

            // Type check the args
            for (var i = 0; i < count; i++)
            {
                // The check for null on the next line is a special case for Concatenate, to retain prior behavior
                // See UntypedBlankAsTable.txt for more examples
                if (argTypes[i].IsTable && argTypes[i] != DType.ObjNull)
                {
                    fArgsValid &= CheckStringColumnType(context, args[i], argTypes[i], errors, ref nodeToCoercedTypeMap);
                    hasTableArg |= true;
                }
                else
                {
                    fArgsValid &= CheckType(context, args[i], argTypes[i], DType.String, errors, ref nodeToCoercedTypeMap);
                }
            }

            fArgsValid &= hasTableArg;

            if (!fArgsValid)
            {
                nodeToCoercedTypeMap = null;
            }

            returnType = DType.CreateTable(new TypedName(DType.String, GetOneColumnTableResultName(context.Features)));

            return fArgsValid;
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
