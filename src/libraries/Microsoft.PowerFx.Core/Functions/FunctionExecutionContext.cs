// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Functions
{
    internal class FunctionExecutionContext
    {
        internal FunctionExecutionContext(FormulaValue[] arguments, TimeZoneInfo timeZoneInfo = null, CultureInfo cultureInfo = null, FormulaType returnType = null)
        {
            Arguments = arguments;
            TimeZoneInfo = timeZoneInfo;
            CultureInfo = cultureInfo;
            ReturnType = returnType;
        }

        internal FormulaValue[] Arguments { get; }
        
        internal TimeZoneInfo TimeZoneInfo { get; }
        
        internal CultureInfo CultureInfo { get; }
        
        internal FormulaType ReturnType { get; }
    }
}
