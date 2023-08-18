// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    internal class FunctionDescr
    {
        internal string Name { get; }

        internal MethodInfo Method { get; }

        internal string[] ArgNames
        {
            get
            {
                var paramNames = Method.GetParameters().Select(p => p.Name);

                // Config is always First arg, and user doesn't provide it.
                if (ConfigType != null)
                {
                    paramNames = paramNames.Skip(1);
                }

                return paramNames.ToArray();
            }
        }

        internal FormulaType RetType { get; }

        // User-facing parameter types. 
        internal FormulaType[] ParamTypes { get; }

        // If not null, then arg0 is from RuntimeConfig
        internal Type ConfigType { get; }

        internal bool IsAsync { get; }

        internal BigInteger LamdaParamMask { get; }

        public FunctionDescr(string name, MethodInfo method, FormulaType retType, FormulaType[] paramTypes, Type configType, BigInteger lamdaParamMask, bool isAsync = false)
        {
            Name = name;
            Method = method;
            RetType = retType;
            ParamTypes = paramTypes;
            ConfigType = configType;
            IsAsync = isAsync;
            LamdaParamMask = lamdaParamMask;
        }
    }
}
