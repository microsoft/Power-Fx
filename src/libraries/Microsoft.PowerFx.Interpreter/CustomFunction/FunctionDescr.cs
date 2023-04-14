// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    internal class FunctionDescr
    {
        internal string Name { get; }

        internal MethodInfo Method { get; }

        internal FormulaType RetType { get; }

        // User-facing parameter types. 
        internal FormulaType[] ParamTypes { get; }

        // If not null, then arg0 is from RuntimeConfig
        internal Type ConfigType { get; }

        internal bool IsAsync { get; }

        internal BigInteger LamdaParamMask { get; }

        internal IEnumerable<CustomFunctionSignatureHelper> ArgumentSignatures { get; }

        public FunctionDescr(string name, MethodInfo method, FormulaType retType, FormulaType[] paramTypes, Type configType, BigInteger lamdaParamMask, IEnumerable<CustomFunctionSignatureHelper> argumentSignatures, bool isAsync = false)
        {
            Name = name;
            Method = method;
            RetType = retType;
            ParamTypes = paramTypes;
            ConfigType = configType;
            IsAsync = isAsync;
            LamdaParamMask = lamdaParamMask;
            ArgumentSignatures = argumentSignatures;
        }
    }
}
