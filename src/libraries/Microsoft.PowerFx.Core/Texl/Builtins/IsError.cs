// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // IsError(value: any)
    internal sealed class IsErrorFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;
        public override bool SupportsParamCoercion => false;

        public IsErrorFunction()
            : base("IsError", TexlStrings.AboutIsError, FunctionCategories.Logical, DType.Boolean, 0, 1, 1)
        { }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsErrorArg };
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length == 1);
            Contracts.AssertValue(errors);
            
            nodeToCoercedTypeMap = null;

            var type = ReturnType;

            Contracts.Assert(ReturnType == DType.Boolean);

            returnType = ReturnType;
            return true;
        }
    }
}
