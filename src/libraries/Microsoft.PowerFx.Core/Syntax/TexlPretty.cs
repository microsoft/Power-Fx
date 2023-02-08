// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Syntax.SourceInformation;

namespace Microsoft.PowerFx.Syntax
{
    // Simple pretty-printing visitor (Task #2489649
    // Todo: currently being used by node.ToString().  Need to figure
    // out if it is being used with anything else.  If not, we can delete
    // this entirely
    internal class TexlPretty : TexlFunctionalVisitor<LazyList<string>, Precedence>
    {
        internal TexlPretty()
        {
        }

        // Public entry point for prettyprinting TEXL parse trees
        public static string PrettyPrint(TexlNode node)
        {
            Contracts.AssertValue(node);

            var pretty = new TexlPretty();
            return string.Concat(node.Accept(pretty, Precedence.None));
        }

        public override LazyList<string> Visit(ErrorNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of("<error>");
        }

        public override LazyList<string> Visit(BlankNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Empty;
        }

        public override LazyList<string> Visit(BoolLitNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of(
                node.Value ? TexlLexer.KeywordTrue : TexlLexer.KeywordFalse);
        }

        public override LazyList<string> Visit(StrLitNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of(
                "\"",
                CharacterUtils.ExcelEscapeString(node.Value),
                "\"");
        }

        public override LazyList<string> Visit(NumLitNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            var nlt = node.Value;
            return LazyList<string>.Of(nlt != null ? nlt.ToString() : node.NumValue.ToString("R", CultureInfo.CurrentCulture));
        }

        public override LazyList<string> Visit(DecLitNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            var nlt = node.Value;
            return LazyList<string>.Of(nlt != null ? nlt.ToString() : node.DecValue.ToString("G29", CultureInfo.CurrentCulture));
        }

        public override LazyList<string> Visit(FirstNameNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            if (node.Ident.AtToken == null)
            {
                return LazyList<string>.Of(node.Ident.Token.ToString());
            }
            else
            {
                return LazyList<string>.Of(
                    TexlLexer.PunctuatorBracketOpen,
                    TexlLexer.PunctuatorAt,
                    node.Ident.Token.ToString(),
                    TexlLexer.PunctuatorBracketClose);
            }
        }

        public override LazyList<string> Visit(ParentNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of(TexlLexer.KeywordParent);
        }

        public override LazyList<string> Visit(SelfNode node, Precedence parentPrecedence)
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
            if (node.Right.AtToken != null)
            {
                values = values.With(TexlLexer.PunctuatorAt);
            }

            values = values.With(GetRightToken(node.Left, node.Right));
            if (node.UsesBracket)
            {
                values = values.With(TexlLexer.PunctuatorBracketClose);
            }

            return ApplyPrecedence(parentPrecedence, Precedence.Primary, values);
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
                    result = LazyList<string>.Of("<error>").With(child);
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
                    return PrettyBinary(" <error> ", parentPrecedence, Precedence.Error, node.Left, node.Right);

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
                    return PrettyBinary(" <error> ", parentPrecedence, Precedence.Atomic + 1, node.Left, node.Right);
            }
        }

        public override LazyList<string> Visit(AsNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            return ApplyPrecedence(
                parentPrecedence,
                Precedence.As,
                node.Left.Accept(this, Precedence.As)
                    .With(SpacedOper(TexlLexer.KeywordAs))
                    .With(node.Right.Token.ToString()));
        }

        public override LazyList<string> Visit(VariadicOpNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);
            Contracts.AssertNonEmpty(node.Children);

            switch (node.Op)
            {
                case VariadicOp.Chain:
                    var op = SpacedOper(TexlLexer.GetLocalizedInstance(CultureInfo.CurrentCulture).LocalizedPunctuatorChainingSeparator);
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
                    return LazyList<string>.Of("<error>");
            }
        }

        public override LazyList<string> Visit(StrInterpNode node, Precedence context)
        {
            Contracts.AssertValue(node);

            var count = node.Count;
            var result = LazyList<string>.Empty;

            result = result.With("$\"");
            for (var i = 0; i < count; i++)
            {
                if (node.Children[i].Kind == NodeKind.StrLit)
                {
                    Contracts.Assert(node.Children[i] is StrLitNode);

                    var strLit = node.Children[i] as StrLitNode;
                    result = result.With(CharacterUtils.ExcelEscapeString(strLit.Value, true));
                }
                else
                {
                    result = result
                        .With("{")
                        .With(node.Children[i].Accept(this, Precedence.None))
                        .With("}");
                }
            }

            result = result.With("\"");
            return result;
        }

        public override LazyList<string> Visit(CallNode node, Precedence parentPrecedence)
        {
            Contracts.AssertValue(node);

            var result = LazyList<string>.Empty;
            var sb = new StringBuilder();
            if (!node.Head.Namespace.IsRoot)
            {
                result = result.With(
                    node.Head.Namespace.ToDottedSyntax(),
                    TexlLexer.PunctuatorDot);
            }

            result = result
                .With(
                    node.Head.Token.ToString(),
                    TexlLexer.PunctuatorParenOpen)
                .With(node.Args.Accept(this, Precedence.Primary))
                .With(TexlLexer.PunctuatorParenClose);

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
                        node.Ids[i].Token.ToString(),
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

        public virtual string GetRightToken(TexlNode leftNode, Identifier right)
        {
            return right.Token.ToString();
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

    internal sealed class PrettyPrintVisitor : TexlFunctionalVisitor<LazyList<string>, PrettyPrintVisitor.Context>
    {
        private readonly string _script;

        private static readonly Dictionary<BinaryOp, Precedence> BinaryPrecedence =
            new Dictionary<BinaryOp, Precedence>()
            {
                { BinaryOp.Or, Precedence.Or },
                { BinaryOp.And, Precedence.And },
                { BinaryOp.Concat, Precedence.Concat },
                { BinaryOp.Add, Precedence.Add },
                { BinaryOp.Mul, Precedence.Mul },
                { BinaryOp.Div, Precedence.Mul },
                { BinaryOp.In, Precedence.In },
                { BinaryOp.Exactin, Precedence.In },
                { BinaryOp.Power, Precedence.Power },
                { BinaryOp.Error, Precedence.Error },
                { BinaryOp.Equal, Precedence.Compare },
                { BinaryOp.NotEqual, Precedence.Compare },
                { BinaryOp.Greater, Precedence.Compare },
                { BinaryOp.GreaterEqual, Precedence.Compare },
                { BinaryOp.Less, Precedence.Compare },
                { BinaryOp.LessEqual, Precedence.Compare },
            };

        private PrettyPrintVisitor(string script)
        {
            _script = script;
        }

        // Public entry point for prettyprinting TEXL parse trees
        public static string Format(TexlNode node, SourceList before, SourceList after, string script)
        {
            Contracts.AssertValue(node);

            var pretty = new PrettyPrintVisitor(script);
            var preRegex = string.Concat(pretty.CommentsOf(before)
                .With(node.Accept(pretty, new Context(0)))
                .With(pretty.CommentsOf(after)))
                .Replace("\n\n", "\n");

            return new Regex(@"\n +(\n +)").Replace(preRegex, (Match match) => match.Groups[1].Value);
        }

        private LazyList<string> CommentsOf(SourceList list)
        {
            if (list == null)
            {
                return LazyList<string>.Empty;
            }

            return LazyList<string>
                .Of(list.Sources
                    .OfType<TokenSource>()
                    .Where(token => token.Token.Kind == TokKind.Comment)
                    .Select(source => GetScriptForToken(source.Token)));
        }

        private LazyList<string> Basic(TexlNode node, Context context)
        {
            return LazyList<string>.Of(
                node.SourceList.Sources
                    .SelectMany(source =>
                    {
                        if (source is NodeSource nodeSource)
                        {
                            return nodeSource.Node.Accept(this, context);
                        }
                        else if (source is WhitespaceSource)
                        {
                            return LazyList<string>.Of(" ");
                        }
                        else
                        {
                            return source.Tokens.Select(GetScriptForToken);
                        }
                    }));
        }

        private LazyList<string> Single(TexlNode node)
        {
            return LazyList<string>.Of(
                node.SourceList.Tokens
                    .Where(token => token.Kind != TokKind.Whitespace)
                    .Select(GetScriptForToken));
        }

        public override LazyList<string> Visit(ErrorNode node, Context context)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of("<error>");
        }

        public override LazyList<string> Visit(BlankNode node, Context context)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Empty;
        }

        public override LazyList<string> Visit(BoolLitNode node, Context context)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of(
                node.Value ? TexlLexer.KeywordTrue : TexlLexer.KeywordFalse);
        }

        public override LazyList<string> Visit(StrLitNode node, Context context)
        {
            Contracts.AssertValue(node);
            return LazyList<string>.Of(
                "\"",
                CharacterUtils.ExcelEscapeString(node.Value),
                "\"");
        }

        public override LazyList<string> Visit(NumLitNode node, Context context)
        {
            Contracts.AssertValue(node);

            return Single(node);
        }

        public override LazyList<string> Visit(DecLitNode node, Context context)
        {
            Contracts.AssertValue(node);

            return Single(node);
        }

        public override LazyList<string> Visit(FirstNameNode node, Context context)
        {
            Contracts.AssertValue(node);

            if (node.Ident.AtToken == null)
            {
                return LazyList<string>.Of(node.Ident.Token.ToString());
            }
            else
            {
                return LazyList<string>.Of(
                    TexlLexer.PunctuatorBracketOpen,
                    TexlLexer.PunctuatorAt,
                    node.Ident.Token.ToString(),
                    TexlLexer.PunctuatorBracketClose);
            }
        }

        public override LazyList<string> Visit(ParentNode node, Context context)
        {
            Contracts.AssertValue(node);

            return LazyList<string>.Of(TexlLexer.KeywordParent);
        }

        public override LazyList<string> Visit(SelfNode node, Context context)
        {
            Contracts.AssertValue(node);

            return LazyList<string>.Of(TexlLexer.KeywordSelf);
        }

        public override LazyList<string> Visit(DottedNameNode node, Context context)
        {
            Contracts.AssertValue(node);

            return Basic(node, context);
        }

        public override LazyList<string> Visit(UnaryOpNode node, Context context)
        {
            Contracts.AssertValue(node);

            return Basic(node, context);
        }

        public override LazyList<string> Visit(BinaryOpNode node, Context context)
        {
            Contracts.AssertValue(node);

            if (node.Token.Kind == TokKind.PercentSign)
            {
                return Basic(node, context);
            }

            if (!BinaryPrecedence.TryGetValue(node.Op, out var precedence))
            {
                Contracts.Assert(false, "Couldn't find precedence for " + node.Op);
                precedence = Precedence.Error;
            }

            var builder = LazyList<string>.Empty;
            var firstNode = true;
            foreach (var source in node.SourceList.Sources.Where(source => !(source is WhitespaceSource)))
            {
                if (source is NodeSource leftOrRight)
                {
                    if (firstNode)
                    {
                        builder = builder
                            .With(leftOrRight.Node.Accept(this, context))
                            .With(" ");
                        firstNode = false;
                    }
                    else
                    {
                        builder = builder
                            .With(" ")
                            .With(leftOrRight.Node.Accept(this, context));
                    }
                }
                else
                {
                    builder = builder.With(source.Tokens.Select(GetScriptForToken));
                }
            }

            return builder;
        }

        public override LazyList<string> Visit(VariadicOpNode node, Context context)
        {
            Contracts.AssertValue(node);
            Contracts.AssertNonEmpty(node.Children);

            if (node.Op == VariadicOp.Chain)
            {
                var result = LazyList<string>.Empty;
                foreach (var source in node.SourceList.Sources.Where(source => !(source is WhitespaceSource)))
                {
                    if (source is NodeSource nodeSource)
                    {
                        result = result
                            .With(nodeSource.Node.Accept(this, context));
                    }
                    else if (source is TokenSource tokenSource && tokenSource.Token.Kind == TokKind.Semicolon)
                    {
                        result = result
                            .With(GetScriptForToken(tokenSource.Token))
                            .With(GetNewLine(context.IndentDepth + 1));
                    }
                    else
                    {
                        result = result.With(source.Tokens.Select(GetScriptForToken));
                    }
                }

                return result;
            }

            return Basic(node, context);
        }

        public override LazyList<string> Visit(StrInterpNode node, Context context)
        {
            Contracts.AssertValue(node);

            var result = LazyList<string>.Empty;
            var withinIsland = false;
            foreach (var source in node.SourceList.Sources.Where(source => !(source is WhitespaceSource)))
            {
                if (source is NodeSource nodeSource)
                {
                    if (withinIsland)
                    {
                        result = result.With(nodeSource.Node.Accept(this, context));
                    }
                    else if (nodeSource.Node.Kind == NodeKind.StrLit)
                    {
                        Contracts.Assert(nodeSource.Node is StrLitNode);

                        var strLitNode = nodeSource.Node as StrLitNode;
                        result = result
                            .With(CharacterUtils.ExcelEscapeString(strLitNode.Value, true));
                    }
                }
                else if (source is TokenSource tokenSource)
                {
                    if (tokenSource.Token.Kind == TokKind.StrLit)
                    {
                        Contracts.Assert(tokenSource.Token is StrLitToken);

                        var strLitToken = tokenSource.Token as StrLitToken;
                        result = result
                            .With(CharacterUtils.ExcelEscapeString(strLitToken.Value, true));
                    }
                    else if (tokenSource.Token.Kind == TokKind.IslandStart)
                    {
                        withinIsland = true;
                        result = result.With(source.Tokens.Select(GetScriptForToken));
                    }
                    else if (tokenSource.Token.Kind == TokKind.IslandEnd)
                    {
                        withinIsland = false;
                        result = result.With(source.Tokens.Select(GetScriptForToken));
                    }
                    else
                    {
                        result = result.With(source.Tokens.Select(GetScriptForToken));
                    }
                }
                else
                {
                    result = result.With(source.Tokens.Select(GetScriptForToken));
                }
            }

            return result;
        }

        public override LazyList<string> Visit(CallNode node, Context context)
        {
            Contracts.AssertValue(node);

            return Basic(node, context);
        }

        public override LazyList<string> Visit(ListNode node, Context context)
        {
            Contracts.AssertValue(node);

            // This must be precalculated, as if any generated argument contains a newline,
            // this should newline as well.
            context = context.Indent();
            var generatedNodes = PreGenerateNodes(context, node.SourceList, out var hasNewline);
            var useNewlines = node.Count > 1 || hasNewline;

            var result = LazyList<string>.Empty;
            foreach (var source in node.SourceList.Sources)
            {
                if (source is WhitespaceSource)
                {
                    continue;
                }

                var nodeSource = source as NodeSource;
                if (nodeSource != null && useNewlines)
                {
                    result = result
                        .With(GetNewLine(context.IndentDepth + 1))
                        .With(generatedNodes[nodeSource]);
                }
                else if (nodeSource != null)
                {
                    result = result.With(nodeSource.Node.Accept(this, context));
                }
                else if (source is TokenSource tokenSource && tokenSource.Token.Kind == TokKind.ParenClose && useNewlines)
                {
                    result = result
                        .With(GetNewLine(context.IndentDepth))
                        .With(GetScriptForToken(tokenSource.Token));
                }
                else
                {
                    result = result.With(source.Tokens.Select(GetScriptForToken));
                }
            }

            return result;
        }

        public override LazyList<string> Visit(RecordNode node, Context context)
        {
            Contracts.AssertValue(node);

            context = context.Indent();
            var generatedNodes = PreGenerateNodes(context, node.SourceList, out var hasNewline);
            var useNewlines = node.Count > 1 || hasNewline;

            var result = LazyList<string>.Empty;
            Token previousToken = null;
            foreach (var source in node.SourceList.Sources)
            {
                if (source is WhitespaceSource)
                {
                    continue;
                }

                var tokenSource = source as TokenSource;
                var commentToken = tokenSource?.Token as CommentToken;
                if (source is NodeSource nodeSource)
                {
                    result = result.With(generatedNodes[nodeSource]);
                }
                else if (tokenSource != null && tokenSource.Token.Kind == TokKind.Colon)
                {
                    result = result
                        .With(GetScriptForToken(tokenSource.Token))
                        .With(" ");
                }
                else if (source is IdentifierSource identifierSource)
                {
                    result = result.With(identifierSource.Tokens.Select(GetScriptForToken));
                }
                else if (tokenSource != null && tokenSource.Token.Kind == TokKind.CurlyClose && useNewlines)
                {
                    result = result
                        .With(GetNewLine(context.IndentDepth))
                        .With(GetScriptForToken(tokenSource.Token));
                }
                else if (tokenSource != null && (tokenSource.Token.Kind == TokKind.CurlyOpen || tokenSource.Token.Kind == TokKind.Comma) && useNewlines)
                {
                    result = result
                        .With(GetScriptForToken(tokenSource.Token))
                        .With(GetNewLine(context.IndentDepth + 1));
                }
                else if (commentToken != null && (previousToken?.Kind == TokKind.CurlyOpen || previousToken?.Kind == TokKind.Comma) && !commentToken.Value.StartsWith("//") && !commentToken.Value.StartsWith("\n"))
                {
                    result = result
                        .With(GetScriptForToken(tokenSource.Token))
                        .With(GetNewLine(context.IndentDepth + 1));
                }
                else if (commentToken != null && (previousToken?.Kind == TokKind.CurlyOpen || previousToken?.Kind == TokKind.Comma) && !commentToken.Value.StartsWith("//") && commentToken.Value.StartsWith("\n"))
                {
                    result = result
                        .With(GetScriptForToken(tokenSource.Token).TrimStart())
                        .With(GetNewLine(context.IndentDepth + 1));
                }
                else
                {
                    result = result.With(source.Tokens.Select(GetScriptForToken));
                }

                previousToken = tokenSource?.Token;
            }

            return result;
        }

        public override LazyList<string> Visit(TableNode node, Context context)
        {
            Contracts.AssertValue(node);

            context = context.Indent();
            var generatedNodes = PreGenerateNodes(context, node.SourceList, out var hasNewline);
            var useNewlines = node.Count > 1 || hasNewline;

            var result = LazyList<string>.Empty;
            foreach (var source in node.SourceList.Sources.Where(source => !(source is WhitespaceSource)))
            {
                var tokenSource = source as TokenSource;
                if (source is NodeSource nodeSource)
                {
                    result = result.With(generatedNodes[nodeSource]);
                }
                else if (tokenSource != null && tokenSource.Token.Kind == TokKind.Comma)
                {
                    if (useNewlines)
                    {
                        result = result
                            .With(GetScriptForToken(tokenSource.Token))
                            .With(GetNewLine(context.IndentDepth + 1));
                    }
                    else
                    {
                        result = result
                            .With(GetScriptForToken(tokenSource.Token))
                            .With(" ");
                    }
                }
                else if (tokenSource != null && tokenSource.Token.Kind == TokKind.BracketOpen && useNewlines)
                {
                    result = result
                        .With(GetScriptForToken(tokenSource.Token))
                        .With(GetNewLine(context.IndentDepth + 1));
                }
                else if (tokenSource != null && tokenSource.Token.Kind == TokKind.BracketClose && useNewlines)
                {
                    result = result
                        .With(GetNewLine(context.IndentDepth))
                        .With(GetScriptForToken(tokenSource.Token));
                }
                else
                {
                    result = result.With(source.Tokens.Select(GetScriptForToken));
                }
            }

            return result;
        }

        public override LazyList<string> Visit(AsNode node, Context context)
        {
            Contracts.AssertValue(node);

            return Basic(node, context);
        }

        private Dictionary<NodeSource, LazyList<string>> PreGenerateNodes(Context context, SourceList sourceList, out bool hasNewline)
        {
            var generatedNodes = new Dictionary<NodeSource, LazyList<string>>();
            foreach (var source in sourceList.Sources)
            {
                if (source is NodeSource nodeSource)
                {
                    generatedNodes[nodeSource] = nodeSource.Node.Accept(this, context);
                }
            }

            hasNewline = generatedNodes.Values
                .SelectMany(x => x)
                .Any(text => text.Contains("\n"));
            return generatedNodes;
        }

        private string GetScriptForToken(Token token)
        {
            return token.Span.GetFragment(_script).TrimEnd(' ');
        }

        private string GetNewLine(int indentation)
        {
            return "\n" + GetNewLineIndent(indentation);
        }

        private string GetNewLineIndent(int indentation)
        {
            return string.Concat(Enumerable.Repeat("    ", indentation - 1));
        }

        public class Context
        {
            public int IndentDepth { get; }

            public Context(int indentDepth)
            {
                IndentDepth = indentDepth;
            }

            public Context With(int indentDepth)
            {
                return new Context(indentDepth);
            }

            internal Context Indent()
            {
                return With(indentDepth: IndentDepth + 1);
            }
        }
    }
}
