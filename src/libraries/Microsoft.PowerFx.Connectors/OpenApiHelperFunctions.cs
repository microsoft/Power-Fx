// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.PowerFx.Connectors
{
    internal class OpenApiHelperFunctions
    {
        private static readonly Regex NotAnXsdNCNameCharRegex = new Regex(@"[^a-zA-Z0-9_-]+", RegexOptions.Compiled);
        private static readonly Regex XsdNCNameStartCharRegex = new Regex(@"^[a-zA-Z_]", RegexOptions.Compiled);

        internal static string NormalizeOperationId(string operationId)
        {
            return MakeValidXsdNCName(Regex.Replace(operationId, @"[^A-Za-z0-9]", string.Empty));
        }
        
        private static string MakeValidXsdNCName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            name = NotAnXsdNCNameCharRegex.Replace(name, EncodeInvalidNCNameChars);

            // Handle the start char to make it is a valid start char.
            // If the first char is not a valid start char, just prepend a valid char.
            if (!XsdNCNameStartCharRegex.IsMatch(name))
            {
                name = "_" + name;
            }

            return name;
        }

        private static string EncodeInvalidNCNameChars(Match match)
        {
            var sb = new StringBuilder();
            
            // The period char '.' is commonly-used in the swagger. To reduce the size of
            // the generated WADL, we directly map it to the underscore. For all other invalid chars,
            // use 2-digit hex ("X2") if char can fit. Otherwise, use 4-digit hex ("X4")
            foreach (var c in match.Value)
            {
                if (c == '.')
                {
                    sb.Append("_");
                }
                else if (c <= 255)
                {
                    sb.Append("_ux" + ((int)c).ToString("X2"));
                }
                else
                {
                    sb.Append("_Ux" + ((int)c).ToString("X4"));
                }
            }

            // End the encoding with an underscore character to make the resulting encoding more 
            // readable (i.e. "!?.$" -> "_ux21_ux3F__ux24_"
            if (sb[sb.Length - 1] != '_')
            {
                sb.Append("_");
            }

            return sb.ToString();
        }
    }
}
