// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Logging
{    
    public sealed class SimpleAnonymizer : TexlVisitor
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
            if (node.Head.Namespace.IsRoot && !BuiltinFunctionsCore.IsKnownPublicFunction(node.Head.Token.ToString()))
            {
                ReplaceSpan(node.Head.Span, 'c', (c, _, _) => true);
            }

            return true;
        }

        public override void Visit(FirstNameNode node)
        {
        }

        public override void Visit(ErrorNode node)
        {
        }

        public override void Visit(BlankNode node)
        {
        }

        public override void Visit(BoolLitNode node)
        {
        }

        public override void Visit(ParentNode node)
        {
        }

        public override void Visit(SelfNode node)
        {
        }

        public override void Visit(TypeLiteralNode node)
        {
        }

        public override void PostVisit(StrInterpNode node)
        {
        }

        public override void PostVisit(DottedNameNode node)
        {
        }

        public override void PostVisit(UnaryOpNode node)
        {
        }

        public override void PostVisit(BinaryOpNode node)
        {
        }

        public override void PostVisit(VariadicOpNode node)
        {
        }

        public override void PostVisit(CallNode node)
        {
        }

        public override void PostVisit(ListNode node)
        {
        }

        public override void PostVisit(RecordNode node)
        {
        }

        public override void PostVisit(TableNode node)
        {
        }

        public override void PostVisit(AsNode node)
        {
        }
    }
}
