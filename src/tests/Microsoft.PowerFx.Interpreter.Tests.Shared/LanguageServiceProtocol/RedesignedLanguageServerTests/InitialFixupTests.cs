// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Tests.LanguageServiceProtocol.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public partial class LanguageServerTestBase
    {
        [Fact]
        public async Task TestInitialFixup()
        {
            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => new MockSqlEngine());

            Init(new InitParams(scopeFactory: scopeFactory));
            var documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}";
            var payload = GetRequestPayload(
            new InitialFixupParams()
            {
                TextDocument = new TextDocumentItem()
                {
                    Uri = documentUri,
                    LanguageId = "powerfx",
                    Version = 1,
                    Text = "new_price * new_quantity"
                }
            }, CustomProtocolNames.InitialFixup);
            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);
            var response = AssertAndGetResponsePayload<TextDocumentItem>(rawResponse, payload.id);

            Assert.Equal(documentUri, response.Uri);
            Assert.Equal("Price * Quantity", response.Text);

            // no change
            payload = GetRequestPayload(
            new InitialFixupParams()
            {
                TextDocument = new TextDocumentItem()
                {
                    Uri = documentUri,
                    LanguageId = "powerfx",
                    Version = 1,
                    Text = "Price * Quantity"
                }
            }, CustomProtocolNames.InitialFixup);
            rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);
            response = AssertAndGetResponsePayload<TextDocumentItem>(rawResponse, payload.id);
            Assert.Equal(documentUri, response.Uri);
            Assert.Equal("Price * Quantity", response.Text);
        }

        [Fact]
        public async Task TestInitialFixupRewriter()
        {
            var engine = new RecalcEngine();

            var record = RecordType.Empty().Add("acc_name", FormulaType.String, "Account Name");
            var symbol = new SymbolTable();
            symbol.AddVariable("NewRecord", record);

            var editorContextScope = new EditorContextScope(engine: engine, parserOptions: null, symbols: symbol);
            editorContextScope.AddPostCheckExpressionRewriter(new MockExpressionRewriter());

            var scopeFactory = new TestPowerFxScopeFactory((string documentUri) => editorContextScope);
            Init(new InitParams(scopeFactory: scopeFactory));
            var documentUri = "powerfx://app?context={\"A\":1,\"B\":[1,2,3]}";
            var payload = GetRequestPayload(
                new InitialFixupParams()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = documentUri,
                        LanguageId = "powerfx",
                        Version = 1,
                        Text = "Filter([1], ThisRecord.Value = 2); ThisRecord.acc_name; With({ThisRecord : 5}, ThisRecord + 2)"
                    }
                }, CustomProtocolNames.InitialFixup);

            var rawResponse = await TestServer.OnDataReceivedAsync(payload.payload);
            var response = AssertAndGetResponsePayload<TextDocumentItem>(rawResponse, payload.id);

            Assert.Equal(documentUri, response.Uri);
            Assert.Equal("Filter([1], ThisRecord.Value = 2); NewRecord.'Account Name'; With({ThisRecord : 5}, ThisRecord + 2)", response.Text);
        }

        private class MockExpressionRewriter : IExpressionRewriter
        {
            public string Process(CheckResult check)
            {
                var mockVisitor = new MockVisitor(check);
                check.ApplyParse().Root.Accept(mockVisitor);
                return mockVisitor.ApplyReplacement();
            }

            private class MockVisitor : IdentityTexlVisitor
            {
                private readonly CheckResult _check;

                private readonly IList<KeyValuePair<Span, string>> _replacements = new List<KeyValuePair<Span, string>>(); 

                public MockVisitor(CheckResult check)
                {
                    this._check = check;
                }

                public override void Visit(FirstNameNode node)
                {
                    if (node.Ident.Name == "ThisRecord")
                    {
                        // makes sure "ThisRecord" is not coming from a scope.
                        if (!_check.IsNodeValidVariable(node))
                        {
                            _replacements.Add(new KeyValuePair<Span, string>(node.GetTextSpan(), "NewRecord"));
                        }
                    }

                    base.Visit(node);
                }

                public string ApplyReplacement()
                {
                    return ReplaceSpans(_check.ApplyGetInvariant(), _replacements);
                }

                private static string ReplaceSpans(string script, IEnumerable<KeyValuePair<Span, string>> worklist)
                {
                    StringBuilder sb = new StringBuilder(script.Length);

                    int index = 0;
                    int lastLim = -1;

                    foreach (KeyValuePair<Span, string> pair in worklist.OrderBy(kvp => kvp.Key.Min))
                    {
                        if (pair.Key.Min < lastLim)
                        {
                            // Avoid corrupting the replacement.
                            throw new InvalidOperationException($"Post-processing failed: replacement span overlap");
                        }

                        sb.Append(script, index, pair.Key.Min - index);
                        sb.Append(pair.Value);
                        index = pair.Key.Lim;

                        lastLim = pair.Key.Lim;
                    }

                    if (index < script.Length)
                    {
                        sb.Append(script, index, script.Length - index);
                    }

                    return sb.ToString();
                }
            }
        }
    }
}
