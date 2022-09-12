// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class EditorContextScopeTests
    {
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

            IPowerFxScopeQuickFix quickFix = scope;
            var fixes = quickFix.Suggest("1+"); // error

            Assert.Single(fixes);
            var fix = fixes[0];
            Assert.Equal("MyText", fix.Text);
            Assert.Equal("MyTitle", fix.Title);
        }

        private class MyEmptyHandler : ICodeFixHandler
        {
            public async Task<IEnumerable<CodeActionResult>> SuggestFixesAsync(
                Engine engine,
                CheckResult checkResult)
            {
                return null;
            }
        }

        private class MyHandler : ICodeFixHandler
        {
            public async Task<IEnumerable<CodeActionResult>> SuggestFixesAsync(
                Engine engine, 
                CheckResult checkResult)
            {
                return new CodeActionResult[]
                {
                    new CodeActionResult
                    {
                         Text = "MyText",
                         Title = "MyTitle"
                    }
                };
            }
        }
    }
}
