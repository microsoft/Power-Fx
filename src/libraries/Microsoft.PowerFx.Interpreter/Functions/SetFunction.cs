// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Interpreter
{
    // Implementation of a Set function which just chains to 
    // RecalcEngine.UpdateVariable().
    // Set has no return value. 
    // Whereas PowerApps' Set() will implicitly define arg0,
    //  this Set() requires arg0 was already defined and has a type.
    //
    // Called as:
    //   Set(var,newValue)
    internal class RecalcEngineSetFunction : TexlFunction
    {
        // Set() is a behavior function. 
        public override bool IsSelfContained => false;

        public override IEnumerable<StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.SetArg1, TexlStrings.SetArg2 };
        }

        public RecalcEngineSetFunction()
        : base(
              DPath.Root,
              "Set",
              "Set",
              TexlStrings.AboutSet,
              FunctionCategories.Behavior,
              DType.Boolean,
              0, // no lambdas
              2,
              2)
        {
        }

        // 2nd argument should be same type as 1st argument. 
        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);

            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.AssertAllValid(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var arg0 = argTypes[0];

            var firstName = args[0].AsFirstName();

            if (firstName != null)
            {
                var info = binding.GetInfo(firstName);
                if (info.Data is NameSymbol nameSymbol && nameSymbol.IsMutable)
                {
                    // We have a variable. type check
                    var arg1 = argTypes[1];

                    if (!arg0.Accepts(arg1))
                    {
                        errors.EnsureError(DocumentErrorSeverity.Critical, args[1], ErrBadType);
                        return;
                    }

                    // Success
                    return;
                }
            }

            errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedValidVariableName_Arg, Name, args[0]);
            return;
        }
    }
}
