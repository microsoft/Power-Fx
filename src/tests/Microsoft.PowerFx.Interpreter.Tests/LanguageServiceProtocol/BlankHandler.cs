// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol.Tests
{
    // Sample CodeFix handler to convert 
    //   Blank(x) --> IsBlank(x) 
    public class BlankHandler : ICodeFixHandler
    {
        public const string Title = "Blank() --> IsBlank()";

        public async Task<IEnumerable<CodeActionResult>> SuggestFixesAsync(Engine engine, CheckResult result, CancellationToken cancel)
        {
            var v = new CodeFixVisitor
            {
                _check = result,
                _expression = result.Parse.Text,
                _fixes = new List<CodeActionResult>()
            };

            result.Parse.Root.Accept(v);

            return v._fixes;
        }

        private class CodeFixVisitor : IdentityTexlVisitor
        {
            public CheckResult _check;
            public string _expression;
            public List<CodeActionResult> _fixes;

            public override void PostVisit(CallNode node)
            {
                var name = node.Head.Name;

                var type = _check.GetNodeType(node);

                if (name == "Blank" && node.Args.Count == 1)
                {
                    var span = node.Head.Token.Span;
                    var newExpr = Replace(span, _expression, "IsBlank");

                    _fixes.Add(new CodeActionResult
                    {
                        Text = newExpr,
                        Title = Title,
                        ActionResultContext = new CodeActionResultContext
                        {
                            ProviderName = nameof(BlankHandler),
                            ActionIdentifier = "Suggestion"
                        }
                    });
                }
            }
        }

        public static string Replace(Span span, string fullExpression, string newText)
        {
            var left = fullExpression.Substring(0, span.Min);
            var right = fullExpression.Substring(span.Lim);
            var x = left + newText + right;

            return x;
        }
    }
}
