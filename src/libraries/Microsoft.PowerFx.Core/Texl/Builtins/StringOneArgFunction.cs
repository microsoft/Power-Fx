// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal abstract class StringOneArgFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public StringOneArgFunction(string name, TexlStrings.StringGetter description, FunctionCategories functionCategories)
            : base(name, description, functionCategories, DType.String, 0, 1, 1, DType.String)
        {
        }

        public StringOneArgFunction(string name, TexlStrings.StringGetter description, FunctionCategories functionCategories, DType returnType)
            : base(name, description, functionCategories, returnType, 0, 1, 1, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.StringFuncArg1 };
        }

        public override bool IsRowScopedServerDelegatable(CallNode callNode, TexlBinding binding, OperationCapabilityMetadata metadata)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);
            Contracts.AssertValue(metadata);

            if (FunctionDelegationCapability.Capabilities == DelegationCapability.None ||
                binding.ErrorContainer.HasErrors(callNode) ||
                !CheckArgsCount(callNode, binding) ||
                !binding.IsRowScope(callNode))
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();
            var argKind = args[0].VerifyValue().Kind;

            switch (argKind)
            {
                case NodeKind.FirstName:
                    {
                        var firstNameStrategy = GetFirstNameNodeDelegationStrategy();
                        return firstNameStrategy.IsValidFirstNameNode(args[0].AsFirstName(), binding, null);
                    }

                case NodeKind.Call:
                    {
                        if (!metadata.IsDelegationSupportedByTable(FunctionDelegationCapability))
                        {
                            return false;
                        }

                        var cNodeStrategy = GetCallNodeDelegationStrategy();
                        return cNodeStrategy.IsValidCallNode(args[0].AsCall(), binding, metadata);
                    }

                case NodeKind.DottedName:
                    {
                        var dottedStrategy = GetDottedNameNodeDelegationStrategy();
                        return dottedStrategy.IsValidDottedNameNode(args[0].AsDottedName(), binding, metadata, null);
                    }

                default:
                    break;
            }

            return false;
        }
    }

    internal abstract class StringOneArgTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public StringOneArgTableFunction(string name, TexlStrings.StringGetter description, FunctionCategories functionCategories)
            : base(name, description, functionCategories, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.StringTFuncArg1 };
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

            // Typecheck the input table
            fValid &= CheckStringColumnType(argTypes[0], args[0], errors, ref nodeToCoercedTypeMap);

            if (nodeToCoercedTypeMap?.Any() ?? false)
            {
                // Now set the coerced type to a table with numeric column type with the same name as in the argument.
                returnType = nodeToCoercedTypeMap[args[0]];
            }
            else
            {
                returnType = context.Features.HasFlag(Features.ConsistentOneColumnTableResult)
                ? DType.CreateTable(new TypedName(DType.String, new DName(ColumnName_ValueStr)))
                : argTypes[0];
            }

            return fValid;
        }
    }
}
