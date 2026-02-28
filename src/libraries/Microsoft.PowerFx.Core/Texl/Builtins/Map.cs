// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.IR.IRTranslator;
using CallNode = Microsoft.PowerFx.Syntax.CallNode;
using IRCallNode = Microsoft.PowerFx.Core.IR.Nodes.CallNode;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Map(source:*, formula)
    // Map(source1 As A, source2 As B, ..., formula)
    // Map(source1 As A, source2 As B, ..., formula, MapLength.Equal)
    internal sealed class MapFunction : FunctionWithTableInput
    {
        public const string MapInvariantFunctionName = "Map";

        public override bool SkipScopeForInlineRecords => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public MapFunction()
            : base(MapInvariantFunctionName, TexlStrings.AboutMap, FunctionCategories.Table, DType.Unknown, 0x2, 2, int.MaxValue, DType.EmptyTable)
        {
            ScopeInfo = new FunctionMapScopeInfo(this);
        }

        /// <summary>
        /// Count consecutive AsNode args from index 0 to determine the number of table args.
        /// 0 or 1 → single-table mode (return 1). 2+ → multi-table mode (return that count).
        /// </summary>
        private static int GetTableCount(CallNode node)
        {
            if (node == null)
            {
                return 1;
            }

            var asCount = 0;
            foreach (var child in node.Args.Children)
            {
                if (child is AsNode)
                {
                    asCount++;
                }
                else
                {
                    break;
                }
            }

            return asCount >= 2 ? asCount : 1;
        }

        public override int GetScopeArgs(CallNode node)
        {
            return GetTableCount(node);
        }

        public override bool IsLambdaParam(TexlNode node, int index)
        {
            // Navigate to parent CallNode to compute table count
            if (node?.Parent?.Parent is CallNode callNode)
            {
                var tableCount = GetTableCount(callNode);
                return index == tableCount;
            }

            // Fallback: default single-table mode, lambda is at index 1
            return index == 1;
        }

        public override bool IsLazyEvalParam(TexlNode node, int index, Features features)
        {
            return IsLambdaParam(node, index);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MapArg1, TexlStrings.MapArg2 };
            yield return new[] { TexlStrings.MapArg1, TexlStrings.MapArg1, TexlStrings.MapArg2 };
            yield return new[] { TexlStrings.MapArg1, TexlStrings.MapArg1, TexlStrings.MapArg2, TexlStrings.MapArg3 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 4)
            {
                return GetGenericSignatures(arity, TexlStrings.MapArg1, TexlStrings.MapArg2);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.AssertAllValues(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            nodeToCoercedTypeMap = null;

            // Count consecutive AsNode args from the start
            var asCount = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is AsNode)
                {
                    asCount++;
                }
                else
                {
                    break;
                }
            }

            var isMultiTable = asCount >= 2;
            var tableCount = isMultiTable ? asCount : 1;

            // Lambda is at index tableCount
            var lambdaIndex = tableCount;

            bool fArgsValid;

            if (!isMultiTable)
            {
                // Single-table mode: original behavior
                fArgsValid = CheckType(context, args[0], argTypes[0], ParamTypes[0], errors, ref nodeToCoercedTypeMap);

                // Detect multi-table intent without proper As: first arg uses As but there are
                // extra args and the second arg looks like a table (not a lambda expression).
                if (asCount == 1 && args.Length > 2 && (argTypes[1].IsTable || argTypes[1].IsRecord))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrMapMultiTableRequiresAs);
                    returnType = DType.Error;
                    fArgsValid = false;
                }
                else if (lambdaIndex < argTypes.Length)
                {
                    var lambdaType = argTypes[lambdaIndex];
                    returnType = ComputeReturnType(lambdaType, args[lambdaIndex], errors, out var valid);
                    fArgsValid &= valid;
                }
                else
                {
                    returnType = DType.Error;
                    fArgsValid = false;
                }

                // MapLength is only valid with multiple tables
                if (args.Length > lambdaIndex + 1)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[lambdaIndex + 1], TexlStrings.ErrMapMapLengthRequiresMultiTable);
                    fArgsValid = false;
                }
            }
            else
            {
                // Multi-table mode
                fArgsValid = true;

                // Validate all table args are tables and all use As
                for (int i = 0; i < tableCount; i++)
                {
                    if (!(args[i] is AsNode))
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrMapMultiTableRequiresAs);
                        fArgsValid = false;
                    }

                    if (!argTypes[i].IsTable && !argTypes[i].IsRecord)
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrNeedTable_Func, Name);
                        fArgsValid = false;
                    }
                }

                // Lambda return type determines result type
                if (lambdaIndex < argTypes.Length)
                {
                    var lambdaType = argTypes[lambdaIndex];
                    returnType = ComputeReturnType(lambdaType, args[lambdaIndex], errors, out var valid);
                    fArgsValid &= valid;
                }
                else
                {
                    returnType = DType.Error;
                    fArgsValid = false;
                }

                // Check optional MapLength arg at end
                if (args.Length > lambdaIndex + 1)
                {
                    fArgsValid &= ValidateMapLengthArg(context, args, argTypes, lambdaIndex + 1, errors);
                }
            }

            return fArgsValid;
        }

        private DType ComputeReturnType(DType lambdaType, TexlNode lambdaArg, IErrorContainer errors, out bool valid)
        {
            valid = true;

            if (lambdaType.IsRecord)
            {
                return lambdaType.ToTable();
            }
            else if (lambdaType.IsPrimitive || lambdaType.IsTable || lambdaType.IsUntypedObject)
            {
                return DType.CreateTable(new TypedName(lambdaType, ColumnName_Value));
            }
            else if (lambdaType.IsVoid)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, lambdaArg, TexlStrings.ErrBadType_VoidExpression, Name);
                valid = false;
                return DType.Error;
            }
            else
            {
                valid = false;
                return DType.Error;
            }
        }

        private bool ValidateMapLengthArg(CheckTypesContext context, TexlNode[] args, DType[] argTypes, int mapLengthIndex, IErrorContainer errors)
        {
            if (mapLengthIndex >= args.Length)
            {
                return true;
            }

            var expectedType = context.Features.StronglyTypedBuiltinEnums
                ? BuiltInEnums.MapLengthEnum.FormulaType._type
                : DType.String;

            if (!expectedType.Accepts(argTypes[mapLengthIndex], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules) ||
                args[mapLengthIndex] is not DottedNameNode)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[mapLengthIndex], TexlStrings.ErrMapInvalidMapLengthArg);
                return false;
            }

            return true;
        }

        public override bool HasSuggestionsForParam(int index)
        {
            Contracts.Assert(index >= 0);

            return index == 0;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            CheckLambdaForSideEffects(binding, args, errors);
        }

        /// <summary>
        /// Map requires a pure (non-side-effectful) lambda expression.
        /// PostVisitValidation catches cases where CheckTypes fails (e.g. Void return)
        /// but we still want to report the behavior function error.
        /// </summary>
        public override bool PostVisitValidation(TexlBinding binding, CallNode callNode)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(callNode);

            return CheckLambdaForSideEffects(binding, callNode.Args.Children.ToArray(), binding.ErrorContainer);
        }

        private static bool CheckLambdaForSideEffects(TexlBinding binding, TexlNode[] args, IErrorContainer errors)
        {
            var tableCount = CountTableArgs(args);
            var lambdaIndex = tableCount;

            if (lambdaIndex < args.Length && binding.HasSideEffects(args[lambdaIndex]))
            {
                errors.EnsureError(args[lambdaIndex], TexlStrings.ErrMapFunctionRequiresPureLambda);
                return true;
            }

            return false;
        }

        private static int CountTableArgs(TexlNode[] args)
        {
            var asCount = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is AsNode)
                {
                    asCount++;
                }
                else
                {
                    break;
                }
            }

            return asCount >= 2 ? asCount : 1;
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { LanguageConstants.MapLengthEnumString };
        }

        internal override IntermediateNode CreateIRCallNode(PowerFx.Syntax.CallNode node, IRTranslatorContext context, List<IntermediateNode> args, ScopeSymbol scope)
        {
            var tableCount = GetTableCount(node);

            // Single-table mode: delegate to base
            if (tableCount <= 1)
            {
                return base.CreateIRCallNode(node, context, args, scope);
            }

            // Multi-table mode: inject scope name resolver record and default MapLength
            var carg = node.Args.Count;
            var lambdaIndex = tableCount;
            var newArgs = new List<IntermediateNode>();

            // Add table args and lambda
            for (int i = 0; i <= lambdaIndex && i < args.Count; i++)
            {
                newArgs.Add(args[i]);
            }

            // Add scope names as ordered text literals (one per table, matching table order)
            ScopeInfo.GetScopeIdent(node.Args.Children.ToArray(), out var scopeIdent);

            for (int i = 0; i < tableCount; i++)
            {
                newArgs.Add(new TextLiteralNode(IRContext.NotInSource(FormulaType.String), scopeIdent[i].Value));
            }

            // Add MapLength string value
            var mapLengthValue = "equal"; // default
            var mapLengthArgIndex = lambdaIndex + 1;
            if (mapLengthArgIndex < carg && node.Args.Children[mapLengthArgIndex] is DottedNameNode dottedName)
            {
                mapLengthValue = dottedName.Right.Name.Value.ToLowerInvariant();
            }

            newArgs.Add(new TextLiteralNode(IRContext.NotInSource(FormulaType.String), mapLengthValue));

            return new IRCallNode(context.GetIRContext(node), this, scope, newArgs);
        }
    }

#if true
    internal sealed class MapFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public MapFunction_UO()
            : base(MapFunction.MapInvariantFunctionName, TexlStrings.AboutMap, FunctionCategories.Table, DType.Unknown, 0x2, 2, int.MaxValue, DType.UntypedObject)
        {
            ScopeInfo = new FunctionMapScopeInfo(this);
        }

        /// <summary>
        /// Count consecutive AsNode args from index 0 to determine the number of table args.
        /// 0 or 1 → single-table mode (return 1). 2+ → multi-table mode (return that count).
        /// </summary>
        private static int GetTableCount(CallNode node)
        {
            if (node == null)
            {
                return 1;
            }

            var asCount = 0;
            foreach (var child in node.Args.Children)
            {
                if (child is AsNode)
                {
                    asCount++;
                }
                else
                {
                    break;
                }
            }

            return asCount >= 2 ? asCount : 1;
        }

        public override int GetScopeArgs(CallNode node)
        {
            return GetTableCount(node);
        }

        public override bool IsLambdaParam(TexlNode node, int index)
        {
            if (node?.Parent?.Parent is CallNode callNode)
            {
                var tableCount = GetTableCount(callNode);
                return index == tableCount;
            }

            return index == 1;
        }

        public override bool IsLazyEvalParam(TexlNode node, int index, Features features)
        {
            return IsLambdaParam(node, index);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MapArg1, TexlStrings.MapArg2 };
            yield return new[] { TexlStrings.MapArg1, TexlStrings.MapArg1, TexlStrings.MapArg2 };
            yield return new[] { TexlStrings.MapArg1, TexlStrings.MapArg1, TexlStrings.MapArg2, TexlStrings.MapArg3 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 4)
            {
                return GetGenericSignatures(arity, TexlStrings.MapArg1, TexlStrings.MapArg2);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.AssertAllValues(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            nodeToCoercedTypeMap = null;

            // Count consecutive AsNode args from the start
            var asCount = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is AsNode)
                {
                    asCount++;
                }
                else
                {
                    break;
                }
            }

            var isMultiTable = asCount >= 2;
            var tableCount = isMultiTable ? asCount : 1;
            var lambdaIndex = tableCount;

            bool fArgsValid;

            if (!isMultiTable)
            {
                // Single-table mode: original behavior
                fArgsValid = CheckType(context, args[0], argTypes[0], ParamTypes[0], errors, ref nodeToCoercedTypeMap);

                // Detect multi-table intent without proper As
                if (asCount == 1 && args.Length > 2 && argTypes[1].IsUntypedObject)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrMapMultiTableRequiresAs);
                    returnType = DType.Error;
                    fArgsValid = false;
                }
                else
                {
                    if (lambdaIndex < argTypes.Length)
                    {
                        var lambdaType = argTypes[lambdaIndex];
                        returnType = ComputeReturnType(lambdaType, args[lambdaIndex], errors, out var valid);
                        fArgsValid &= valid;
                    }
                    else
                    {
                        returnType = DType.Error;
                        fArgsValid = false;
                    }

                    // MapLength is only valid with multiple tables
                    if (args.Length > lambdaIndex + 1)
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[lambdaIndex + 1], TexlStrings.ErrMapMapLengthRequiresMultiTable);
                        fArgsValid = false;
                    }
                }
            }
            else
            {
                // Multi-table mode
                fArgsValid = true;

                // Validate all table args are UntypedObject and all use As
                for (int i = 0; i < tableCount; i++)
                {
                    if (!(args[i] is AsNode))
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrMapMultiTableRequiresAs);
                        fArgsValid = false;
                    }

                    if (!argTypes[i].IsUntypedObject)
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrNeedTable_Func, Name);
                        fArgsValid = false;
                    }
                }

                // Lambda return type determines result type
                if (lambdaIndex < argTypes.Length)
                {
                    var lambdaType = argTypes[lambdaIndex];
                    returnType = ComputeReturnType(lambdaType, args[lambdaIndex], errors, out var valid);
                    fArgsValid &= valid;
                }
                else
                {
                    returnType = DType.Error;
                    fArgsValid = false;
                }

                // Check optional MapLength arg at end
                if (args.Length > lambdaIndex + 1)
                {
                    fArgsValid &= ValidateMapLengthArg(context, args, argTypes, lambdaIndex + 1, errors);
                }
            }

            return fArgsValid;
        }

        private DType ComputeReturnType(DType lambdaType, TexlNode lambdaArg, IErrorContainer errors, out bool valid)
        {
            valid = true;

            if (lambdaType.IsRecord)
            {
                return lambdaType.ToTable();
            }
            else if (lambdaType.IsPrimitive || lambdaType.IsTable || lambdaType.IsUntypedObject)
            {
                return DType.CreateTable(new TypedName(lambdaType, ColumnName_Value));
            }
            else if (lambdaType.IsVoid)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, lambdaArg, TexlStrings.ErrBadType_VoidExpression, Name);
                valid = false;
                return DType.Error;
            }
            else
            {
                valid = false;
                return DType.Error;
            }
        }

        private bool ValidateMapLengthArg(CheckTypesContext context, TexlNode[] args, DType[] argTypes, int mapLengthIndex, IErrorContainer errors)
        {
            if (mapLengthIndex >= args.Length)
            {
                return true;
            }

            var expectedType = context.Features.StronglyTypedBuiltinEnums
                ? BuiltInEnums.MapLengthEnum.FormulaType._type
                : DType.String;

            if (!expectedType.Accepts(argTypes[mapLengthIndex], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules) ||
                args[mapLengthIndex] is not DottedNameNode)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[mapLengthIndex], TexlStrings.ErrMapInvalidMapLengthArg);
                return false;
            }

            return true;
        }

        public override bool HasSuggestionsForParam(int index)
        {
            Contracts.Assert(index >= 0);

            return index == 0;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            CheckLambdaForSideEffects(binding, args, errors);
        }

        /// <summary>
        /// PostVisitValidation catches cases where CheckTypes fails (e.g. Void return)
        /// but we still want to report the behavior function error.
        /// </summary>
        public override bool PostVisitValidation(TexlBinding binding, CallNode callNode)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(callNode);

            return CheckLambdaForSideEffects(binding, callNode.Args.Children.ToArray(), binding.ErrorContainer);
        }

        private static bool CheckLambdaForSideEffects(TexlBinding binding, TexlNode[] args, IErrorContainer errors)
        {
            var tableCount = CountTableArgs(args);
            var lambdaIndex = tableCount;

            if (lambdaIndex < args.Length && binding.HasSideEffects(args[lambdaIndex]))
            {
                errors.EnsureError(args[lambdaIndex], TexlStrings.ErrMapFunctionRequiresPureLambda);
                return true;
            }

            return false;
        }

        private static int CountTableArgs(TexlNode[] args)
        {
            var asCount = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is AsNode)
                {
                    asCount++;
                }
                else
                {
                    break;
                }
            }

            return asCount >= 2 ? asCount : 1;
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { LanguageConstants.MapLengthEnumString };
        }

        internal override IntermediateNode CreateIRCallNode(PowerFx.Syntax.CallNode node, IRTranslatorContext context, List<IntermediateNode> args, ScopeSymbol scope)
        {
            var tableCount = GetTableCount(node);

            // Single-table mode: delegate to base
            if (tableCount <= 1)
            {
                return base.CreateIRCallNode(node, context, args, scope);
            }

            // Multi-table mode: inject scope name resolver record and default MapLength
            var carg = node.Args.Count;
            var lambdaIndex = tableCount;
            var newArgs = new List<IntermediateNode>();

            // Add table args and lambda
            for (int i = 0; i <= lambdaIndex && i < args.Count; i++)
            {
                newArgs.Add(args[i]);
            }

            // Add scope names as ordered text literals (one per table, matching table order)
            ScopeInfo.GetScopeIdent(node.Args.Children.ToArray(), out var scopeIdent);

            for (int i = 0; i < tableCount; i++)
            {
                newArgs.Add(new TextLiteralNode(IRContext.NotInSource(FormulaType.String), scopeIdent[i].Value));
            }

            // Add MapLength string value
            var mapLengthValue = "equal"; // default
            var mapLengthArgIndex = lambdaIndex + 1;
            if (mapLengthArgIndex < carg && node.Args.Children[mapLengthArgIndex] is DottedNameNode dottedName)
            {
                mapLengthValue = dottedName.Right.Name.Value.ToLowerInvariant();
            }

            newArgs.Add(new TextLiteralNode(IRContext.NotInSource(FormulaType.String), mapLengthValue));

            return new IRCallNode(context.GetIRContext(node), this, scope, newArgs);
        }
    }
#endif
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
