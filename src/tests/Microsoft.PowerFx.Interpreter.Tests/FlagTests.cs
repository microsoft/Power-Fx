// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class FlagTests
    {
        /// <summary>
        /// "@ syntax is deprecated. Now, users should write 
        /// Filter(A, ThisRecord.Value = 2) or Filter(A As Foo, Foo.Value = 2)
        /// instead of
        /// Filter(A, A[@Value] = 2).
        /// </summary>
        /// <param name="expression"></param>
        [Theory]
        [InlineData("Sum(A, A[@Value])")]
        [InlineData("Filter(A, A[@Value] = 2)")]
        public void Parser_DisableRowScopeDisambiguationSyntax(string expression)
        {
            var engine = new RecalcEngine(new PowerFxConfig(features: Features.DisableRowScopeDisambiguationSyntax));
            var engineWithoutFlag = new RecalcEngine(new PowerFxConfig());
            
            NumberValue r1 = FormulaValue.New(1);
            NumberValue r2 = FormulaValue.New(2);
            NumberValue r3 = FormulaValue.New(3);
            TableValue val = FormulaValue.NewSingleColumnTable(r1, r2, r3);

            engine.UpdateVariable("A", val);
            var resultWithFlag = engine.Parse(expression);

            engineWithoutFlag.UpdateVariable("A", val);
            var resultWithoutFlag = engineWithoutFlag.Parse(expression);

            Assert.False(resultWithFlag.IsSuccess);
            Assert.NotNull(resultWithFlag.Errors.First(error => error.MessageKey.Equals(TexlStrings.ErrDeprecated.Key)));

            Assert.True(resultWithoutFlag.IsSuccess);
        }
    }
}
