// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DisplayNameTests : PowerFxTest
    {
        public class LazyRecordType : RecordType
        {
            public override IEnumerable<string> FieldNames { get; }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                type = name switch
                {
                    "Num" => FormulaType.Number,
                    "B" => FormulaType.Boolean,
                    "Nested" => TableType.Empty().Add(new NamedFormulaType("Inner", FormulaType.Number, "InnerDisplay")),
                    _ => FormulaType.Blank
                };

                return type != FormulaType.Blank;
            }

            public LazyRecordType()
                : base(new CustomDisplayNameProvider())
            {
                FieldNames = new List<string>() { "Num", "B", "Nested" };
            }

            public override bool Equals(object other)
            {
                return other is LazyRecordType;
            }

            public override int GetHashCode()
            {
                return 3;
            }

            private class CustomDisplayNameProvider : DisplayNameProvider
            {
                public override IEnumerable<KeyValuePair<DName, DName>> LogicalToDisplayPairs => throw new NotImplementedException();

                public override bool TryGetDisplayName(DName logicalName, out DName displayDName)
                {
                    var displayName = logicalName.Value switch
                    {
                        "Num" => "DisplayNum",
                        "B" => "DisplayB",
                        "Inner" => "InnerDisplay",
                        "Nested" => "NestedDisplay",
                        _ => null
                    };
                    displayDName = displayName == null ? default : new DName(displayName);
                    return displayName != null;
                }

                public override bool TryGetLogicalName(DName displayName, out DName logicalDName)
                {
                    var logicalName = displayName.Value switch
                    {
                        "DisplayNum" => "Num",
                        "DisplayB" => "B",
                        "InnerDisplay" => "Inner",
                        "NestedDisplay" => "Nested",
                        _ => null
                    };
                    logicalDName = logicalName == null ? default : new DName(logicalName);
                    return logicalName != null;
                }

                internal override bool TryRemapLogicalAndDisplayNames(DName displayName, out DName logicalName, out DName newDisplayName)
                {
                    newDisplayName = displayName;
                    return TryGetLogicalName(displayName, out logicalName);
                }
            }
        }

        public DisplayNameTests()
            : base()
        {
            _engine = new Engine(new PowerFxConfig(CultureInfo.InvariantCulture));
        }

        private readonly Engine _engine;

        [Fact]
        public void CollisionsThrow()
        {
            var r1 = RecordType.Empty()
                .Add(new NamedFormulaType("Num", FormulaType.Number, new DName("DisplayNum")));

            Assert.Throws<NameCollisionException>(() => r1.Add(new NamedFormulaType("DisplayNum", FormulaType.Date, "NoCollision")));
            Assert.Throws<NameCollisionException>(() => r1.Add(new NamedFormulaType("NoCollision", FormulaType.Date, "DisplayNum")));
            Assert.Throws<NameCollisionException>(() => r1.Add(new NamedFormulaType("NoCollision", FormulaType.Date, "Num")));
        }

        [Fact]
        public void ImmutableDisplayNameProvider()
        {
            var r1 = RecordType.Empty();

            var r2 = r1.Add(new NamedFormulaType("Logical", FormulaType.String, "Foo"));
            var r3 = r1.Add(new NamedFormulaType("Logical", FormulaType.String, "Bar"));

            Assert.False(ReferenceEquals(r2._type.DisplayNameProvider, r3._type.DisplayNameProvider));
        }

        [Fact]
        public void DisableDisplayNames()
        {
            var r1 = RecordType.Empty()
                .Add(new NamedFormulaType("Logical", FormulaType.String, "Foo"));

            var r2 = RecordType.Empty()
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
        [InlineData("Sum(NestedDisplay, ThisRecord.InnerDisplay)", "Sum(Nested, ThisRecord.Inner)", false)]
        public void ValidateDisplayNames(string inputExpression, string outputExpression, bool toDisplay)
        {
            var r1 = RecordType.Empty()
                .Add(new NamedFormulaType("Num", FormulaType.Number, "DisplayNum"))
                .Add(new NamedFormulaType("B", FormulaType.Boolean, "DisplayB"))
                .Add(new NamedFormulaType(
                    "Nested", 
                    TableType.Empty().Add(new NamedFormulaType("Inner", FormulaType.Number, "InnerDisplay")), 
                    "NestedDisplay"));

            // Below Record r2 Tests the second method where we provide DisplayNameProvider via constructor to 
            // initialize the DisplayNameProvider for derived record types.
            var r2 = new LazyRecordType();

            var records = new RecordType[] { r1, r2 };

            foreach (var record in records)
            {
                var result = _engine.Check(inputExpression, record);
                Assert.True(result.IsSuccess);

                if (toDisplay)
                {
                    var outDisplayExpression = _engine.GetDisplayExpression(inputExpression, record);
                    Assert.Equal(outputExpression, outDisplayExpression);
                }
                else
                {
                    var outInvariantExpression = _engine.GetInvariantExpression(inputExpression, record);
                    Assert.Equal(outputExpression, outInvariantExpression);
                }
            }
        }

        [Fact]
        public void ConvertToDisplayNamesNoNames()
        {
            var r1 = RecordType.Empty()
                .Add(new NamedFormulaType("Num", FormulaType.Number))
                .Add(new NamedFormulaType("B", FormulaType.Boolean));

            var displayExpressions = _engine.GetDisplayExpression("If(B, Num, 1234)", r1);

            Assert.Equal("If(B, Num, 1234)", displayExpressions);
        }

        [Fact]
        public void ConvertToInvariantNamesNoNames()
        {
            var r1 = RecordType.Empty()
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
        [InlineData("firstos.option_1 <> Os1Value", "firstos.option_1 <> Os1ValueRenamed", "Os1Value", "Os1ValueRenamed")] // Globals
        [InlineData("TestSecondOptionSet.Option3 = DisplayOS2Value", "secondos.option_3 = Os2ValueRenamed", "Os2Value", "Os2ValueRenamed")]
        [InlineData("If(false, TestSecondOptionSet.Option4, Os2Value)", "If(false, secondos.option_4, Os2ValueRenamed)", "Os2Value", "Os2ValueRenamed")]
        public void RenameParameter(string expressionBase, string expectedExpression, string path, string newName)
        {
            var config = new PowerFxConfig(CultureInfo.InvariantCulture);
            var optionSet1 = new OptionSet("firstos", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));

            config.AddOptionSet(optionSet1, new DName("TestFirstOptionSet"));
            var optionSet2 = new OptionSet("secondos", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_3", "Option3" },
                    { "option_4", "Option4" }
            }));
            config.AddOptionSet(optionSet2, new DName("TestSecondOptionSet"));

            var r1 = RecordType.Empty()
                .Add(new NamedFormulaType("Num", FormulaType.Number, "DisplayNum"))
                .Add(new NamedFormulaType("B", FormulaType.Boolean, "DisplayB"))
                .Add(new NamedFormulaType("Os1Value", optionSet1.FormulaType, "DisplayOS1Value"))
                .Add(new NamedFormulaType("Os2Value", optionSet2.FormulaType, "DisplayOS2Value"))
                .Add(new NamedFormulaType(
                        "Nested",
                        TableType.Empty().Add(new NamedFormulaType("Inner", FormulaType.Number, "InnerDisplay")),
                        "NestedDisplay"))
                .Add(new NamedFormulaType(
                        "RecordNest",
                        RecordType.Empty()
                            .Add(new NamedFormulaType("SomeString", FormulaType.String, "DisplaySomeString"))
                            .Add(new NamedFormulaType("nest2", RecordType.Empty().Add(new NamedFormulaType("datetest", FormulaType.DateTime, "DisplayDT")), "DisplayReallyNested")),
                        "superrecordnest"));

            var dpath = DPath.Root;
            foreach (var segment in path.Split('.'))
            {
                dpath = dpath.Append(new DName(segment));
            }

            var engine = new Engine(config);

            var renamer = engine.CreateFieldRenamer(r1, dpath, new DName(newName));

            Assert.Equal(expectedExpression, renamer.ApplyRename(expressionBase));
        }

        [Fact]
        public void RenameLazyRecord()
        {
            var engine = new Engine(new PowerFxConfig(CultureInfo.InvariantCulture));

            var renamer = engine.CreateFieldRenamer(
                new BindingEngineTests.LazyRecursiveRecordType(),
                DPath.Root.Append(new DName("Loop")).Append(new DName("SomeString")),
                new DName("Var"));

            Assert.Equal("Loop.Var = \"1\"", renamer.ApplyRename("Loop.SomeString = \"1\""));
        }

        [Fact]
        public void RenameLazyRecordReusedTypes()
        {
            var engine = new Engine(new PowerFxConfig(CultureInfo.InvariantCulture));

            var renamer = engine.CreateFieldRenamer(
                new BindingEngineTests.LazyRecursiveRecordType(),
                DPath.Root.Append(new DName("Loop")).Append(new DName("Loop")).Append(new DName("Loop")).Append(new DName("Loop")).Append(new DName("Loop")),
                new DName("Var"));

            Assert.Equal("Var.Var.SomeString = \"1\"", renamer.ApplyRename("Loop.Loop.SomeString = \"1\""));
        }

        [Fact]
        public void ConvertToDisplayNotForced()
        {
            var r1 = RecordType.Empty()
                .Add(new NamedFormulaType("Num", FormulaType.Number, "SomeDisplayNum"))
                .Add(new NamedFormulaType("B", FormulaType.Boolean, "SomeDisplayB"));

            var formula = new Formula("If(SomeDisplayB, SomeDisplayNum, 1234)", CultureInfo.InvariantCulture);
            formula.EnsureParsed(TexlParser.Flags.None);

            var binding = TexlBinding.Run(
                new Glue2DocumentBinderGlue(),
                null,
                new Core.Entities.QueryOptions.DataSourceToQueryOptionsMap(),
                formula.ParseTree,
                new SymbolTable(),
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
            var r1 = RecordType.Empty()
                .Add(new NamedFormulaType("Num", FormulaType.Number, "DisplayNum"))
                .Add(new NamedFormulaType("B", FormulaType.Boolean, "DisplayB"))
                .Add(new NamedFormulaType(
                    "Nested",
                    TableType.Empty().Add(new NamedFormulaType("Inner", FormulaType.Number, "InnerDisplay")),
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

        [Theory]
        [InlineData("r1.Display1", true)]
        [InlineData("If(true, r1).Display1", true)]
        [InlineData("If(true, r1, r1).Display1", true)]
        [InlineData("If(true, Blank(), r1).Display1", true)]

        // If types are different, you have no access to Display name.
        [InlineData("If(true, r1, r2).Display1", false)]
        [InlineData("If(true, r1, r2).Display0", false)]
        [InlineData("If(true, r1, {Display1 : 123}).Display1", false)]

        // If types are different, you have access to logical name, only if the name and type are same!
        [InlineData("If(true, r1, r2).F1", true)]
        [InlineData("If(false, r1, r2).F1", true)]
        [InlineData("If(true, r1, r2).F0", false)]
        public void DisplayNameTest(string input, bool succeeds)
        {
            var r1 = RecordType.Empty()
                        .Add(new NamedFormulaType("F1", FormulaType.Number, "Display1"))    
                        .Add(new NamedFormulaType("F0", FormulaType.String, "Display0")); // F0 is Not a Common type

            var r2 = RecordType.Empty()
                        .Add(new NamedFormulaType("F1", FormulaType.Number, "Display1"))
                        .Add(new NamedFormulaType("F0", FormulaType.Number, "Display0"));
            var parameters = RecordType.Empty()
                .Add("r1", r1)
                .Add("r2", r2);

            var engine = new Engine(new PowerFxConfig());

            var result = engine.Check(input, parameters);
            var actual = result.IsSuccess;
            Assert.Equal(succeeds, actual);
        }
    }
}
