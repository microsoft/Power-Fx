// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    public static class LexicalHelpers
    {
        /// <summary>
        /// Convert a literal format to a Power Fx expression. 
        /// 
        /// </summary>
        /// <param name="literal"></param>
        /// <returns></returns>
        public static string LiteralToExpression(string literal)
        {
            if (string.IsNullOrWhiteSpace(literal))
            {
                return null;
            }

            char first = literal[0];

            // Equals sign means the rest is an expression
            if (first == '=')
            {
                return literal.Substring(1);
            }

            // Tick means the rest is a string literal
            if (first == '\'')
            {
                var str = literal.Substring(1);

                var expr = FormulaValue.New(str).ToExpression();
                return expr;
            }

            // Boolean literals
            if (literal.Equals("true", StringComparison.OrdinalIgnoreCase))                
            {
                return "true";
            }

            if (literal.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return "false";
            }

            if (first != ' ')
            {
                // Numerical literals
                if (decimal.TryParse(literal, out var decimalValue))
                {
                    var str = DecLitToken.ToString(decimalValue);
                    return str;
                }
            }

            // Everything else is an interpolated string. 
            {
                var expr = "$\"" + StrLitToken.EscapeString(literal) + "\"";
                return expr;
            }
        }
    }
}
