// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Tests.BindingEngineTests;

namespace Microsoft.PowerFx.Core.Tests
{
    // Test obselete functions, but still required for compat 
#pragma warning disable SA1507 // Code should not contain multiple blank lines in a row
#pragma warning disable CS0618 // Type or member is obsolete
    public class CompatTests : PowerFxTest
    {
        [Fact]
        public void ConfigFunctionInfos()
        {
            var config = new PowerFxConfig();

            var func = new BehaviorFunction();
            config.AddFunction(func);

            // Includes default functions 
            // $$$ FunctionInfos is obsolete
            var functions = config.FunctionInfos.ToArray();

            Assert.True(functions.Length > 100);
            Assert.Contains(functions, x => x.Name == func.Name);
        }

        // Verify that an engine can override CreateResolver() directly. 
        // They shouldn't do this - instead use SymbolTables.
        [Fact]
        public void OverrideResolver()
        {
            var engine = new TestEngine();
            var result = engine.Check("x"); // defined in TestEngine's custom resolver
            Assert.True(result.IsSuccess);
            Assert.Equal(FormulaType.Number, result.ReturnType);
        }

        private class TestEngine : Engine
        {
            public TestEngine() 
                : base(new PowerFxConfig())
            {
            }

            [Obsolete]
            private protected override INameResolver CreateResolver()
            {
                // Use SymbolTable as an easy way to implement INameResolver.
                var s = new SymbolTable();
                s.AddConstant("x", FormulaValue.New(3));
                return s;
            }
        }
    }
}
