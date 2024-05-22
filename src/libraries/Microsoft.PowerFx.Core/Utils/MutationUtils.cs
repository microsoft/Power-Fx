// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using CallNode = Microsoft.PowerFx.Syntax.CallNode;
using RecordNode = Microsoft.PowerFx.Core.IR.Nodes.RecordNode;

namespace Microsoft.PowerFx.Core.Utils
{
    internal class MutationUtils
    {
        public static void CheckForReadOnlyFields(DType dataSourceType, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            if (dataSourceType.AssociatedDataSources.Any())
            {
                var tableDsInfo = dataSourceType.AssociatedDataSources.Single();

                if (tableDsInfo is IExternalCdsDataSource cdsTableInfo)
                {
                    for (int i = 0; i < argTypes.Length; i++)
                    {
                        if (!cdsTableInfo.IsArgTypeValidForMutation(argTypes[i], out var invalidFieldNames))
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrRecordContainsInvalidFields_Arg, string.Join(", ", invalidFieldNames));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds specific errors for mutation functions.
        /// </summary>
        public static void CheckSemantics(TexlBinding binding, TexlFunction function, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            var targetArg = args[0];

            // control!property are not valid targets for Collect, Remove, etc.
            DottedNameNode dotted;
            if ((dotted = targetArg.AsDottedName()) != null
                && binding.TryCastToFirstName(dotted.Left, out var firstNameInfo)
                && firstNameInfo.Kind == BindKind.Control)
            {
                errors.EnsureError(targetArg, TexlStrings.ErrInvalidArgs_Func, function.Name);
                return;
            }

            // Checks for something similar to Collect(x.a, 4).
            if (!binding.TryCastToFirstName(targetArg, out firstNameInfo))
            {
                return;
            }

            if (firstNameInfo.Data is IExternalDataSource info && !info.IsWritable)
            {
                errors.EnsureError(targetArg, TexlStrings.ErrInvalidArgs_Func, function.Name);
                return;
            }
        }

        public static string GetScalarSingleColumnNameForType(Features features, DKind kind)
        {
            return kind switch
            {
                DKind.Image or
                DKind.Hyperlink or
                DKind.Media or
                DKind.Blob or
                DKind.PenImage => features.ConsistentOneColumnTableResult ? TableValue.ValueName : "Url",

                _ => TableValue.ValueName
            };
        }

        public static List<IntermediateNode> CreateIRCallNodeCollect(CallNode node, IRTranslator.IRTranslatorContext context, List<IntermediateNode> args, ScopeSymbol scope)
        {
            var newArgs = new List<IntermediateNode>() { args[0] };

            foreach (var arg in args.Skip(1))
            {
                if (arg.IRContext.ResultType._type.IsPrimitive)
                {
                    newArgs.Add(
                        new RecordNode(
                            new IRContext(arg.IRContext.SourceContext, RecordType.Empty().Add(TableValue.ValueName, arg.IRContext.ResultType)),
                            new Dictionary<DName, IntermediateNode>
                            {
                                { TableValue.ValueDName, arg }
                            }));
                }
                else
                {
                    newArgs.Add(arg);
                }
            }

            return newArgs;
        }
    }
}
