// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Public.Config;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx.Functions
{
    internal class PowerFxFunctionPtr<T> : PowerFxFunctionPtr
        where T : BuiltinFunction
    {
        public PowerFxFunctionPtr(AsyncFunctionPtr asyncFunctionPtr)
        {
            AsyncFunctionPtr = asyncFunctionPtr;
        }
    }

    internal class PowerFxFunctionPtr
    {
        internal AsyncFunctionPtr AsyncFunctionPtr;
    }

    internal static class BasicServiceProviderExtentions
    {
        public static void AddFunction(this IBasicServiceProvider serviceProvider, Type functionType, AsyncFunctionPtr functionPtr)
        {
            Type pfxFuncPtrType = typeof(PowerFxFunctionPtr<>).MakeGenericType(functionType);
            object pfxFuncPtr = pfxFuncPtrType.GetConstructors().First().Invoke(new object[] { functionPtr });

            serviceProvider.AddService(pfxFuncPtrType, pfxFuncPtr);            
        }
    }    
}
