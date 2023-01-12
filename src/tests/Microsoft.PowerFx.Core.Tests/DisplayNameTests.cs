// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
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
                    "Nested" => TableType.Empty()
                        .Add(new NamedFormulaType("Inner", FormulaType.Number, "InnerDisplay"))
                        .Add(new NamedFormulaType("DisplayNum", FormulaType.Number, "InnerLogicalConflicts")),
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

            // Collision on symbol table
            var symbol = new SymbolTable();

            // Adds display name for a variable.
            symbol.AddVariable("logicalVariable", FormulaType.Number, displayName: "displayVariable");
            symbol.AddVariable("logicalVariable2", FormulaType.Number, displayName: "displayVariable2");

            // should throw if try to add same name constant
            Assert.Throws<NameCollisionException>(() => symbol.AddConstant("logicalVariable", FormulaValue.New(1)));
            Assert.Throws<NameCollisionException>(() => symbol.AddConstant("displayVariable", FormulaValue.New(1)));

            // should be able to remove variable using display name
            Assert.Throws<NameCollisionException>(() => symbol.AddConstant("logicalVariable2", FormulaValue.New(1)));
            Assert.Throws<NameCollisionException>(() => symbol.AddConstant("displayVariable2", FormulaValue.New(1)));
            symbol.RemoveVariable("displayVariable2");
            symbol.AddConstant("logicalVariable2", FormulaValue.New(1));
            symbol.AddConstant("displayVariable2", FormulaValue.New(1));

            var config = new PowerFxConfig() { SymbolTable = symbol };

            // should throw if try to add same name entity
            // displayVariable is display name for variable logicalVariable
            var optionSet = new OptionSet("displayVariable", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "foo", "Option1" },
                    { "baz", "foo" }
            }));
            Assert.Throws<NameCollisionException>(() => config.AddEntity(optionSet, new DName("newName")));

            // logicalVariable is logical name for variable logicalVariable.
            var optionSet2 = new OptionSet("logicalVariable", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "foo", "Option1" },
                    { "baz", "foo" }
            }));
            Assert.Throws<NameCollisionException>(() => config.AddEntity(optionSet2, new DName("newName")));

            // option set with new name.
            var optionSet3 = new OptionSet("newName", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "foo", "Option1" },
                    { "baz", "foo" }
            }));

            // displayVariable is display name for variable logicalVariable
            Assert.Throws<NameCollisionException>(() => config.AddEntity(optionSet3, new DName("displayVariable")));

            // logicalVariable is logical name for variable logicalVariable
            Assert.Throws<NameCollisionException>(() => config.AddEntity(optionSet3, new DName("logicalVariable")));

            // Remove variable and remove from display name as well.
            symbol.RemoveVariable("logicalVariable");

            // Now below should not throw an exception.
            config.AddEntity(optionSet3, new DName("displayVariable"));
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
        [InlineData("First(Nested.Inner).Inner", "First(NestedDisplay.InnerDisplay).InnerDisplay", true)]
        [InlineData("First(Nested).DisplayNum", "First(NestedDisplay).InnerLogicalConflicts", true)]
        [InlineData("If(DisplayB, DisplayNum, 1234)", "If(B, Num, 1234)", false)]
        [InlineData("If(B, Num, 1234)", "If(B, Num, 1234)", false)]
        [InlineData("If(DisplayB, Num, 1234)", "If(B, Num, 1234)", false)]
        [InlineData("Sum(NestedDisplay, InnerDisplay)", "Sum(Nested, Inner)", false)]
        [InlineData("Sum(NestedDisplay /* The source */ , InnerDisplay /* Sum over the InnerDisplay column */)", "Sum(Nested /* The source */ , Inner /* Sum over the InnerDisplay column */)", false)]
        [InlineData("Sum(NestedDisplay, ThisRecord.InnerDisplay)", "Sum(Nested, ThisRecord.Inner)", false)]
        [InlineData("First(NestedDisplay.InnerDisplay).InnerDisplay", "First(Nested.Inner).Inner", false)]
        [InlineData("First(NestedDisplay).InnerLogicalConflicts", "First(Nested).DisplayNum", false)]
        [InlineData("First(NestedDisplay).DisplayNum", "First(Nested).DisplayNum", false)]
        public void ValidateDisplayNames(string inputExpression, string outputExpression, bool toDisplay)
        {
            var r1 = RecordType.Empty()
                .Add(new NamedFormulaType("Num", FormulaType.Number, "DisplayNum"))
                .Add(new NamedFormulaType("B", FormulaType.Boolean, "DisplayB"))
                .Add(new NamedFormulaType(
                    "Nested", 
                    TableType.Empty()
                        .Add(new NamedFormulaType("Inner", FormulaType.Number, "InnerDisplay"))
                        .Add(new NamedFormulaType("DisplayNum", FormulaType.Number, "InnerLogicalConflicts")), 
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

        // Not found
        [InlineData("First(Nested).Inner", "First(Nested).Inner", "Nested.Missing", "Bar")]
        [InlineData("First(Nested).Inner", "First(Nested).Inner", "Missing", "Bar")]
        [InlineData("First(Nested).InnerDisplay", "First(Nested).InnerDisplay", "Missing", "Bar")]
        [InlineData("First(Nested).InnerDisplay", "First(Nested).InnerDisplay", "Nested.Missing", "InnerRenamedLogicalName")]
        [InlineData("With({SomeValue: 123}, RecordNest.nest2.datetest)", "With({SomeValue: 123}, RecordNest.nest2.datetest)", "RecordNest.nest2.Missing", "Foo")]
        [InlineData("With({RecordNest: {nest2: {datetest: 123}}}, RecordNest.nest2.datetest)", "With({RecordNest: {nest2: {datetest: 123}}}, RecordNest.nest2.datetest)", "RecordNest.nest2.Missing", "Foo")]
        [InlineData("With(RecordNest, SomeString + nest2.datetest)", "With(RecordNest, SomeString + nest2.datetest)", "Missing", "Foo")]
        [InlineData("TestSecondOptionSet.Option3 = DisplayOS2Value", "TestSecondOptionSet.Option3 = DisplayOS2Value", "Missing", "Os2ValueRenamed")]
        [InlineData("If(false, TestSecondOptionSet.Option4, Os2Value)", "If(false, TestSecondOptionSet.Option4, Os2Value)", "Missing", "Os2ValueRenamed")]
        [InlineData("B & Invalid()", "B & Invalid()", "M", "A")]
        [InlineData("B + + + ", "B + + + ", "M", "A")]
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

            if (renamer.Find(expressionBase))
            {
                Assert.Equal(expectedExpression, renamer.ApplyRename(expressionBase));
            }
            else
            {
                Assert.Equal(engine.GetInvariantExpression(expressionBase, r1), renamer.ApplyRename(expressionBase));
            }
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

        // Verify lookup methods against logical/display names. 
        [Fact]
        public void FieldLookup()
        {
            var r1 = RecordType.Empty()
                .Add(new NamedFormulaType("Num", FormulaType.Number, "SomeDisplayNum"))
                .Add(new NamedFormulaType("B", FormulaType.Boolean, "SomeDisplayB"));

            FormulaType type;
            
            type = r1.GetFieldType("Num");
            Assert.Equal(FormulaType.Number, type);

            // Display name not found because we only lookup logical 
            var found = r1.TryGetFieldType("SomeDisplayNum", out type);
            Assert.False(found);
            Assert.Equal(FormulaType.Blank, type);

            // Lookup to get display name 
            found = r1.TryGetFieldType("Num", out var logical, out type);
            Assert.True(found);
            Assert.Equal(FormulaType.Number, type);
            Assert.Equal("Num", logical);

            // This overload handles display name
            found = r1.TryGetFieldType("SomeDisplayNum", out logical, out type);
            Assert.True(found);
            Assert.Equal(FormulaType.Number, type);
            Assert.Equal("Num", logical);
        }

        [Theory]
        [InlineData("ForAll(Outer, { Inner: 123 })", "ForAll(OuterDisplay, { Inner: 123 })")]
        [InlineData("ForAll(Outer, ForAll(Inner, { OuterField: 123, InnerField: 456 })", "ForAll(OuterDisplay, ForAll(InnerDisplay, { OuterField: 123, InnerField: 456 })")]
        [InlineData("ForAll(Inner, { InnerField: 123 })", "ForAll(InnerDisplay, { InnerField: 123 })")]
        public void ConvertToDisplayNamesForAllNoScopes(string expression, string expected)
        {
            var r1 = RecordType.Empty()
                .Add(new NamedFormulaType(
                        "Inner",
                        TableType.Empty().Add(new NamedFormulaType("InnerField", FormulaType.Number, "InnerFieldDisplay")),
                        "InnerDisplay"))
                .Add(new NamedFormulaType(
                        "Outer",
                        TableType.Empty().Add(new NamedFormulaType("OuterField", FormulaType.Number, "OuterFieldDisplay")),
                        "OuterDisplay"));

            var outDisplayExpression = _engine.GetDisplayExpression(expression, r1);
            Assert.Equal(expected, outDisplayExpression);
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

        [Theory]
        [InlineData("d", "displayName")]
        [InlineData("D", "displayName")]
        [InlineData("di", "displayName")]
        [InlineData("DI", "displayName")]
        [InlineData("dis", "displayName")]
        [InlineData("DIs", "displayName")]
        [InlineData("display", "displayName")]
        [InlineData("displayname", "displayName")]
        [InlineData("l", "logicalB")]
        [InlineData("L", "logicalB")]
        [InlineData("lo", "logicalB")]
        [InlineData("LO", "logicalB")]
        [InlineData("logical", "logicalB")]
        [InlineData("logicalB", "logicalB")]
        public void TestSuggestIdentifier(string txt, string expected)
        {
            var pfxConfig = new PowerFxConfig(Features.SupportColumnNamesAsIdentifiers);
            var recalcEngine = new Engine(pfxConfig);
            var rt = RecordType.Empty()
                .Add(new NamedFormulaType("logicalA", FormulaType.Number, displayName: "displayName"))
                .Add(new NamedFormulaType("logicalB", FormulaType.Number));

            var intellisenseResult = recalcEngine.Suggest($"DropColumns(myTable, {txt}", rt, 21 + txt.Length);

            Assert.NotNull(intellisenseResult);
            Assert.NotNull(intellisenseResult.Suggestions);
            Assert.True(intellisenseResult.Suggestions.Any());

            var intellisenseSuggestion = intellisenseResult.Suggestions.FirstOrDefault(s => s.DisplayText.Text == expected) as IntellisenseSuggestion;

            Assert.NotNull(intellisenseSuggestion);
            Assert.Equal(expected, intellisenseSuggestion.Text);
            Assert.Equal(DType.Number, intellisenseSuggestion.Type);
        }
    }
}
