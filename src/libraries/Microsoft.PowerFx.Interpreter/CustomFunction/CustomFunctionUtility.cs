// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx
{
    internal class CustomFunctionUtility
    {
        public static TexlStrings.StringGetter[] GenerateArgSignature(DType[] paramTypes, IEnumerable<CustomFunctionSignatureHelper> argumentSignatures = null)
        {
            var paramCount = paramTypes.Length;
            var signature = new StringGetter[paramCount];

            var customSignature = argumentSignatures?.GetEnumerator() ?? Enumerable.Empty<CustomFunctionSignatureHelper>().GetEnumerator();

            for (var i = 0; i < paramCount; i++)
            {
                if (customSignature.MoveNext())
                {
                    signature[i] = SG($"{customSignature.Current.ArgLabel[i]} : {paramTypes[i].GetKindString()}");
                }
                else
                {
                    signature[i] = SG($"Arg{i + 1} : {paramTypes[i].GetKindString()}");
                }
            }

            return signature;        
        }

        public static StringGetter SG(string text)
        {
            return (string locale) => text;
        }

        private static IEnumerable<TexlStrings.StringGetter[]> GetCustomSignatures(IEnumerable<CustomFunctionSignatureHelper> argumentSignatures)
        {
            if (argumentSignatures == null)
            {
                yield break;
            }

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
    }
}
