// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class PublicSurfaceTests
    {
        [Fact]
        public void Test()
        {
            var asm = typeof(ICodeFixHandler).Assembly;

            // The goal for public namespaces is to make the SDK easy for the consumer. 
            // Namespace principles for public classes:
            // - prefer fewer namespaces. See C# for example: https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis
            // - For easy discovery, but Engine in "Microsoft.PowerFx".
            // - For sub areas with many related classes, cluster into a single subnamespace.
            // - Avoid nesting more than 1 level deep

            var allowed = new HashSet<string>()
            {
                // Public APIs 
                "Microsoft.PowerFx.EditorEngineExtensions",
                "Microsoft.PowerFx.EditorContextScope",
                "Microsoft.PowerFx.ICodeFixHandler",
                "Microsoft.PowerFx.Core.IPowerFxScopeFactory",
                "Microsoft.PowerFx.Core.IPowerFxScopeQuickFix",

                "Microsoft.PowerFx.LanguageServerProtocol.LanguageServer",

                // Internal
                "Microsoft.PowerFx.LanguageServerProtocol.JsonRpcHelper",
                "Microsoft.PowerFx.LanguageServerProtocol.CodeActionKind",
                "Microsoft.PowerFx.LanguageServerProtocol.CommandName",

                // Protocol classes.
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CodeAction",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CodeActionCommand",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CodeActionContext",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CodeActionParams",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CodeActionResult",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CodeActionResultContext",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CommandExecutedParams",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CompletionContext",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CompletionItem",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CompletionItemKind",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CompletionList",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CompletionParams",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CompletionTriggerKind",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.CustomProtocolNames",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.Diagnostic",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.DiagnosticSeverity",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.DidChangeTextDocumentParams",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.DidOpenTextDocumentParams",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.InitialFixupParams",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.ParameterInformation",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.Position",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.PublishDiagnosticsParams",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.PublishExpressionTypeParams",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.PublishTokensParams",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.Range",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.SignatureHelp",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.SignatureHelpContext",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.SignatureHelpParams",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.SignatureHelpTriggerKind",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.SignatureInformation",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.TextDocumentContentChangeEvent",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.TextDocumentIdentifier",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.TextDocumentItem",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.TextDocumentNames",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.TextDocumentPositionParams",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.TextEdit",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.VersionedTextDocumentIdentifier",
                "Microsoft.PowerFx.LanguageServerProtocol.Protocol.WorkspaceEdit",
            };

            var sb = new StringBuilder();
            var count = 0;
            foreach (var type in asm.GetTypes().Where(t => t.IsPublic))
            {
                var name = type.FullName;
                if (!allowed.Contains(name))
                {
                    sb.Append('"');
                    sb.Append(name);
                    sb.Append("\",");
                    sb.AppendLine();
                    count++;
                }

                allowed.Remove(name);
            }

            Assert.True(count == 0, $"Unexpected public types: {sb}");

            // Types we expect to be in the assembly are all there. 
            Assert.Empty(allowed);
        }
    }
}
