// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Boolean(arg:s)
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanFunction : BuiltinFunction
    {
        public const string BooleanInvariantFunctionName = "Boolean";

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanFunction()
            : base(BooleanInvariantFunctionName, TexlStrings.AboutBoolean, FunctionCategories.Text, DType.Boolean, 0, 1, 1, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanArg1 };
        }
    }

    // Boolean(E:*[s])
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanFunction_T : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanFunction_T()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBooleanT, FunctionCategories.Table, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanTArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            var arg = args[0];
            var argType = argTypes[0];
            fValid &= CheckStringColumnType(argType, arg, errors, ref nodeToCoercedTypeMap);

            var rowType = DType.EmptyRecord.Add(new TypedName(DType.Boolean, ColumnName_Value));
            returnType = rowType.ToTable();

            return fValid;
        }

        public override bool TryGetParamDescription(string paramName, out string paramDescription)
        {
            Contracts.AssertNonEmpty(paramName);

            return StringResources.TryGet("AboutBooleanT_" + paramName, out paramDescription);
        }
    }

    // Boolean(arg:n)
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanNFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanNFunction()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBooleanN, FunctionCategories.Text, DType.Boolean, 0, 1, 1, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanNArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "N");
        }

        public override bool TryGetParamDescription(string paramName, out string paramDescription)
        {
            Contracts.AssertNonEmpty(paramName);

            return StringResources.TryGet("AboutBooleanN_" + paramName, out paramDescription);
        }
    }

    // Boolean(arg:w)
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanWFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanWFunction()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBooleanW, FunctionCategories.Text, DType.Boolean, 0, 1, 1, DType.Decimal)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanWArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "W");
        }

        public override bool TryGetParamDescription(string paramName, out string paramDescription)
        {
            Contracts.AssertNonEmpty(paramName);

            return StringResources.TryGet("AboutBooleanW_" + paramName, out paramDescription);
        }
    }

    // Boolean(E:*[n])
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanNFunction_T : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanNFunction_T()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBooleanNT, FunctionCategories.Table, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanNTArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "N_T");
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            var arg = args[0];
            var argType = argTypes[0];
            fValid &= CheckNumericColumnType(argType, arg, errors, ref nodeToCoercedTypeMap);

            var rowType = DType.EmptyRecord.Add(new TypedName(DType.Boolean, ColumnName_Value));
            returnType = rowType.ToTable();

            return fValid;
        }

        public override bool TryGetParamDescription(string paramName, out string paramDescription)
        {
            Contracts.AssertNonEmpty(paramName);

            return StringResources.TryGet("AboutBooleanNT_" + paramName, out paramDescription);
        }
    }

    // Boolean(E:*[n])
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanWFunction_T : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanWFunction_T()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBooleanWT, FunctionCategories.Table, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanWTArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "W_T");
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            var arg = args[0];
            var argType = argTypes[0];
            fValid &= CheckNumericColumnType(argType, arg, errors, ref nodeToCoercedTypeMap);

            var rowType = DType.EmptyRecord.Add(new TypedName(DType.Boolean, ColumnName_Value));
            returnType = rowType.ToTable();

            return fValid;
        }

        public override bool TryGetParamDescription(string paramName, out string paramDescription)
        {
            Contracts.AssertNonEmpty(paramName);

            return StringResources.TryGet("AboutBooleanWT_" + paramName, out paramDescription);
        }
    }

    // Boolean(arg:b)
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanBFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanBFunction()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBooleanN, FunctionCategories.Text, DType.Boolean, 0, 1, 1, DType.Boolean)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanArg1 };
        }

        /// <summary>
        /// If arg's result type is Boolean, no need to make a function call to Boolean() function. It can just emit arg directly.
        /// </summary>
        internal override IR.Nodes.IntermediateNode CreateIRCallNode(PowerFx.Syntax.CallNode node, IRTranslator.IRTranslatorContext context, List<IntermediateNode> args, ScopeSymbol scope)
        {
            if (args[0].IRContext.ResultType._type == DType.Boolean)
            {
                return args[0];
            }
            else
            {
                return base.CreateIRCallNode(node, context, args, scope);
            }
        }
    }

    // Boolean(E:*[b])
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanBFunction_T : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanBFunction_T()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBooleanBT, FunctionCategories.Table, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanNTArg1 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            var arg = args[0];
            var argType = argTypes[0];
            fValid &= CheckBooleanColumnType(argType, arg, errors, ref nodeToCoercedTypeMap);

            var rowType = DType.EmptyRecord.Add(new TypedName(DType.Boolean, ColumnName_Value));
            returnType = rowType.ToTable();

            return fValid;
        }

        public override bool TryGetParamDescription(string paramName, out string paramDescription)
        {
            Contracts.AssertNonEmpty(paramName);

            return StringResources.TryGet("AboutBooleanBT_" + paramName, out paramDescription);
        }

        /// <summary>
        /// If arg is Table of boolean, no need to make a function call to boolean function. It can just emit the table 
        /// arg directly.
        /// </summary>
        internal override IR.Nodes.IntermediateNode CreateIRCallNode(PowerFx.Syntax.CallNode node, IRTranslator.IRTranslatorContext context, List<IntermediateNode> args, ScopeSymbol scope)
        {
            var child = args[0];
            var rowType = DType.EmptyRecord.Add(new TypedName(DType.Boolean, ColumnName_Value));
            var booleanTReturnType = rowType.ToTable();
            if (child.IRContext.ResultType._type == booleanTReturnType)
            {
                return child;
            }
            else
            {
                return base.CreateIRCallNode(node, context, args, scope);
            }
        }
    }

    // Boolean(arg:O)
    internal sealed class BooleanFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanFunction_UO()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBoolean, FunctionCategories.Text, DType.Boolean, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
