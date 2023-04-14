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
    internal class CustomTexlFunction : TexlFunction
    {
        public Func<IServiceProvider, FormulaValue[], CancellationToken, Task<FormulaValue>> _impl;

        internal BigInteger LamdaParamMask;

        private readonly IEnumerable<CustomFunctionSignatureHelper> _argumentSignatures;

        public CustomTexlFunction(string name, FormulaType returnType, params FormulaType[] paramTypes)
            : this(name, returnType._type, null, Array.ConvertAll(paramTypes, x => x._type))
        {
        }

        public CustomTexlFunction(string name, FormulaType returnType, IEnumerable<CustomFunctionSignatureHelper> argumentSignatures, params FormulaType[] paramTypes)
            : this(name, returnType._type, argumentSignatures, Array.ConvertAll(paramTypes, x => x._type))
        {
        }

        public CustomTexlFunction(string name, DType returnType, params DType[] paramTypes)
            : this(name, returnType, null, paramTypes)
        {
        }

        public CustomTexlFunction(string name, DType returnType, IEnumerable<CustomFunctionSignatureHelper> argumentSignatures, params DType[] paramTypes)
            : base(DPath.Root, name, name, SG("Custom func " + name), FunctionCategories.MathAndStat, returnType, 0, paramTypes.Length, paramTypes.Length, paramTypes)
        {
            _argumentSignatures = argumentSignatures;

            if (_argumentSignatures == null)
            {
                _argumentSignatures = new CustomFunctionSignatureHelper[] { new CustomFunctionSignatureHelper("Arg 1") };
            }
        }

        public override bool IsSelfContained => true;

        public static StringGetter SG(string text)
        {
            return (string locale) => text;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            foreach (var signature in GetCustomSignatures(_argumentSignatures))
            {
                yield return signature;
            }
        }

        internal static IEnumerable<TexlStrings.StringGetter[]> GetCustomSignatures(IEnumerable<CustomFunctionSignatureHelper> argumentSignatures)
        {
            foreach (var signature in argumentSignatures)
            {
                TexlStrings.StringGetter[] sign = new StringGetter[signature.Count];

                for (var i = 0; i < signature.Count; i++)
                {
                    sign[i] = SG(signature.ArgLabel[i]);
                }

                yield return sign;
            }
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
