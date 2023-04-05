// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abstract base class for all 1-arg math functions that return numeric values.
    internal abstract class MathOneArgFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgPreprocessor(int index)
        {
            return base.GetGenericArgPreprocessor(index);
        }

        public override bool IsSelfContained => true;

        public MathOneArgFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc)
            : base(name, description, fc, DType.Number, 0, 1, 1, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MathFuncArg1 };
        }
    }

    internal abstract class MathOneArgTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public MathOneArgTableFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc)
            : base(name, description, fc, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MathTFuncArg1 };
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
            Contracts.Assert(args.Length == 1);
            Contracts.AssertValue(errors);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            var arg = args[0];
            var argType = argTypes[0];
            fValid &= CheckNumericColumnType(argType, arg, errors, ref nodeToCoercedTypeMap, context, out returnType);

            if (!fValid)
            {
                nodeToCoercedTypeMap = null;
            }

            return fValid;
        }
    }

    // Abstract base class for all 2-arg math functions that return numeric values.
    internal abstract class MathTwoArgFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgPreprocessor(int index)
        {
            return base.GetGenericArgPreprocessor(index);
        }

        public override bool IsSelfContained => true;

        public MathTwoArgFunction(string name, TexlStrings.StringGetter description, int minArity)
            : base(name, description, FunctionCategories.MathAndStat, DType.Number, 0, minArity, 2, DType.Number, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            if (MinArity == 1)
            {
                yield return new[] { TexlStrings.MathTFuncArg1 };
            }

            yield return new[] { TexlStrings.MathTFuncArg1, TexlStrings.MathTFuncArg2 };
        }
    }

    internal abstract class MathTwoArgTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        // Before ConsistentOneColumnTableResult, this function would always return a fixed name "Result" (Mod)
        public virtual bool InConsistentTableResultFixedName => false;

        // Before ConsistentOneColumnTableResult, this function would use the second argument name if a table (Log, Power)
        public virtual bool InConsistentTableResultUseSecondArg => false;

        public MathTwoArgTableFunction(string name, TexlStrings.StringGetter description, int minArity)
            : base(name, description, FunctionCategories.Table, DType.EmptyTable, 0, minArity, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            if (MinArity == 1)
            {
                yield return new[] { TexlStrings.MathTFuncArg1 };
            }

            yield return new[] { TexlStrings.MathTFuncArg1, TexlStrings.MathTFuncArg2 };
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
            Contracts.Assert(args.Length >= 1 && args.Length <= 2);
            Contracts.AssertValue(errors);
            Contracts.Assert(!InConsistentTableResultFixedName || !InConsistentTableResultUseSecondArg);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            if (argTypes.Length == 2)
            {
                var type0 = argTypes[0];
                var type1 = argTypes[1];

                var otherType = DType.Invalid;
                TexlNode otherArg = null;

                // At least one of the arguments has to be a table.
                if (type0.IsTable)
                {
                    // Ensure we have a one-column table of numerics
                    if (InConsistentTableResultFixedName)
                    {
                        fValid &= CheckNumericColumnType(type0, args[0], errors, ref nodeToCoercedTypeMap);
                        returnType = DType.CreateTable(new TypedName(DType.Number, GetOneColumnTableResultName(context.Features)));
                    }
                    else
                    {
                        fValid &= CheckNumericColumnType(type0, args[0], errors, ref nodeToCoercedTypeMap, context, out returnType);
                    }

                    // Check arg1 below.
                    otherArg = args[1];
                    otherType = type1;
                }
                else if (type1.IsTable)
                {
                    // Ensure we have a one-column table of numerics
                    if (InConsistentTableResultUseSecondArg)
                    {
                        fValid &= CheckNumericColumnType(type1, args[1], errors, ref nodeToCoercedTypeMap, context, out returnType);
                    }
                    else
                    {
                        fValid &= CheckNumericColumnType(type1, args[1], errors, ref nodeToCoercedTypeMap);

                        // Since the 1st arg is not a table, make a new table return type *[Result:n]
                        returnType = DType.CreateTable(new TypedName(DType.Number, GetOneColumnTableResultName(context.Features)));
                    }

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
                else if (!CheckType(otherArg, otherType, DType.Number, errors, ref nodeToCoercedTypeMap))
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, otherArg, TexlStrings.ErrTypeError);
                }
            }
            else
            {
                var type0 = argTypes[0];

                if (type0.IsTable)
                {
                    // Ensure we have a one-column table of numerics
                    fValid &= CheckNumericColumnType(type0, args[0], errors, ref nodeToCoercedTypeMap, context, out returnType);
                }
                else
                {
                    Contracts.Assert(returnType.IsTable);
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrTypeError);
                    fValid = false;
                }
            }

            return fValid;
        }
    }
}
