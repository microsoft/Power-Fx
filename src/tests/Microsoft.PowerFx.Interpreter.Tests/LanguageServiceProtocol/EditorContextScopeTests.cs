// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Tests.LanguageServiceProtocol.Tests.LanguageServerTests;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class EditorContextScopeTests
    {
        // Check() calls through to engine. 
        [Fact]
        public void Test()
        {
            var engine = new Engine(new PowerFxConfig());

            var scope = engine.CreateEditorScope();
            var result = scope.Check("1+2");
            Assert.Equal(result.ReturnType, FormulaType.Number);
        }

        [Fact]
        public void Fix()
        {
            var engine = new Engine(new PowerFxConfig());

            var scope = engine.CreateEditorScope();

            Assert.Throws<ArgumentNullException>(() => scope.AddQuickFixHandler(null));
            scope.AddQuickFixHandler(new MyEmptyHandler());
            scope.AddQuickFixHandler(new MyHandler());

            // Can't double add a  handler with same name. 
            Assert.Throws<InvalidOperationException>(() => scope.AddQuickFixHandler(new MyHandler()));

            var fixes = scope.SuggestFixes("1+", null); // error

            Assert.Single(fixes);
            var fix = fixes[0];
            Assert.Equal("MyText", fix.Text);
            Assert.Equal("MyTitle", fix.Title);
        }

        // Still report ecen when a handler throws. 
        [Fact]
        public void FixHandlerFails()
        {
            var failHandler = new ExceptionQuickFixHandler();

            var engine = new Engine(new PowerFxConfig());

            var scope = engine.CreateEditorScope();
            scope.AddQuickFixHandler(new MyHandler());
            scope.AddQuickFixHandler(failHandler);

            var errorList = new List<Exception>();
            var fixes = scope.SuggestFixes("1+2", (ex) =>
            {
                errorList.Add(ex);
            });

            // Still got fix from handler 
            Assert.Single(fixes);
            var fix = fixes[0];
            Assert.Equal("MyText", fix.Text);
            Assert.Equal("MyTitle", fix.Title);

            // Also got error reported. 
            Assert.Equal(1, failHandler._counter); // was invoked
            Assert.Single(errorList);
            var ex2 = errorList[0];

            Assert.Contains(failHandler.HandlerName, ex2.Message);            
        }

        // Verify OnCommandExecuted callbacks is executed. 
        [Fact]
        public void OnCommandExecuted()
        {
            var handler = new MyHandler();
            var engine = new Engine(new PowerFxConfig());

            var scope = engine.CreateEditorScope();
            scope.AddQuickFixHandler(handler);

            var fixes = scope.SuggestFixes("1+1", null);

            scope.OnCommandExecuted(null); // Nop
            scope.OnCommandExecuted(new CodeAction()); // Nop

            var ca = new CodeAction
            {
                ActionResultContext = new CodeActionResultContext()
            };
            scope.OnCommandExecuted(ca); // nop

            ca.ActionResultContext.HandlerName = string.Empty;
            scope.OnCommandExecuted(ca); // nop

            ca.ActionResultContext.HandlerName = "   "; // whitespace
            scope.OnCommandExecuted(ca); // nop

            Assert.Empty(handler._onExecuted); // all were nops.

            // Set to real value from Suggest to get callback
            var fix = fixes[0];
            ca = new CodeAction
            {
                 ActionResultContext = fix.ActionResultContext
            };            
            scope.OnCommandExecuted(ca);
            Assert.Single(handler._onExecuted);
            Assert.Equal("Action1", handler._onExecuted[0]);
        }

        // Calling Suggest() on intellisense doesn't need to compute errors
        [Fact]
        public void SuggestDoesntNeedErrors()
        {
            var engine = new MyEngine();

            IPowerFxScope ctx = engine.CreateEditorScope();
            var result = ctx.Suggest("1+2", 1);

            Assert.Equal(0, engine.PostCheckCounter);            
        }

        [Fact]
        public void NullCtor()
        {
            Assert.Throws<ArgumentNullException>(() => new EditorContextScope(null));
        }

        [Fact]
        public void Ctor()
        {
            var check = new CheckResult(new Engine());
            var editor = new EditorContextScope(
                (expr) => check.SetText(expr).SetBindingInfo());

            var check2 = editor.Check("1+2");
            Assert.Same(check, check2);

            Assert.True(check2.IsSuccess);
        }

        // Fail if the getter doesn't fully create the CheckResult
        [Fact]
        public void MissingInit()
        {
            var check = new CheckResult(new Engine());

            var editor = new EditorContextScope(
                (expr) => check.SetText(expr));

            Assert.Throws<InvalidOperationException>(() => editor.Check("1+2"));

            editor = new EditorContextScope(
                (expr) => check);

            Assert.Throws<InvalidOperationException>(() => editor.Check("3+4"));
        }

        private class MyEngine : Engine
        {
            public MyEngine()
                : base(new PowerFxConfig())
            {
            }

            public int PostCheckCounter = 0;
                    
            protected override IEnumerable<ExpressionError> PostCheck(CheckResult check)
            {
                PostCheckCounter++;
                return base.PostCheck(check);
            }
        }

        [Fact]
        public void HandlerName()
        {
            var handler = new MyHandler();
            var name = handler.HandlerName;

            // By default, handler name is the fully qualified name. 
            Assert.Equal("Microsoft.PowerFx.Tests.LanguageServiceProtocol.EditorContextScopeTests+MyHandler", name);
        }

        private class MyEmptyHandler : CodeFixHandler
        {
            public override void OnCodeActionApplied(string actionId)
            {
                // Empty implementaion.
            }

            public override async Task<IEnumerable<CodeFixSuggestion>> SuggestFixesAsync(
                Engine engine,
                CheckResult checkResult,
                CancellationToken cancel)
            {
                return null;
            }
        }

        private class MyHandler : CodeFixHandler
        {
            public List<string> _onExecuted = new List<string>();

            public override void OnCodeActionApplied(string actionIdentifier)
            {
                _onExecuted.Add(actionIdentifier);
            }

            public override async Task<IEnumerable<CodeFixSuggestion>> SuggestFixesAsync(
                Engine engine, 
                CheckResult checkResult,
                CancellationToken cancel)
            {
                return new CodeFixSuggestion[]
                {
                    new CodeFixSuggestion
                    {
                         SuggestedText = "MyText",
                         Title = "MyTitle",
                         ActionIdentifier = "Action1"
                    }
                };
            }
        }
    }
}
