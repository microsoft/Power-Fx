// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx
{
    internal class CustomFunctionUtility
    {
        public static TexlStrings.StringGetter[] GenerateArgSignature(string[] argNames, DType[] paramTypes)
        {
            var count = paramTypes?.Length ?? 0;
            var signature = new StringGetter[count];

            for (var i = 0; i < count; i++)
            {
                var argName = i < argNames.Length ? argNames[i] : $"Arg{i + 1}";
                signature[i] = SG($"{argName} : {paramTypes[i].GetKindString()}");
            }

            return signature;
        }

        public static StringGetter SG(string text)
        {
            return (string locale) => text;
        }
    }
}
