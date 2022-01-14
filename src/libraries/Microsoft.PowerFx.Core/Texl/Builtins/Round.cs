// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abstract base for all scalar flavors of round (Round, RoundUp, RoundDown)
    internal abstract class ScalarRoundingFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public ScalarRoundingFunction(string name, TexlStrings.StringGetter description)
            : base(name, description, FunctionCategories.MathAndStat, DType.Number, 0, 2, 2, DType.Number, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.RoundArg1, TexlStrings.RoundArg2 };
        }
    }

    // Abstract base for all overloads of round that take table arguments
    internal abstract class TableRoundingFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public TableRoundingFunction(string name, TexlStrings.StringGetter description)
            : base(name, description, FunctionCategories.Table, DType.EmptyTable, 0, 2, 2)
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

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            var type0 = argTypes[0];
            var type1 = argTypes[1];

            var otherType = DType.Invalid;
            TexlNode otherArg = null;

            // At least one of the arguments has to be a table.
            if (type0.IsTable)
            {
                // Ensure we have a one-column table of numerics
                fValid &= CheckNumericColumnType(type0, args[0], errors, ref nodeToCoercedTypeMap);

                // Borrow the return type from the 1st arg
                returnType = type0;

                // Check arg1 below.
                otherArg = args[1];
                otherType = type1;
            }
            else if (type1.IsTable)
            {
                // Ensure we have a one-column table of numerics
                fValid &= CheckNumericColumnType(type1, args[1], errors, ref nodeToCoercedTypeMap);

                // Since the 1st arg is not a table, make a new table return type *[Result:n]
                returnType = DType.CreateTable(new TypedName(DType.Number, OneColumnTableResultName));

                // Check arg0 below.
                otherArg = args[0];
                otherType = type0;
            }
            else
            {
                Contracts.Assert(returnType.IsTable);
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTypeError);
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrTypeError);

                // Both args are invalid. No need to continue.
                return false;
            }

            Contracts.Assert(otherType.IsValid);
            Contracts.AssertValue(otherArg);
            Contracts.Assert(returnType.IsTable);
            Contracts.Assert(!fValid || returnType.IsColumn);

            if (otherType.IsTable)
            {
                // Ensure we have a one-column table of numerics
                fValid &= CheckNumericColumnType(otherType, otherArg, errors, ref nodeToCoercedTypeMap);
            }
            else if (!DType.Number.Accepts(otherType))
            {
                if (otherType.CoercesTo(DType.Number))
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, otherArg, DType.Number);
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, otherArg, TexlStrings.ErrTypeError);
                }
            }

            return fValid;
        }
    }

    // Round(number:n, digits:n)
    internal sealed class RoundScalarFunction : ScalarRoundingFunction
    {
        public RoundScalarFunction()
            : base("Round", TexlStrings.AboutRound)
        {
        }
    }

    // RoundUp(number:n, digits:n)
    internal sealed class RoundUpScalarFunction : ScalarRoundingFunction
    {
        public RoundUpScalarFunction()
            : base("RoundUp", TexlStrings.AboutRoundUp)
        {
        }
    }

    // RoundDown(number:n, digits:n)
    internal sealed class RoundDownScalarFunction : ScalarRoundingFunction
    {
        public RoundDownScalarFunction()
            : base("RoundDown", TexlStrings.AboutRoundDown)
        {
        }
    }

    // Round(number:n|*[n], digits:n|*[n])
    internal sealed class RoundTableFunction : TableRoundingFunction
    {
        public RoundTableFunction()
            : base("Round", TexlStrings.AboutRoundT)
        {
        }
    }

    // RoundUp(number:n|*[n], digits:n|*[n])
    internal sealed class RoundUpTableFunction : TableRoundingFunction
    {
        public RoundUpTableFunction()
            : base("RoundUp", TexlStrings.AboutRoundUpT)
        {
        }
    }

    // RoundDown(number:n|*[n], digits:n|*[n])
    internal sealed class RoundDownTableFunction : TableRoundingFunction
    {
        public RoundDownTableFunction()
            : base("RoundDown", TexlStrings.AboutRoundDownT)
        {
        }
    }
}
