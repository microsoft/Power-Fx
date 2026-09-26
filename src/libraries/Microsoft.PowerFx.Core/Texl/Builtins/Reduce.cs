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
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.IR.IRTranslator;
using IRCallNode = Microsoft.PowerFx.Core.IR.Nodes.CallNode;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Reduce(source:*, formula, initialValue)
    internal sealed class ReduceFunction : FunctionWithTableInput
    {
        public const string ReduceInvariantFunctionName = "Reduce";

        public override bool SkipScopeForInlineRecords => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public ReduceFunction()
            : base(ReduceInvariantFunctionName, TexlStrings.AboutReduce, FunctionCategories.Table, DType.Unknown, 0x2, 2, 3, DType.EmptyTable)
        {
            ScopeInfo = new FunctionReduceScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ReduceArg1, TexlStrings.ReduceArg2 };
            yield return new[] { TexlStrings.ReduceArg1, TexlStrings.ReduceArg2, TexlStrings.ReduceArg3 };
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
            var fArgsValid = CheckType(context, args[0], argTypes[0], ParamTypes[0], errors, ref nodeToCoercedTypeMap);

            if (args.Length > 2)
            {
                // When InitialValue is provided, its type determines the return type.
                returnType = argTypes[2];

                // If InitialValue is ObjNull (e.g. Blank()), fall back to formula type.
                if (returnType == DType.ObjNull)
                {
                    returnType = argTypes[1];
                }

                // Validate that the formula result type is compatible with the InitialValue type.
                // Skip if either is ObjNull (unknown/unresolved type).
                if (argTypes[1] != DType.ObjNull && argTypes[2] != DType.ObjNull)
                {
                    fArgsValid &= CheckType(context, args[1], argTypes[1], argTypes[2], errors, ref nodeToCoercedTypeMap);
                }
            }
            else
            {
                // No InitialValue provided - return type is the formula type.
                returnType = argTypes[1];
            }

            // If the return type is still ObjNull, the type could not be inferred.
            if (returnType == DType.ObjNull)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrReduceUndeterminedType);
                fArgsValid = false;
            }

            return fArgsValid;
        }

        internal override IntermediateNode CreateIRCallNode(PowerFx.Syntax.CallNode node, IRTranslatorContext context, List<IntermediateNode> args, ScopeSymbol scope)
        {
            // Determine the reduce field name from the AST (default "ThisReduce" or As-renamed)
            var reduceName = FunctionReduceScopeInfo.GetReduceName(node.Args.Children.ToArray());

            var newArgs = new List<IntermediateNode>(args);

            // Insert the reduce field name as a text literal after the lambda (at index 2).
            // This allows the runtime to know what field name to use for the accumulator.
            var reduceNameNode = new TextLiteralNode(IRContext.NotInSource(FormulaType.String), reduceName.Value);
            newArgs.Insert(2, reduceNameNode);

            return new IRCallNode(context.GetIRContext(node), this, scope, newArgs);
        }

        public override bool HasSuggestionsForParam(int index)
        {
            Contracts.Assert(index >= 0);

            return index == 0;
        }
    }
}
