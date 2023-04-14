// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx
{
    // Helper for SetPropertyFunction 
    // Binds as: 
    //    SetProperty(control.property:, arg:any)
    // Invokes as:
    //    SetProperty(control, "property", arg)
    internal sealed class CustomSetPropertyFunction : TexlFunction
    {
        public override bool IsAsync => true;

        public override bool IsSelfContained => false; // marks as behavior 

        public override bool SupportsParamCoercion => false;

        public Func<FormulaValue[], Task<FormulaValue>> _impl;

        private readonly IEnumerable<CustomFunctionSignatureHelper> _argumentSignatures;

        public CustomSetPropertyFunction(string name)
            : this(name, null)
        {
        }

        public CustomSetPropertyFunction(string name, IEnumerable<CustomFunctionSignatureHelper> argumentSignatures)
            : base(DPath.Root, name, name, SG(name), FunctionCategories.Behavior, DType.Boolean, 0, 2, 2)
        {
            _argumentSignatures = argumentSignatures;
            if (_argumentSignatures == null)
            {
                _argumentSignatures = new CustomFunctionSignatureHelper[] { new CustomFunctionSignatureHelper("Arg 1", "Arg 2") };
            }
        }

        private static StringGetter SG(string text) => CustomTexlFunction.SG(text);

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            foreach (var signature in CustomTexlFunction.GetCustomSignatures(_argumentSignatures))
            {
                yield return signature;
            }
        }

        // 2nd argument should be same type as 1st argument. 
        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.AssertAllValid(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            nodeToCoercedTypeMap = null;
            returnType = DType.Boolean;

            var arg0 = argTypes[0];

            var dottedName = args[0].AsDottedName();

            // Global-scoped variable name should be a firstName.
            if (dottedName == null)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedValidVariableName_Arg, Name, args[0]);
                return false;
            }

            var arg1 = argTypes[1];

            if (!arg0.Accepts(arg1))
            {
                errors.EnsureError(DocumentErrorSeverity.Critical, args[1], ErrBadType);
                return false;
            }

            return true;
        }

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            var result = _impl(args);
            return await result.ConfigureAwait(false);
        }
    }
}
