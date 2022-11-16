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

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // With(scope: object, fn: function)
    internal sealed class WithFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public WithFunction()
            : base("With", TexlStrings.AboutWith, FunctionCategories.Table, DType.Unknown, 0x2, 2, 2, DType.EmptyRecord)
        {
            ScopeInfo = new FunctionScopeInfo(this, iteratesOverScope: false);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.WithArg1, TexlStrings.WithArg2 };
        }

        // `with` is a keyword in javascript so the typescript function name is suffixed with `_R`
        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_R");
        }

        protected override bool CheckTypes(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            // Base call yields unknown return type, so we set it accordingly below
            var fArgsValid = base.CheckTypes(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            // Return type determined by second argument (function)
            // Since CheckTypes is called on partial functions, return type should be error when a second argument is undefined
            if (argTypes.Length >= 2)
            {
                returnType = argTypes[1];
            }
            else
            {
                returnType = DType.Error;
            }

            return fArgsValid;
        }

        /// <summary>
        /// With function has special syntax where datasource can be provided as scope parameter argument.
        /// </summary>
        /// <param name="node"></param>
        /// <returns>TexlNode for argument that can be used to determine tabular datasource.</returns>
        public override IEnumerable<TexlNode> GetTabularDataSourceArg(CallNode node)
        {
            Contracts.AssertValue(node);

            var dsArg = node.Args.Children[0];

            if (dsArg is VariadicBase variadicBaseDSArg)
            {
                return variadicBaseDSArg.Children;
            }

            return new[] { dsArg };
        }
    }
}
