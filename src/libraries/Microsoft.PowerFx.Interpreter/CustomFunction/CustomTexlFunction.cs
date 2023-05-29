// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Internal adapter for adding custom functions. 
    /// </summary>
    [ThreadSafeImmutable]
    internal class CustomTexlFunction : TexlFunction
    {
        public readonly Func<IServiceProvider, FormulaValue[], CancellationToken, Task<FormulaValue>> _impl;

        internal readonly BigInteger LamdaParamMask;

        public CustomTexlFunction(string name, FormulaType returnType, params FormulaType[] paramTypes)
            : this(name, returnType._type, Array.ConvertAll(paramTypes, x => x._type))
        {
        }

        public CustomTexlFunction(string name, FormulaType returnType, Func<IServiceProvider, FormulaValue[], CancellationToken, Task<FormulaValue>> impl, BigInteger lamdaParamMask, params FormulaType[] paramTypes)
            : this(name, returnType._type, Array.ConvertAll(paramTypes, x => x._type))
        {
            _impl = impl;
            LamdaParamMask = lamdaParamMask;
        }

        public CustomTexlFunction(string name, DType returnType, params DType[] paramTypes)
            : base(DPath.Root, name, name, CustomFunctionUtility.SG("Custom func " + name), FunctionCategories.MathAndStat, returnType, 0, paramTypes.Length, paramTypes.Length, paramTypes)
        {
        }

        public override bool IsSelfContained => true;

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return CustomFunctionUtility.GenerateArgSignature(ParamTypes);
        }

        public virtual Task<FormulaValue> InvokeAsync(IServiceProvider serviceProvider, FormulaValue[] args, CancellationToken cancellationToken)
        {
            return _impl(serviceProvider, args, cancellationToken);
        }

        public override bool IsLazyEvalParam(int index)
        {
            return LamdaParamMask.TestBit(index);
        }
    }
}
