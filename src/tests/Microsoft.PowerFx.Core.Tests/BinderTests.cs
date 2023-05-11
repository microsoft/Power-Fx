// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class BinderTests : PowerFxTest
    {
        [Theory]
        [InlineData("x")]
        [InlineData("x.Value")]
        [InlineData("x.Tbl")]
        [InlineData("First(x.Tbl)")]
        [InlineData("First(x.Tbl).Value2")]
        [InlineData("FirstN(x.Tbl, 3).Value2")]
        [InlineData("Last(x.Tbl).Value2")]
        [InlineData("LastN(x.Tbl, 3).Value2")]
        [InlineData("Index(x.Tbl, 3).Value2")]
        [InlineData("x.Rec")]
        [InlineData("x.Rec.Value3")]
        public void TestMutableNodes(string expression)
        {
            var config = new PowerFxConfig();
            config.SymbolTable.AddVariable(
                "x",
                new KnownRecordType(TestUtils.DT("![Value:n,Tbl:*[Value2:n],Rec:![Value3:n,Value4:n]]")));
            var engine = new Engine(config);
            var checkResult = engine.Check(expression);
            var binding = checkResult.Binding;
            Assert.True(binding.IsMutable(binding.Top));
        }
    }
}
