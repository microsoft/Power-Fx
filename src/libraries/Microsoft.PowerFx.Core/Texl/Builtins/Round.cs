// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abstract base for all scalar flavors of round (Round, RoundUp, RoundDown)
    internal abstract class ScalarRoundingFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgPreprocessor(int index)
        {
            return index == 0 ? ArgPreprocessor.ReplaceBlankWithFuncResultTypedZero : ArgPreprocessor.ReplaceBlankWithFloatZero;
        }

        public override bool IsSelfContained => true;

        public ScalarRoundingFunction(string name, TexlStrings.StringGetter description, int arityMin)
            : base(name, description, FunctionCategories.MathAndStat, DType.Unknown, 0, arityMin, 2, DType.Unknown, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RoundArg1, TexlStrings.RoundArg2 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);
            Contracts.AssertValue(errors);

            var fValid = true;
            nodeToCoercedTypeMap = null;

            var arg0 = args[0];
            var arg0Type = argTypes[0];

            returnType = NumDecReturnType(context, nativeDecimal: true, arg0Type);

            if (CheckType(arg0, arg0Type, returnType, DefaultErrorContainer, out var matchedWithCoercion0))
            {
                if (matchedWithCoercion0)
                {
                    nodeToCoercedTypeMap = new Dictionary<TexlNode, DType>();
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, arg0, returnType, allowDupes: true);
                }
            }
            else
            {
                fValid = false;
            }

            if (args.Length == 2)
            {
                var arg1 = args[1];
                var arg1Type = argTypes[1];

                if (CheckType(arg1, arg1Type, DType.Number, DefaultErrorContainer, out var matchedWithCoercion1))
                {
                    if (matchedWithCoercion1)
                    {
                        if (nodeToCoercedTypeMap == null)
                        {
                            nodeToCoercedTypeMap = new Dictionary<TexlNode, DType>();
                        }

                        CollectionUtils.Add(ref nodeToCoercedTypeMap, arg1, DType.Number, allowDupes: true);
                    }
                }
                else
                {
                    fValid = false;
                }
            }

            if (!fValid)
            {
                nodeToCoercedTypeMap = null;
            }

            return fValid;
        }
    }

    // Abstract base for all overloads of round that take table arguments
    internal abstract class TableRoundingFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public TableRoundingFunction(string name, TexlStrings.StringGetter description, int arityMin)
            : base(name, description, FunctionCategories.Table, DType.EmptyTable, 0, arityMin, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RoundTArg1, TexlStrings.RoundTArg2 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var type0 = argTypes[0];

            var scalarType = NumDecReturnType(context, nativeDecimal: true, type0);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            // At least one of the arguments has to be a table.
            if (!argTypes[0].IsTable && (args.Length == 1 || !argTypes[1].IsTable))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTypeError);
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrTypeError);

                // Both args are invalid. No need to continue.
                fValid = false;
            }

            if (type0.IsTable)
            {
                // Ensure we have a one-column table of numerics
                fValid &= CheckNumDecColumnType(scalarType, type0, args[0], errors, ref nodeToCoercedTypeMap);

                // Decimal TODO: else case should be based on scalarType - ends up with same type as something coerced
                returnType = context.Features.HasFlag(Features.ConsistentOneColumnTableResult)
                    ? DType.CreateTable(new TypedName(scalarType, new DName(ColumnName_ValueStr)))
                    : type0;
            }
            else
            {
                if (!scalarType.Accepts(type0))
                {
                    if (type0.CoercesTo(scalarType))
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, args[0], scalarType);
                    }
                    else
                    {
                        fValid = false;
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTypeError);
                    }
                }

                // Since the 1st arg is not a table, make a new table return type *[Result:n] or
                // *[Value:n] if the consistent return schema flag is enabled
                returnType = DType.CreateTable(new TypedName(scalarType, GetOneColumnTableResultName(context.Features)));
            }

            if (args.Length == 2)
            {
                var type1 = argTypes[1];

                if (type1.IsTable)
                {
                    // Ensure we have a one-column table of numerics
                    fValid &= CheckNumericColumnType(type1, args[1], errors, ref nodeToCoercedTypeMap);
                }
                else
                {
                    if (!DType.Number.Accepts(type1))
                    {
                        if (type1.CoercesTo(DType.Number))
                        {
                            CollectionUtils.Add(ref nodeToCoercedTypeMap, args[1], DType.Number);
                        }
                        else
                        {
                            fValid = false;
                            errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTypeError);
                        }
                    }
                }
            }

            Contracts.Assert(returnType.IsTable);
            Contracts.Assert(!fValid || returnType.IsColumn);

            return fValid;
        }
    }

    // Round(number:n|w, digits:n|w)
    internal sealed class RoundScalarFunction : ScalarRoundingFunction
    {
        public RoundScalarFunction()
            : base("Round", TexlStrings.AboutRound, arityMin: 2)
        {
        }
    }

    // RoundUp(number:n, digits:n)
    internal sealed class RoundUpScalarFunction : ScalarRoundingFunction
    {
        public RoundUpScalarFunction()
            : base("RoundUp", TexlStrings.AboutRoundUp, arityMin: 2)
        {
        }
    }

    // RoundDown(number:n|w, digits:n|w)
    internal sealed class RoundDownScalarFunction : ScalarRoundingFunction
    {
        public RoundDownScalarFunction()
            : base("RoundDown", TexlStrings.AboutRoundDown, arityMin: 2)
        {
        }
    }

    // Round(number:w|n|*[w|n], digits:w|n|*[w|n])
    internal sealed class RoundTableFunction : TableRoundingFunction
    {
        public RoundTableFunction()
            : base("Round", TexlStrings.AboutRoundT, arityMin: 2)
        {
        }
    }

    // RoundUp(number:w|n|*[w|n], digits:w|n|*[w|n])
    internal sealed class RoundUpTableFunction : TableRoundingFunction
    {
        public RoundUpTableFunction()
            : base("RoundUp", TexlStrings.AboutRoundUpT, arityMin: 2)
        {
        }
    }

    // RoundDown(number:n|w|*[n|w], digits:n|w|*[n|w])
    internal sealed class RoundDownTableFunction : TableRoundingFunction
    {
        public RoundDownTableFunction()
            : base("RoundDown", TexlStrings.AboutRoundDownT, arityMin: 2)
        {
        }
    }
}
