// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Internal adapter for adding custom functions. 
    /// </summary>
    internal class CustomTexlFunction : TexlFunction
    {
        public Func<IServiceProvider, FormulaValue[], CancellationToken, Task<FormulaValue>> _impl;

        internal BigInteger LamdaParamMask;

        private readonly string[] _argNames;

        public override bool IsSelfContained => !_isBehavior;

        private readonly bool _isBehavior;

        public CustomTexlFunction(string name, FunctionCategories functionCategory, FormulaType returnType, string[] argNames, params FormulaType[] paramTypes)
            : this(name, functionCategory, returnType._type, argNames, Array.ConvertAll(paramTypes, x => x._type))
        {
        }

        public CustomTexlFunction(string name, FunctionCategories functionCategory, DType returnType, string[] argNames, params DType[] paramTypes)
            : base(DPath.Root, name, name, CustomFunctionUtility.SG("Custom func " + name), functionCategory, returnType, 0, paramTypes.Length, paramTypes.Length, paramTypes)
        {
            _isBehavior = functionCategory == FunctionCategories.Behavior;
            _argNames = argNames;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return CustomFunctionUtility.GenerateArgSignature(_argNames, ParamTypes);
        }

        public virtual Task<FormulaValue> InvokeAsync(IServiceProvider serviceProvider, FormulaValue[] args, CancellationToken cancellationToken)
        {
            return _impl(serviceProvider, args, cancellationToken);
        }

        public override bool IsLazyEvalParam(int index)
        {
            return LamdaParamMask.TestBit(index);
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            var isValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            // Check if all record names of args exist against arg types and if its possible to coerce.
            if (isValid)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    DType curType = argTypes[i]; // caller
                    DType paramType = this.ParamTypes[i]; // callsite 

                    if (curType.IsRecord || curType.IsTable)
                    {
                        // If param type is an empty record, then don't enforce. 
                        if (paramType.ChildCount == 0)
                        {
                            continue;
                        }

                        if (!curType.CheckAggregateNames(paramType, args[i], errors, SupportCoercionForArg(i), context.Features.PowerFxV1CompatibilityRules))
                        {
                            return false;
                        }
                    }
                }
            }

            return isValid;
        }
    }
}
