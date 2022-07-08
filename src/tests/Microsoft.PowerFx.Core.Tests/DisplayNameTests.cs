// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DisplayNameTests : PowerFxTest
    {
        public DisplayNameTests()
            : base()
        {
            _engine = new Engine(new PowerFxConfig(CultureInfo.InvariantCulture));
        }

        private readonly Engine _engine;

        [Fact]
        public void CollisionsThrow()
        {
            var r1 = new KnownRecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number, new DName("DisplayNum")));

            Assert.Throws<NameCollisionException>(() => r1.Add(new NamedFormulaType("DisplayNum", FormulaType.Date, "NoCollision")));
            Assert.Throws<NameCollisionException>(() => r1.Add(new NamedFormulaType("NoCollision", FormulaType.Date, "DisplayNum")));
            Assert.Throws<NameCollisionException>(() => r1.Add(new NamedFormulaType("NoCollision", FormulaType.Date, "Num")));
        }

        [Fact]
        public void ImmutableDisplayNameProvider()
        {
            var r1 = new KnownRecordType();

            var r2 = r1.Add(new NamedFormulaType("Logical", FormulaType.String, "Foo"));
            var r3 = r1.Add(new NamedFormulaType("Logical", FormulaType.String, "Bar"));

            Assert.False(ReferenceEquals(r2._type.DisplayNameProvider, r3._type.DisplayNameProvider));
        }

        [Fact]
        public void DisableDisplayNames()
        {
            var r1 = new KnownRecordType()
                .Add(new NamedFormulaType("Logical", FormulaType.String, "Foo"));

            var r2 = new KnownRecordType()
                .Add(new NamedFormulaType("Other", FormulaType.String, "Foo"));

            Assert.IsType<SingleSourceDisplayNameProvider>(r1._type.DisplayNameProvider);

            var disabledType = DType.AttachOrDisableDisplayNameProvider(r1._type, r2._type.DisplayNameProvider);

            Assert.IsType<DisabledDisplayNameProvider>(disabledType.DisplayNameProvider);
        }

        [Theory]
        [InlineData("If(B, Num, 1234)", "If(DisplayB, DisplayNum, 1234)", true)]
        [InlineData("If(DisplayB, DisplayNum, 1234)", "If(DisplayB, DisplayNum, 1234)", true)]
        [InlineData("If(DisplayB, Num, 1234)", "If(DisplayB, DisplayNum, 1234)", true)]
        [InlineData("Sum(Nested, Inner)", "Sum(NestedDisplay, InnerDisplay)", true)]
        [InlineData("Sum(Nested /* The source */ , Inner /* Sum over the InnerDisplay column */)", "Sum(NestedDisplay /* The source */ , InnerDisplay /* Sum over the InnerDisplay column */)", true)]
        [InlineData("If(DisplayB, DisplayNum, 1234)", "If(B, Num, 1234)", false)]
        [InlineData("If(B, Num, 1234)", "If(B, Num, 1234)", false)]
        [InlineData("If(DisplayB, Num, 1234)", "If(B, Num, 1234)", false)]
        [InlineData("Sum(NestedDisplay, InnerDisplay)", "Sum(Nested, Inner)", false)]
        [InlineData("Sum(NestedDisplay /* The source */ , InnerDisplay /* Sum over the InnerDisplay column */)", "Sum(Nested /* The source */ , Inner /* Sum over the InnerDisplay column */)", false)]
        public void ValidateDisplayNames(string inputExpression, string outputExpression, bool toDisplay)
        {
            var r1 = new KnownRecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number, "DisplayNum"))
                .Add(new NamedFormulaType("B", FormulaType.Boolean, "DisplayB"))
                .Add(new NamedFormulaType(
                    "Nested", 
                    new TableType().Add(new NamedFormulaType("Inner", FormulaType.Number, "InnerDisplay")), 
                    "NestedDisplay"));

            if (toDisplay)
            {
                var outDisplayExpression = _engine.GetDisplayExpression(inputExpression, r1);
                Assert.Equal(outputExpression, outDisplayExpression);
            }
            else
            {
                var outInvariantExpression = _engine.GetInvariantExpression(inputExpression, r1);
                Assert.Equal(outputExpression, outInvariantExpression);
            }
        }

        [Fact]
        public void ConvertToDisplayNamesNoNames()
        {
            var r1 = new KnownRecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number))
                .Add(new NamedFormulaType("B", FormulaType.Boolean));

            var displayExpressions = _engine.GetDisplayExpression("If(B, Num, 1234)", r1);

            Assert.Equal("If(B, Num, 1234)", displayExpressions);
        }

        [Fact]
        public void ConvertToInvariantNamesNoNames()
        {
            var r1 = new KnownRecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number))
                .Add(new NamedFormulaType("B", FormulaType.Boolean));

            var displayExpressions = _engine.GetInvariantExpression("If(B, Num, 1234)", r1);

            Assert.Equal("If(B, Num, 1234)", displayExpressions);
        }

        [Theory]
        [InlineData("If(B, Num, 1234)", "If(A, Num, 1234)", "B", "A")]
        [InlineData("RecordNest.SomeString", "RecordNest.SomeString", "B", "A")]
        [InlineData("RecordNest.SomeString", "RecordNest.Foo", "RecordNest.SomeString", "Foo")]
        [InlineData("RecordNest.SomeString", "Foo.SomeString", "RecordNest", "Foo")]
        [InlineData("First(Nested).Inner", "First(Nested).Inner", "B", "A")]
        [InlineData("First(Nested).Inner", "First(Nested).Bar", "Nested.Inner", "Bar")]
        [InlineData("First(Nested).Inner", "First(Bar).Inner", "Nested", "Bar")]
        [InlineData("First(Nested).InnerDisplay", "First(Bar).Inner", "Nested", "Bar")]
        [InlineData("First(Nested).InnerDisplay", "First(Nested).InnerRenamedLogicalName", "Nested.Inner", "InnerRenamedLogicalName")]
        [InlineData("With({SomeValue: 123}, RecordNest.nest2.datetest)", "With({SomeValue: 123}, RecordNest.nest2.Foo)", "RecordNest.nest2.datetest", "Foo")]
        [InlineData("With({RecordNest: {nest2: {datetest: 123}}}, RecordNest.nest2.datetest)", "With({RecordNest: {nest2: {datetest: 123}}}, RecordNest.nest2.datetest)", "RecordNest.nest2.datetest", "Foo")]
        [InlineData("With(RecordNest, SomeString + nest2.datetest)", "With(Foo, SomeString + nest2.datetest)", "RecordNest", "Foo")]
        [InlineData("With(RecordNest, SomeString + nest2.datetest)", "With(RecordNest, Foo + nest2.datetest)", "RecordNest.SomeString", "Foo")]
        [InlineData("With(RecordNest, SomeString + nest2.datetest)", "With(RecordNest, SomeString + Foo.datetest)", "RecordNest.nest2", "Foo")]
        [InlineData("With(RecordNest, SomeString + nest2.datetest)", "With(RecordNest, SomeString + nest2.Foo)", "RecordNest.nest2.datetest", "Foo")]
        [InlineData("With({value: RecordNest.SomeString}, value & B)", "With({value: RecordNest.'abcd efg'}, value & B)", "RecordNest.SomeString", "abcd efg")]
        [InlineData("If(B, Text(B), \"B\")", "If(A, Text(A), \"B\")", "B", "A")]
        [InlineData("B & Invalid()", "A & Invalid()", "B", "A")] // Rename with bind errors
        [InlineData("B + + + ", "A + + + ", "B", "A")] // Rename with parse errors
        [InlineData("With({x: RecordNest, y: RecordNest}, x.SomeString & y.SomeString)", "With({x: RecordNest, y: RecordNest}, x.S2 & y.S2)", "RecordNest.SomeString", "S2")]
        public void RenameParameter(string expressionBase, string expectedExpression, string path, string newName)
        {
            var r1 = new KnownRecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number, "DisplayNum"))
                .Add(new NamedFormulaType("B", FormulaType.Boolean, "DisplayB"))
                .Add(new NamedFormulaType(
                        "Nested",
                        new TableType().Add(new NamedFormulaType("Inner", FormulaType.Number, "InnerDisplay")),
                        "NestedDisplay"))
                .Add(new NamedFormulaType(
                        "RecordNest",
                        new KnownRecordType()
                            .Add(new NamedFormulaType("SomeString", FormulaType.String, "DisplaySomeString"))
                            .Add(new NamedFormulaType("nest2", new KnownRecordType().Add(new NamedFormulaType("datetest", FormulaType.DateTime, "DisplayDT")), "DisplayReallyNested")),
                        "superrecordnest"));

            var dpath = DPath.Root;
            foreach (var segment in path.Split('.'))
            {
                dpath = dpath.Append(new DName(segment));
            }

            var renamer = _engine.CreateFieldRenamer(r1, dpath, new DName(newName));

            Assert.Equal(expectedExpression, renamer.ApplyRename(expressionBase));
        }

        [Fact]
        public void ConvertToDisplayNotForced()
        {
            var r1 = new KnownRecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number, "SomeDisplayNum"))
                .Add(new NamedFormulaType("B", FormulaType.Boolean, "SomeDisplayB"));

            var formula = new Formula("If(SomeDisplayB, SomeDisplayNum, 1234)", CultureInfo.InvariantCulture);
            formula.EnsureParsed(TexlParser.Flags.None);

            var binding = TexlBinding.Run(
                new Glue2DocumentBinderGlue(),
                null,
                new Core.Entities.QueryOptions.DataSourceToQueryOptionsMap(),
                formula.ParseTree,
                new SimpleResolver(new PowerFxConfig(CultureInfo.InvariantCulture)),
                BindingConfig.Default,
                ruleScope: r1._type,
                updateDisplayNames: true);

            Assert.Empty(binding.NodesToReplace);
        }
    }

    public class CommaSeparatedDecimalLocaleConversionTests
    {
        public CommaSeparatedDecimalLocaleConversionTests()
        {
            _engine = new Engine(new PowerFxConfig(CultureInfo.GetCultureInfo("fr-FR")));
        }

        private readonly Engine _engine;

        [Theory]
        [InlineData("If(B, Num, 1234.56)", "If(DisplayB; DisplayNum; 1234,56)", true)]
        [InlineData("123456.789", "123456,789", true)]
        [InlineData("a;b;c;d;e;", "a;;b;;c;;d;;e;;", true)]
        [InlineData("If(DisplayB, DisplayNum, 1234)", "If(DisplayB; DisplayNum; 1234)", true)]
        [InlineData("If(DisplayB, Num, 1234)", "If(DisplayB; DisplayNum; 1234)", true)]
        [InlineData("Sum(Nested, Inner)", "Sum(NestedDisplay; InnerDisplay)", true)]
        [InlineData("Sum(Nested /* The source */ , Inner /* Sum over the InnerDisplay column */)", "Sum(NestedDisplay /* The source */ ; InnerDisplay /* Sum over the InnerDisplay column */)", true)]
        [InlineData("If(DisplayB; DisplayNum; 1234,56)", "If(B, Num, 1234.56)", false)]
        [InlineData("123456,789", "123456.789", false)]
        [InlineData("a;;b;;c;;d;;e;;", "a;b;c;d;e;", false)]
        [InlineData("If(B; Num; 1234)", "If(B, Num, 1234)", false)]
        [InlineData("If(DisplayB; Num; 1234)", "If(B, Num, 1234)", false)]
        [InlineData("Sum(NestedDisplay; InnerDisplay)", "Sum(Nested, Inner)", false)]
        [InlineData("Sum(NestedDisplay /* The source */ ; InnerDisplay /* Sum over the InnerDisplay column */)", "Sum(Nested /* The source */ , Inner /* Sum over the InnerDisplay column */)", false)]
        public void ValidateExpressionConversionCommaSeparatedLocale(string inputExpression, string outputExpression, bool toDisplay)
        {
            var r1 = new KnownRecordType()
                .Add(new NamedFormulaType("Num", FormulaType.Number, "DisplayNum"))
                .Add(new NamedFormulaType("B", FormulaType.Boolean, "DisplayB"))
                .Add(new NamedFormulaType(
                    "Nested",
                    new TableType().Add(new NamedFormulaType("Inner", FormulaType.Number, "InnerDisplay")),
                    "NestedDisplay"));

            if (toDisplay)
            {
                var outDisplayExpression = _engine.GetDisplayExpression(inputExpression, r1);
                Assert.Equal(outputExpression, outDisplayExpression);
            }
            else
            {
                var outInvariantExpression = _engine.GetInvariantExpression(inputExpression, r1);
                Assert.Equal(outputExpression, outInvariantExpression);
            }
        }
    }
}
