// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Logging
{
    // A visitor that provides PII-free unformatted prints of powerapps formulas.
    internal sealed class StructuralPrint : TexlFunctionalVisitor<LazyList<string>, Precedence>
    {
        private readonly TexlBinding _binding;
        private readonly ISanitizedNameProvider _nameProvider;

        private StructuralPrint(TexlBinding binding = null, ISanitizedNameProvider nameProvider = null)
        {
            _binding = binding;
            _nameProvider = nameProvider;
        }

        // Public entry point for prettyprinting TEXL parse trees
        public static string Print(TexlNode node, TexlBinding binding = null, ISanitizedNameProvider nameProvider = null)
        {
            Contracts.AssertValue(node);

            var pretty = new StructuralPrint(binding, nameProvider);
            return string.Concat(node.Accept(pretty, Precedence.None));
        }
        
        public override LazyList<string> Visit(ErrorNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of("#$error$#");
        }

        public override LazyList<string> Visit(BlankNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of("Blank()");
        }

        public override LazyList<string> Visit(BoolLitNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of("#$boolean$#");
        }

        public override LazyList<string> Visit(StrLitNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of("#$string$#");
        }

        public override LazyList<string> Visit(NumLitNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            var nlt = node.Value;
            return LazyList<string>.Of("#$number$#");
        }

        public override LazyList<string> Visit(DecLitNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            var nlt = node.Value;
            return LazyList<string>.Of("#$decimal$#");
        }

        public override LazyList<string> Visit(FirstNameNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            if (_nameProvider != null && _nameProvider.TrySanitizeIdentifier(node.Ident, out var sanitizedName))
            {
                return LazyList<string>.Of(sanitizedName);
            }

            var info = _binding?.GetInfo(node);
            if (info != null && info.Kind != BindKind.Unknown)
            {
                return LazyList<string>.Of($"#${Enum.GetName(typeof(BindKind), info.Kind)}$#");
            }

            if (node.Ident.AtToken == null)
            {
                return LazyList<string>.Of("#$firstname$#");
            }
            else
            {
                return LazyList<string>.Of("#$disambiguation$#");
            }
        }

        public override LazyList<string> Visit(ParentNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of(TexlLexer.KeywordParent);
        }

        public override LazyList<string> Visit(SelfNode node, Precedence context)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of(TexlLexer.KeywordSelf);
        }

        public override LazyList<string> Visit(DottedNameNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            var separator = TexlParser.GetTokString(node.Token.Kind);

            var values = node.Left.Accept(this, Precedence.Primary);
            values = values.With(separator);
            if (node.Right.AtToken != null || node.UsesBracket)
            {
                values = values.With("#$disambiguation$#");
            }
            else
            {
                if (node.RightNode != null)
                {
                    values = values.With(node.RightNode?.Accept(this, parentPrecedence));
                }
                else
                {
                    if (_nameProvider != null && _nameProvider.TrySanitizeIdentifier(node.Right, out var sanitizedName, node))
                    {
                        values = values.With(sanitizedName);
                    }
                    else
                    {
                        values = values.With("#$righthandid$#");
                    }
                }
            }

            return ApplyPrecedence(parentPrecedence, Precedence.Primary, values);
        }

        public override LazyList<string> Visit(AsNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            return ApplyPrecedence(
                parentPrecedence,
                Precedence.As,
                node.Left.Accept(this, Precedence.As)
                    .With(SpacedOper(TexlLexer.KeywordAs))
                    .With("#$righthandid$#"));
        }

        public override LazyList<string> Visit(UnaryOpNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            var child = node.Child.Accept(this, Precedence.PrefixUnary);

            LazyList<string> result;
            switch (node.Op)
            {
                case UnaryOp.Not:
                    if (node.Token.Kind == TokKind.KeyNot)
                    {
                        result = LazyList<string>.Of(TexlLexer.KeywordNot, " ").With(child);
                    }
                    else
                    {
                        result = LazyList<string>.Of(TexlLexer.PunctuatorBang).With(child);
                    }

                    break;
                case UnaryOp.Minus:
                    result = LazyList<string>.Of(TexlLexer.PunctuatorSub).With(child);
                    break;
                case UnaryOp.Percent:
                    result = LazyList<string>.Of(child).With(TexlLexer.PunctuatorPercent);
                    break;
                default:
                    Contracts.Assert(false);
                    result = LazyList<string>.Of("#$error$#").With(child);
                    break;
            }

            return ApplyPrecedence(parentPrecedence, Precedence.PrefixUnary, result);
        }

        public override LazyList<string> Visit(BinaryOpNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            switch (node.Op)
            {
                case BinaryOp.Or:
                    if (node.Token.Kind == TokKind.KeyOr)
                    {
                        return PrettyBinary(SpacedOper(TexlLexer.KeywordOr), parentPrecedence, Precedence.Or, node.Left, node.Right);
                    }
                    else
                    {
                        return PrettyBinary(SpacedOper(TexlLexer.PunctuatorOr), parentPrecedence, Precedence.Or, node.Left, node.Right);
                    }

                case BinaryOp.And:
                    if (node.Token.Kind == TokKind.KeyAnd)
                    {
                        return PrettyBinary(SpacedOper(TexlLexer.KeywordAnd), parentPrecedence, Precedence.And, node.Left, node.Right);
                    }
                    else
                    {
                        return PrettyBinary(SpacedOper(TexlLexer.PunctuatorAnd), parentPrecedence, Precedence.And, node.Left, node.Right);
                    }

                case BinaryOp.Concat:
                    return PrettyBinary(SpacedOper(TexlLexer.PunctuatorAmpersand), parentPrecedence, Precedence.Concat, node.Left, node.Right);
                case BinaryOp.Add:
                    return PrettyBinary(SpacedOper(TexlLexer.PunctuatorAdd), parentPrecedence, Precedence.Add, node.Left, node.Right);
                case BinaryOp.Mul:
                    return PrettyBinary(SpacedOper(TexlLexer.PunctuatorMul), parentPrecedence, Precedence.Mul, node.Left, node.Right);
                case BinaryOp.Div:
                    return PrettyBinary(SpacedOper(TexlLexer.PunctuatorDiv), parentPrecedence, Precedence.Mul, node.Left, node.Right);
                case BinaryOp.In:
                    return PrettyBinary(SpacedOper(TexlLexer.KeywordIn), parentPrecedence, Precedence.In, node.Left, node.Right);
                case BinaryOp.Exactin:
                    return PrettyBinary(SpacedOper(TexlLexer.KeywordExactin), parentPrecedence, Precedence.In, node.Left, node.Right);
                case BinaryOp.Power:
                    return PrettyBinary(TexlLexer.PunctuatorCaret, parentPrecedence, Precedence.Power, Precedence.PrefixUnary, node.Left, node.Right);
                case BinaryOp.Error:
                    return PrettyBinary("#$error$#", parentPrecedence, Precedence.Error, node.Left, node.Right);

                case BinaryOp.Equal:
                    return PrettyBinary(SpacedOper(TexlLexer.PunctuatorEqual), parentPrecedence, Precedence.Compare, node.Left, node.Right);
                case BinaryOp.NotEqual:
                    return PrettyBinary(SpacedOper(TexlLexer.PunctuatorNotEqual), parentPrecedence, Precedence.Compare, node.Left, node.Right);
                case BinaryOp.Less:
                    return PrettyBinary(SpacedOper(TexlLexer.PunctuatorLess), parentPrecedence, Precedence.Compare, node.Left, node.Right);
                case BinaryOp.LessEqual:
                    return PrettyBinary(SpacedOper(TexlLexer.PunctuatorLessOrEqual), parentPrecedence, Precedence.Compare, node.Left, node.Right);
                case BinaryOp.Greater:
                    return PrettyBinary(SpacedOper(TexlLexer.PunctuatorGreater), parentPrecedence, Precedence.Compare, node.Left, node.Right);
                case BinaryOp.GreaterEqual:
                    return PrettyBinary(SpacedOper(TexlLexer.PunctuatorGreaterOrEqual), parentPrecedence, Precedence.Compare, node.Left, node.Right);

                default:
                    Contracts.Assert(false);
                    return PrettyBinary("#$error$#", parentPrecedence, Precedence.Atomic + 1, node.Left, node.Right);
            }
        }

        public override LazyList<string> Visit(VariadicOpNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            Contracts.AssertNonEmpty(node.Children);

            switch (node.Op)
            {
                case VariadicOp.Chain:
                    var count = node.Count;
                    var result = LazyList<string>.Empty;

                    for (var i = 0; i < count; i++)
                    {
                        result = result
                            .With(node.Children[i].Accept(this, Precedence.None));
                        if (i != count - 1)
                        {
                            result = result.With(SpacedOper(TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorChainingSeparator));
                        }
                    }

                    return result;
                default:
                    Contracts.Assert(false);
                    return LazyList<string>.Of("#$error$#");
            }
        }

        public override LazyList<string> Visit(StrInterpNode node, Precedence context)
        {
            Contracts.AssertValue(node);

            var count = node.Count;
            var result = LazyList<string>.Empty.With("$\"");

            for (var i = 0; i < count; i++)
            {
                if (node.Children[i].Kind == NodeKind.StrLit)
                {
                    result = result
                        .With(node.Children[i].Accept(this, context));
                }
                else
                {
                    result = result
                        .With("{")
                        .With(node.Children[i].Accept(this, context))
                        .With("}");
                }
            }

            return result.With("\"");
        }

        public override LazyList<string> Visit(CallNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            var result = LazyList<string>.Empty;
            if (!node.Head.Namespace.IsRoot)
            {
                result = result.With(
                    "#$servicefunction#$",
                    TexlLexer.PunctuatorDot);
            }
            else
            {
                result = result
                    .With(
                        node.Head.Token.ToString(),
                        TexlLexer.PunctuatorParenOpen)
                    .With(node.Args.Accept(this, Precedence.Primary))
                    .With(TexlLexer.PunctuatorParenClose);
            }

            return ApplyPrecedence(parentPrecedence, Precedence.Primary, result);
        }

        public override LazyList<string> Visit(ListNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            var listSep = TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorListSeparator + " ";
            var result = LazyList<string>.Empty;
            for (var i = 0; i < node.Children.Length; ++i)
            {
                result = result
                    .With(node.Children[i].Accept(this, Precedence.None));
                if (i != node.Children.Length - 1)
                {
                    result = result.With(listSep);
                }
            }

            return result;
        }

        public override LazyList<string> Visit(RecordNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            var listSep = TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorListSeparator + " ";
            var result = LazyList<string>.Empty;
            for (var i = 0; i < node.Children.Length; ++i)
            {
                result = result
                    .With(
                        "#$fieldname$#",
                        TexlLexer.PunctuatorColon)
                    .With(node.Children[i].Accept(this, Precedence.SingleExpr));
                if (i != node.Children.Length - 1)
                {
                    result = result.With(listSep);
                }
            }

            result =
                LazyList<string>.Of(TexlLexer.PunctuatorCurlyOpen, " ")
                .With(result)
                .With(" ", TexlLexer.PunctuatorCurlyClose);

            if (node.SourceRestriction != null)
            {
                result = LazyList<string>.Of(TexlLexer.PunctuatorAt).With(result);
            }

            return ApplyPrecedence(parentPrecedence, Precedence.SingleExpr, result);
        }

        public override LazyList<string> Visit(TableNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            var listSep = TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorListSeparator + " ";
            var result = LazyList<string>.Empty;
            for (var i = 0; i < node.Children.Length; ++i)
            {
                result = result.With(node.Children[i].Accept(this, Precedence.SingleExpr));
                if (i != node.Children.Length - 1)
                {
                    result = result.With(listSep);
                }
            }

            result = LazyList<string>.Of(TexlLexer.PunctuatorBracketOpen, " ")
                .With(result)
                .With(" ", TexlLexer.PunctuatorBracketClose);

            return ApplyPrecedence(parentPrecedence, Precedence.SingleExpr, result);
        }

        private LazyList<string> ApplyPrecedence(Precedence parentPrecedence, Precedence precedence, LazyList<string> strings)
        {
            if (parentPrecedence > precedence)
            {
                var result = LazyList<string>.Of(TexlLexer.PunctuatorParenOpen);
                result = result.With(strings);
                result = result.With(TexlLexer.PunctuatorParenClose);
                return result;
            }

            return strings;
        }

        // For left associative operators: precRight == precLeft + 1.
        private LazyList<string> PrettyBinary(string strOp, Precedence parentPrecedence, Precedence precLeft, TexlNode left, TexlNode right)
        {
            return PrettyBinary(strOp, parentPrecedence, precLeft, precLeft + 1, left, right);
        }

        private LazyList<string> PrettyBinary(string strOp, Precedence parentPrecedence, Precedence precLeft, Precedence precRight, TexlNode left, TexlNode right)
        {
            Contracts.AssertNonEmpty(strOp);

            return ApplyPrecedence(
                parentPrecedence,
                precLeft,
                left.Accept(this, precLeft)
                    .With(strOp)
                    .With(right.Accept(this, precRight)));
        }

        private string SpacedOper(string op)
        {
            Contracts.AssertNonEmpty(op);

            return " " + op + " ";
        }
    }
}
