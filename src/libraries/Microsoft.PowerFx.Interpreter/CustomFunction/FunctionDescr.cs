// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.PowerFx.Core.Utils;
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
                if (IsConfigArgPresent)
                {
                    paramNames = paramNames.Skip(1);
                }

                return paramNames.ToArray();
            }
        }

        internal FormulaType RetType { get; }

        // User-facing parameter types. 
        internal FormulaType[] ParamTypes { get; }

        // If true, then arg0 is from RuntimeConfig, this can't be made readonly since ConfigType is a protected field.
        internal bool IsConfigArgPresent { get; set; }

        internal bool IsAsync { get; }

        internal BigInteger LamdaParamMask { get; }

        internal DPath NameSpace { get; }

        public FunctionDescr(DPath ns, string name, MethodInfo method, FormulaType retType, FormulaType[] paramTypes, BigInteger lamdaParamMask, bool isAsync = false)
        {
            NameSpace = ns;
            Name = name;
            Method = method;
            RetType = retType;
            ParamTypes = paramTypes;
            IsAsync = isAsync;
            LamdaParamMask = lamdaParamMask;
        }
    }
}
