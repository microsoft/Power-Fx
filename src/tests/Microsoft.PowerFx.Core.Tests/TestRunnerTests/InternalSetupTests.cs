// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Parser;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class InternalSetupTests : PowerFxTest
    {
        [Fact]
        public void InternalSetup_Parse_Null()
        {
            var iSetup = InternalSetup.Parse(null);

            Assert.NotNull(iSetup);
            Assert.Null(iSetup.HandlerName);
            Assert.Equal(TexlParser.Flags.None, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_EmptyString()
        {
            var iSetup = InternalSetup.Parse(string.Empty);

            Assert.NotNull(iSetup);
            Assert.Null(iSetup.HandlerName);
            Assert.Equal(TexlParser.Flags.None, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_Whitespaces()
        {
            var iSetup = InternalSetup.Parse("  ");

            Assert.NotNull(iSetup);
            Assert.Null(iSetup.HandlerName);
            Assert.Equal(TexlParser.Flags.None, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_SimpleHandler()
        {
            var iSetup = InternalSetup.Parse("SomeHandler");

            Assert.NotNull(iSetup);
            Assert.Equal("SomeHandler", iSetup.HandlerName);
            Assert.Equal(TexlParser.Flags.None, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_OneFlag()
        {
            var iSetup = InternalSetup.Parse(TexlParser.Flags.NamedFormulas.ToString());

            Assert.NotNull(iSetup);
            Assert.Null(iSetup.HandlerName);
            Assert.Equal(TexlParser.Flags.NamedFormulas, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_TwoFlags()
        {
            var iSetup = InternalSetup.Parse($"{TexlParser.Flags.NamedFormulas}, {TexlParser.Flags.EnableExpressionChaining}");

            Assert.NotNull(iSetup);
            Assert.Null(iSetup.HandlerName);
            Assert.Equal(TexlParser.Flags.NamedFormulas | TexlParser.Flags.EnableExpressionChaining, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_HandlerAndFlag()
        {
            var iSetup = InternalSetup.Parse($"SomeHandler, {TexlParser.Flags.EnableExpressionChaining}");

            Assert.NotNull(iSetup);
            Assert.Equal("SomeHandler", iSetup.HandlerName);
            Assert.Equal(TexlParser.Flags.EnableExpressionChaining, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_HandlerAndTwoFlags()
        {
            var iSetup = InternalSetup.Parse($", {TexlParser.Flags.NamedFormulas}, SomeHandler, {TexlParser.Flags.EnableExpressionChaining}");

            Assert.NotNull(iSetup);
            Assert.Equal("SomeHandler", iSetup.HandlerName);
            Assert.Equal(TexlParser.Flags.NamedFormulas | TexlParser.Flags.EnableExpressionChaining, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_HandlerAndTwoFlags2()
        {
            var iSetup = InternalSetup.Parse($"SomeHandler, NamedFormulas, EnableExpressionChaining");

            Assert.NotNull(iSetup);
            Assert.Equal("SomeHandler", iSetup.HandlerName);
            Assert.Equal(TexlParser.Flags.NamedFormulas | TexlParser.Flags.EnableExpressionChaining, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_TableSyntaxDoesntWrapRecordsFlags()
        {
            var iSetup = InternalSetup.Parse($"TableSyntaxDoesntWrapRecords");

            Assert.NotNull(iSetup);
            Assert.Null(iSetup.HandlerName);
            Assert.Equal(Features.TableSyntaxDoesntWrapRecords, iSetup.Features);
        }

        [Fact]
        public void InternalSetup_Parse_TwoHandlers()
        {
            Assert.Throws<ArgumentException>(() => InternalSetup.Parse("Handler1, Handler2, Handler3, Handler4, Handler5, Handler6"));            
        }
    }
}
