// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Logging
{
    /// <summary>
    /// Simple anonymizer for Power Fx expressions.
    /// It will replace:
    /// - string content with 'x' characters
    /// - numbers digits with '0' characters
    /// - decimal digits with '1' characters
    /// - unknown function names with 'c' characters, including ReflectionFunctions.
    /// </summary>
    public sealed class SimpleAnonymizer : IdentityTexlVisitor
    {
        private readonly char[] _expression;

        private SimpleAnonymizer(string expression)
        {
            _expression = expression.ToCharArray();
        }

        public static string GetAnonymousExpression(ParseResult parse)
        {
            SimpleAnonymizer anonymizer = new SimpleAnonymizer(parse.Text);
            parse.Root.Accept(anonymizer);
            return new string(anonymizer._expression);
        }

        public override void Visit(StrLitNode node)
        {
            // replace string content but keep double quotes at beginning and end
            ReplaceSpan(node, 'x', (c, i, s) => !(c == '"' && (i == s.Min || i == s.Lim - 1)));
        }

        private void ReplaceSpan(TexlNode node, char replacement, Func<char, int, Span, bool> condition)
        {
            ReplaceSpan(node.GetTextSpan(), replacement, condition);
        }

        private void ReplaceSpan(Span span, char replacement, Func<char, int, Span, bool> condition)
        {
            for (int i = span.Min; i < span.Lim; i++)
            {
                char c = _expression[i];

                if (condition(c, i, span))
                {
                    _expression[i] = replacement;
                }
            }
        }

        public override void Visit(NumLitNode node)
        {
            // replace digits with 0 (keep E or -)
            ReplaceSpan(node, '0', (c, _, _) => c >= '0' && c <= '9');
        }

        public override void Visit(DecLitNode node)
        {
            // replace digits with 1 (keep E or -)
            ReplaceSpan(node, '1', (c, _, _) => c >= '0' && c <= '9');
        }

        public override bool PreVisit(CallNode node)
        {
            // ReflectionFunction functions will be renamed
            if (node.Head.Namespace.IsRoot && !BuiltinFunctionsCore.IsKnownPublicFunction(node.Head.Token.ToString()))
            {
                ReplaceSpan(node.Head.Span, 'c', (c, _, _) => true);
            }

            return true;
        }
    }
}
