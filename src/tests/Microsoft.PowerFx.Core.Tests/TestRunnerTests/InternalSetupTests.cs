// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
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
            Assert.Null(iSetup.HandlerNames);
            Assert.Equal(TexlParser.Flags.None, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_EmptyString()
        {
            var iSetup = InternalSetup.Parse(string.Empty);

            Assert.NotNull(iSetup);
            Assert.Null(iSetup.HandlerNames);
            Assert.Equal(TexlParser.Flags.None, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_Whitespaces()
        {
            var iSetup = InternalSetup.Parse("  ");

            Assert.NotNull(iSetup);
            Assert.Null(iSetup.HandlerNames);
            Assert.Equal(TexlParser.Flags.None, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_SimpleHandler()
        {
            var iSetup = InternalSetup.Parse("SomeHandler");

            Assert.NotNull(iSetup);
            Assert.Equal("SomeHandler", iSetup.HandlerNames.First());
            Assert.Equal(TexlParser.Flags.None, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_OneFlag()
        {
            var iSetup = InternalSetup.Parse(TexlParser.Flags.NamedFormulas.ToString());

            Assert.NotNull(iSetup);
            Assert.Empty(iSetup.HandlerNames);
            Assert.Equal(TexlParser.Flags.NamedFormulas, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_TwoFlags()
        {
            var iSetup = InternalSetup.Parse($"{TexlParser.Flags.NamedFormulas}, {TexlParser.Flags.EnableExpressionChaining}");

            Assert.NotNull(iSetup);
            Assert.Empty(iSetup.HandlerNames);
            Assert.Equal(TexlParser.Flags.NamedFormulas | TexlParser.Flags.EnableExpressionChaining, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_HandlerAndFlag()
        {
            var iSetup = InternalSetup.Parse($"SomeHandler, {TexlParser.Flags.EnableExpressionChaining}");

            Assert.NotNull(iSetup);
            Assert.Equal("SomeHandler", iSetup.HandlerNames.First());
            Assert.Equal(TexlParser.Flags.EnableExpressionChaining, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_HandlerAndTwoFlags()
        {
            var iSetup = InternalSetup.Parse($", {TexlParser.Flags.NamedFormulas}, SomeHandler, {TexlParser.Flags.EnableExpressionChaining}");

            Assert.NotNull(iSetup);
            Assert.Equal("SomeHandler", iSetup.HandlerNames.First());
            Assert.Equal(TexlParser.Flags.NamedFormulas | TexlParser.Flags.EnableExpressionChaining, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_HandlerAndTwoFlags2()
        {
            var iSetup = InternalSetup.Parse($"SomeHandler, NamedFormulas, EnableExpressionChaining");

            Assert.NotNull(iSetup);
            Assert.Equal("SomeHandler", iSetup.HandlerNames.First());
            Assert.Equal(TexlParser.Flags.NamedFormulas | TexlParser.Flags.EnableExpressionChaining, iSetup.Flags);
        }

        [Fact]
        public void InternalSetup_Parse_DisableTableSyntaxDoesntWrapRecordsFlags()
        {
            var iSetup = InternalSetup.Parse($"disable:TableSyntaxDoesntWrapRecords");

            Assert.NotNull(iSetup);
            Assert.Empty(iSetup.HandlerNames);
            Assert.False(iSetup.Features.TableSyntaxDoesntWrapRecords);
        }

        [Fact]
        public void InternalSetup_Parse_DisableMultipleFlags()
        {
            var iSetup = InternalSetup.Parse($"disable:TableSyntaxDoesntWrapRecords,disable:ConsistentOneColumnTableResult");

            Assert.NotNull(iSetup);
            Assert.Empty(iSetup.HandlerNames);

            Assert.False(iSetup.Features.TableSyntaxDoesntWrapRecords);
            Assert.False(iSetup.Features.ConsistentOneColumnTableResult);
        }

        [Fact]
        public void InternalSetup_Parse_EnablingAndDisablingFeatures()
        {
            var iSetup = InternalSetup.Parse("SomeHandler,disable:TableSyntaxDoesntWrapRecords,EnableExpressionChaining");
            Assert.Equal(TexlParser.Flags.EnableExpressionChaining, iSetup.Flags);
            Assert.Equal("SomeHandler", iSetup.HandlerNames.First());
            Assert.False(iSetup.Features.TableSyntaxDoesntWrapRecords);
        }

        [Fact]
        public void InternalSetup_Parse_TwoHandlers()
        {
            var iSetup = InternalSetup.Parse("Handler1, Handler2");
            Assert.NotNull(iSetup);
            Assert.Equal(2, iSetup.HandlerNames.Count());
            Assert.Equal("Handler1", iSetup.HandlerNames.First());
            Assert.Equal("Handler2", iSetup.HandlerNames.Last());
        }
    }
}
