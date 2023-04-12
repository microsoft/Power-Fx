// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abstract base class for all 1-arg math functions that return numeric values.
    internal abstract class MathOneArgFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgPreprocessor(int index)
        {
            return _nativeDecimal ? ArgPreprocessor.ReplaceBlankWithCallZero_Scalar : ArgPreprocessor.ReplaceBlankWithFloatZero;
        }

        public override bool IsSelfContained => true;

        // This function natively supports decimal inputs with decimal outputs, without coercion to float.
        // Most math functions, for example Sin, Cos, Degrees, etc. coerce their input to floating point
        // and return a floating point output.
        private readonly bool _nativeDecimal = false;

        public MathOneArgFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, bool nativeDecimal = false)
            : base(name, description, fc, nativeDecimal ? DType.Unknown : DType.Number, 0, 1, 1, nativeDecimal ? DType.Unknown : DType.Number)
        {
            _nativeDecimal = nativeDecimal;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MathFuncArg1 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length == 1);
            Contracts.AssertValue(errors);

            var fValid = true;
            nodeToCoercedTypeMap = null;

            returnType = DetermineNumericFunctionReturnType(_nativeDecimal, context.NumberIsFloat, argTypes[0]);

            fValid &= CheckType(args[0], argTypes[0], returnType, errors, ref nodeToCoercedTypeMap);

            if (!fValid)
            { 
                nodeToCoercedTypeMap = null;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNumberExpected);
            }

            return fValid;
        }
    }

    internal abstract class MathOneArgTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        // This function natively supports decimal inputs with decimal outputs, without coercion to float.
        // Most math functions, for example Sin, Cos, Degrees, etc. coerce their input to floating point
        // and return a floating point output.
        private readonly bool _nativeDecimal = false;

        public MathOneArgTableFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, bool nativeDecimal = false)
            : base(name, description, fc, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
            _nativeDecimal = nativeDecimal;
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

            if (argType.IsTable)
            { 
                fValid &= TryGetSingleColumn(argType, arg, errors, out var column);
                var returnScalarType = DetermineNumericFunctionReturnType(_nativeDecimal, context.NumberIsFloat, column.Type);
                fValid &= CheckColumnType(argType, arg, column, returnScalarType, errors, ref nodeToCoercedTypeMap, context, out returnType);
            }
            else
            {
                fValid = false;
                Contracts.Assert(returnType.IsTable);
                errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrTypeError);
            }

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
            return _nativeDecimal ? ArgPreprocessor.ReplaceBlankWithCallZero_Scalar : ArgPreprocessor.ReplaceBlankWithFloatZero;
        }

        public override bool IsSelfContained => true;

        // This function natively supports decimal inputs with decimal outputs, without coercion to float.
        // Most math functions, for example Sin, Cos, Degrees, etc. coerce their input to floating point
        // and return a floating point output.
        private readonly bool _nativeDecimal = false;

        public MathTwoArgFunction(string name, TexlStrings.StringGetter description, int minArity, bool nativeDecimal = false)
            : base(name, description, FunctionCategories.MathAndStat, nativeDecimal ? DType.Unknown : DType.Number, 0, minArity, 2, nativeDecimal ? DType.Unknown : DType.Number, nativeDecimal ? DType.Unknown : DType.Number)
        {
            _nativeDecimal = nativeDecimal;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            if (MinArity == 1)
            {
                yield return new[] { TexlStrings.MathTFuncArg1 };
            }

            yield return new[] { TexlStrings.MathTFuncArg1, TexlStrings.MathTFuncArg2 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length >= MinArity && args.Length <= MaxArity);
            Contracts.AssertValue(errors);

            var fValid = true;
            nodeToCoercedTypeMap = null;

            returnType = DetermineNumericFunctionReturnType(_nativeDecimal, context.NumberIsFloat, argTypes[0]);

            if (!CheckType(args[0], argTypes[0], returnType, errors, ref nodeToCoercedTypeMap))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNumberExpected);
                fValid = false;
            }

            if (args.Length == 2 && 
                !CheckType(args[1], argTypes[1], returnType, errors, ref nodeToCoercedTypeMap))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrNumberExpected);
                fValid = false;
            }

            return fValid;
        }
    }

    internal abstract class MathTwoArgTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        // This function natively supports decimal inputs with decimal outputs, without coercion to float.
        // Most math functions, for example Sin, Cos, Degrees, etc. coerce their input to floating point
        // and return a floating point output.
        private readonly bool _nativeDecimal = false;

        // Before ConsistentOneColumnTableResult, this function would always return a fixed name "Result" (Mod)
        protected virtual bool InConsistentTableResultFixedName => false;

        // Before ConsistentOneColumnTableResult, this function would use the second argument name if a table (Log, Power)
        protected virtual bool InConsistentTableResultUseSecondArg => false;

        public MathTwoArgTableFunction(string name, TexlStrings.StringGetter description, int minArity, bool nativeDecimal = false)
            : base(name, description, FunctionCategories.Table, DType.EmptyTable, 0, minArity, 2)
        {
            _nativeDecimal = nativeDecimal;
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

            DType returnScalarType;

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
                    fValid &= TryGetSingleColumn(type0, args[0], errors, out var column0);
                    returnScalarType = DetermineNumericFunctionReturnType(_nativeDecimal, context.NumberIsFloat, column0.Type);

                    // Ensure we have a one-column table of numerics
                    if (InConsistentTableResultFixedName)
                    {
                        fValid &= CheckColumnType(type0, args[0], column0, returnScalarType, errors, ref nodeToCoercedTypeMap);
                        returnType = DType.CreateTable(new TypedName(returnScalarType, GetOneColumnTableResultName(context.Features)));
                    }
                    else
                    {
                        fValid &= CheckColumnType(type0, args[0], column0, returnScalarType, errors, ref nodeToCoercedTypeMap, context, out returnType);
                    }

                    // Check arg1 below.
                    otherArg = args[1];
                    otherType = type1;
                }
                else if (type1.IsTable)
                {
                    fValid &= TryGetSingleColumn(type1, args[1], errors, out var column1);
                    returnScalarType = DetermineNumericFunctionReturnType(_nativeDecimal, context.NumberIsFloat, type0);

                    // Ensure we have a one-column table of numerics
                    if (InConsistentTableResultUseSecondArg)
                    {
                        fValid &= CheckColumnType(type1, args[1], column1, returnScalarType, errors, ref nodeToCoercedTypeMap, context, out returnType);
                    }
                    else
                    {
                        fValid &= CheckColumnType(type1, args[1], column1, returnScalarType, errors, ref nodeToCoercedTypeMap);

                        // Since the 1st arg is not a table, make a new table return type *[Result:n]
                        returnType = DType.CreateTable(new TypedName(returnScalarType, GetOneColumnTableResultName(context.Features)));
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
                    fValid &= TryGetSingleColumn(otherType, otherArg, errors, out var otherColumn);
                    fValid &= CheckColumnType(otherType, otherArg, otherColumn, returnScalarType, errors, ref nodeToCoercedTypeMap);
                }
                else if (!CheckType(otherArg, otherType, returnScalarType, errors, ref nodeToCoercedTypeMap))
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
                    fValid &= TryGetSingleColumn(type0, args[0], errors, out var oneArgColumn);
                    returnScalarType = DetermineNumericFunctionReturnType(_nativeDecimal, context.NumberIsFloat, oneArgColumn.Type);
                    fValid &= CheckColumnType(type0, args[0], oneArgColumn, returnScalarType, errors, ref nodeToCoercedTypeMap, context, out returnType);
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
