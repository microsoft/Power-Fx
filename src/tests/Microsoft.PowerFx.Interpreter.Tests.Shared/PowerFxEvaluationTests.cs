// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter.Tests.Helpers;
using Microsoft.PowerFx.Logging;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class ExpressionEvaluationTests : PowerFxTest
    {
        // Each setup handler can define 4 types of specific actions to define the PowerFxConfig, Parameters and Engine configuration
        // - initPfxConfig: to define the initial PowerFxConfig
        // - updatePfxConfig: to update the PowerFxConfig, like enabling functions
        //                    this function returns an 'object' which will be sent to 'parameters' function if defined
        //                    if initPfxConfig is null, PowerFxConfig is created with default settings
        // - parameters: to define the parameters for the test case
        //               receives an 'object' if defined in updatePfxConfig
        // - configureEngine: to configure the engine               
        // Each of these actions are executed in order (init, update, param, configure)
        internal static Dictionary<string, 
            (Func<PowerFxConfig, PowerFxConfig> initPfxConfig, 
             Func<PowerFxConfig, SymbolTable, object> updatePfxConfig, 
             Func<object, RecordValue> parameters, 
             Action<RecalcEngine, bool> configureEngine)> SetupHandlers = new ()
        {
            { "AllEnumsSetup", (AllEnumsSetup, null, null, null) },
            { "AllEnumsPlusTestEnumsSetup", (AllEnumsPlusTestEnumsSetup, null, null, null) },
            { "AllEnumsPlusTestOptionSetsSetup", (AllEnumsPlusTestOptionSetsSetup, null, null, null) },
            { "Blob", (null, BlobSetup, null, null) },
            { "DecimalSupport", (null, null, null, null) }, // Decimal is enabled in the C# interpreter
            { "EnableJsonFunctions", (null, EnableJsonFunctions, null, null) },
            { "MutationFunctionsTestSetup", (null, null, null, MutationFunctionsTestSetup) },
            { "OptionSetSortTestSetup", (null, OptionSetSortTestSetup, null, null) },
            { "OptionSetTestSetup", (null, OptionSetTestSetup1, OptionSetTestSetup2, null) },
            { "RegEx", (null, RegExSetup, null, null) },
            { "TraceSetup", (null, null, null, TraceSetup) },
        };

        private static object EnableJsonFunctions(PowerFxConfig config, SymbolTable symbolTable)
        {
            config.EnableJsonFunctions();
            return null;
        }

        private static object RegExSetup(PowerFxConfig config, SymbolTable symbolTable)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            config.EnableRegExFunctions(new TimeSpan(0, 0, 5));
#pragma warning restore CS0618 // Type or member is obsolete

            return null;
        }       

        private static object BlobSetup(PowerFxConfig config, SymbolTable symbolTable)
        {
            config.AddBlobTestFunctions();
            config.EnableSetFunction();

            return new List<(ISymbolSlot slot, FormulaValue value)>()
            { 
                AddBlankVar(symbolTable, FormulaType.Blob, "blob", true),
                AddBlankVar(symbolTable, FormulaType.String, "str", true)
            };
        }

        private static (ISymbolSlot slot, FormulaValue value) AddBlankVar(SymbolTable symbolTable, FormulaType type, string varName, bool mutable)
        {
            return (symbolTable.AddVariable(varName, type, mutable, varName), FormulaValue.NewBlank(type));
        }

        private static PowerFxConfig AllEnumsSetup(PowerFxConfig config)
        {
            var store = new EnumStoreBuilder().WithDefaultEnums();
            var newConfig = PowerFxConfig.BuildWithEnumStore(store, new TexlFunctionSet(), config.Features);

            // There are likewise no built in functions that take Boolean backed option sets as parameters
            newConfig.AddFunction(new TestXORBooleanFunction());
            newConfig.AddFunction(config.Features.StronglyTypedBuiltinEnums ? new STE_TestXORYesNoFunction() : new Boolean_TestXORYesNoFunction());
            newConfig.AddFunction(new TestColorInvertFunction());
            newConfig.AddFunction(config.Features.StronglyTypedBuiltinEnums ? new STE_TestColorBlueRampInvertFunction() : new Color_TestColorBlueRampInvertFunction());

            return newConfig;
        }

        private static PowerFxConfig AllEnumsPlusTestOptionSetsSetup(PowerFxConfig config)
        {
            var store = new EnumStoreBuilder().WithDefaultEnums();
            var newConfig = PowerFxConfig.BuildWithEnumStore(store, new TexlFunctionSet(), config.Features);

            // There are no built in enums with boolean values and only one with colors.  Adding these for testing purposes.
            newConfig.AddEntity(_testYesNo_OptionSet, _testYesNo_OptionSet.EntityName);
            newConfig.AddEntity(_testYeaNay_OptionSet, _testYeaNay_OptionSet.EntityName);
            newConfig.AddEntity(_testBooleanNoCoerce_OptionSet, _testBooleanNoCoerce_OptionSet.EntityName);
            newConfig.AddEntity(_testNumberCoerceTo_OptionSet, _testNumberCoerceTo_OptionSet.EntityName);
            newConfig.AddEntity(_testNumberCompareNumeric_OptionSet, _testNumberCompareNumeric_OptionSet.EntityName);
            newConfig.AddEntity(_testNumberCompareNumericCoerceFrom_OptionSet, _testNumberCompareNumericCoerceFrom_OptionSet.EntityName);
            newConfig.AddEntity(_testBlueRampColors_OptionSet, _testBlueRampColors_OptionSet.EntityName);
            newConfig.AddEntity(_testRedRampColors_OptionSet, _testRedRampColors_OptionSet.EntityName);

            // There are likewise no built in functions that take Boolean backed option sets as parameters
            newConfig.AddFunction(new TestXORBooleanFunction());
            newConfig.AddFunction(config.Features.StronglyTypedBuiltinEnums ? new STE_TestXORYesNo_OptionSetFunction() : new Boolean_TestXORYesNoFunction());
            newConfig.AddFunction(config.Features.StronglyTypedBuiltinEnums ? new STE_TestXORNoCoerce_OptionSetFunction() : new Boolean_TestXORNoCoerceFunction());
            newConfig.AddFunction(new TestColorInvertFunction());
            newConfig.AddFunction(config.Features.StronglyTypedBuiltinEnums ? new STE_TestColorBlueRampInvert_OptionSetFunction() : new Color_TestColorBlueRampInvertFunction());
            newConfig.AddFunction(new TestSignDecimalFunction());
            newConfig.AddFunction(new TestSignNumberFunction());

            return newConfig;
        }

        private static readonly BooleanOptionSet _testYesNo_OptionSet = new BooleanOptionSet(
            "TestYesNo",
            new Dictionary<bool, DName>()
            {
                { true, new DName("Yes") },
                { false, new DName("No") }
            }.ToImmutableDictionary(),
            canCoerceFromBackingKind: true,
            canCoerceToBackingKind: true);

        private static readonly BooleanOptionSet _testYeaNay_OptionSet = new BooleanOptionSet(
            "TestYeaNay",
            new Dictionary<bool, DName>()
            {
                { true, new DName("Yea") },
                { false, new DName("Nay") }
            }.ToImmutableDictionary(),
            canCoerceFromBackingKind: true,
            canCoerceToBackingKind: true);

        private static readonly BooleanOptionSet _testBooleanNoCoerce_OptionSet = new BooleanOptionSet(
            "TestBooleanNoCoerce",
            new Dictionary<bool, DName>()
            {
                { true, new DName("SuperTrue") },
                { false, new DName("SuperFalse") }
            }.ToImmutableDictionary());

        private static readonly NumberOptionSet _testNumberCoerceTo_OptionSet = new NumberOptionSet(
            "TestNumberCoerceTo",
            new Dictionary<double, DName>()
            {
                { 10D, new DName("X") },
                { 5D, new DName("V") },
                { 1D, new DName("I") }
            }.ToImmutableDictionary(),
            canCoerceToBackingKind: true);

        private static readonly NumberOptionSet _testNumberCompareNumeric_OptionSet = new NumberOptionSet(
            "TestNumberCompareNumeric",
            new Dictionary<double, DName>()
            {
                { 10D, new DName("X") },
                { 5D, new DName("V") },
                { 1D, new DName("I") }
            }.ToImmutableDictionary(),
            canCompareNumeric: true);

        private static readonly NumberOptionSet _testNumberCompareNumericCoerceFrom_OptionSet = new NumberOptionSet(
            "TestNumberCompareNumericCoerceFrom",
            new Dictionary<double, DName>()
            {
                { 10D, new DName("X") },
                { 5D, new DName("V") },
                { 1D, new DName("I") }
            }.ToImmutableDictionary(),
            canCompareNumeric: true,
            canCoerceFromBackingKind: true);

        private static readonly ColorOptionSet _testBlueRampColors_OptionSet = new ColorOptionSet(
            "TestBlueRamp",
            new Dictionary<double, DName>()
            {
                { (double)0xFF0000FFU, new DName("Blue100") },
                { (double)0xFF3F3FFFU, new DName("Blue75") },
                { (double)0xFF7F7FFFU, new DName("Blue50") },
                { (double)0xFFBFBFFFU, new DName("Blue25") },
                { (double)0xFFFFFFFFU, new DName("Blue0") }
            }.ToImmutableDictionary());

        private static readonly ColorOptionSet _testRedRampColors_OptionSet = new ColorOptionSet(
            "TestRedRamp",
            new Dictionary<double, DName>()
            {
                { (double)0xFFFF0000U, new DName("Red100") },
                { (double)0xFFFF3F3FU, new DName("Red75") },
                { (double)0xFFFF7F7FU, new DName("Red50") },
                { (double)0xFFFFBFBFU, new DName("Red25") },
                { (double)0xFFFFFFFFU, new DName("Red0") }
            }.ToImmutableDictionary());

        private static PowerFxConfig AllEnumsPlusTestEnumsSetup(PowerFxConfig config)
        {
            var store = new EnumStoreBuilder().WithDefaultEnums();

            // There are no built in enums with boolean values and only one with colors.  Adding these for testing purposes.
            store.TestOnly_WithCustomEnum(_testYesNo, append: true);
            store.TestOnly_WithCustomEnum(_testYeaNay, append: true);
            store.TestOnly_WithCustomEnum(_testBooleanNoCoerce, append: true);
            store.TestOnly_WithCustomEnum(_testNumberCoerceTo, append: true);
            store.TestOnly_WithCustomEnum(_testNumberCompareNumeric, append: true);
            store.TestOnly_WithCustomEnum(_testNumberCompareNumericCoerceFrom, append: true);
            store.TestOnly_WithCustomEnum(_testBlueRampColors, append: true);
            store.TestOnly_WithCustomEnum(_testRedRampColors, append: true);

            var newConfig = PowerFxConfig.BuildWithEnumStore(store, new TexlFunctionSet(), config.Features);

            // There are likewise no built in functions that take Boolean backed option sets as parameters
            newConfig.AddFunction(new TestXORBooleanFunction());
            newConfig.AddFunction(config.Features.StronglyTypedBuiltinEnums ? new STE_TestXORYesNoFunction() : new Boolean_TestXORYesNoFunction());
            newConfig.AddFunction(config.Features.StronglyTypedBuiltinEnums ? new STE_TestXORNoCoerceFunction() : new Boolean_TestXORNoCoerceFunction());
            newConfig.AddFunction(new TestColorInvertFunction());
            newConfig.AddFunction(config.Features.StronglyTypedBuiltinEnums ? new STE_TestColorBlueRampInvertFunction() : new Color_TestColorBlueRampInvertFunction());
            newConfig.AddFunction(new TestSignDecimalFunction());
            newConfig.AddFunction(new TestSignNumberFunction());

            return newConfig;
        }

        private static readonly EnumSymbol _testYesNo = new EnumSymbol(
                    new DName("TestYesNo"),
                    DType.Boolean,
                    new Dictionary<string, object>()
                    {
                        { "Yes", true },
                        { "No", false }
                    },
                    canCoerceFromBackingKind: true,
                    canCoerceToBackingKind: true);

        private static readonly EnumSymbol _testYeaNay = new EnumSymbol(
                    new DName("TestYeaNay"),
                    DType.Boolean,
                    new Dictionary<string, object>()
                    {
                        { "Yea", true },
                        { "Nay", false }
                    },
                    canCoerceFromBackingKind: true,
                    canCoerceToBackingKind: true);

        private static readonly EnumSymbol _testBooleanNoCoerce = new EnumSymbol(
                    new DName("TestBooleanNoCoerce"),
                    DType.Boolean,
                    new Dictionary<string, object>()
                    {
                        { "SuperTrue", true },
                        { "SuperFalse", false }
                    });

        private static readonly EnumSymbol _testNumberCoerceTo = new EnumSymbol(
                    new DName("TestNumberCoerceTo"),
                    DType.Number,
                    new Dictionary<string, object>()
                    {
                        { "X", 10 },
                        { "V", 5 },
                        { "V2", 5 }, // intentionally the same value, should compare on value and not label
                        { "I", 1 }
                    },
                    canCoerceToBackingKind: true);

        private static readonly EnumSymbol _testNumberCompareNumeric = new EnumSymbol(
                    new DName("TestNumberCompareNumeric"),
                    DType.Number,
                    new Dictionary<string, object>()
                    {
                        { "X", 10 },
                        { "V", 5 },
                        { "V2", 5 }, // intentionally the same value, should compare on value and not label
                        { "I", 1 }
                    },
                    canCompareNumeric: true);

        private static readonly EnumSymbol _testNumberCompareNumericCoerceFrom = new EnumSymbol(
                    new DName("TestNumberCompareNumericCoerceFrom"),
                    DType.Number,
                    new Dictionary<string, object>()
                    {
                        { "X", 10 },
                        { "V", 5 },
                        { "V2", 5 }, // intentionally the same value, should compare on value and not label
                        { "I", 1 }
                    },
                    canCompareNumeric: true,
                    canCoerceFromBackingKind: true);

        private static readonly EnumSymbol _testBlueRampColors = new EnumSymbol(
                    new DName("TestBlueRamp"),
                    DType.Color,
                    new Dictionary<string, object>()
                    {
                        { "Blue100", (double)0xFF0000FFU },
                        { "Blue75", (double)0xFF3F3FFFU },
                        { "Blue50", (double)0xFF7F7FFFU },
                        { "Blue25", (double)0xFFBFBFFFU },
                        { "Blue0", (double)0xFFFFFFFFU }
                    });

        private static readonly EnumSymbol _testRedRampColors = new EnumSymbol(
                    new DName("TestRedRamp"),
                    DType.Color,
                    new Dictionary<string, object>()
                    {
                        { "Red100", (double)0xFFFF0000U },
                        { "Red75", (double)0xFFFF3F3FU },
                        { "Red50", (double)0xFFFF7F7FU },
                        { "Red25", (double)0xFFFFBFBFU },
                        { "Red0", (double)0xFFFFFFFFU }
                    });

        private class TestXORBooleanFunction : ReflectionFunction
        {
            public TestXORBooleanFunction()
                : base("TestXORBoolean", FormulaType.Boolean, new[] { FormulaType.Boolean, FormulaType.Boolean })
            {
            }

            public FormulaValue Execute(BooleanValue x, BooleanValue y)
            {
                return BooleanValue.New(x.Value ^ y.Value);
            }
        }

        private class TestSignNumberFunction : ReflectionFunction
        {
            public TestSignNumberFunction()
                : base("TestSignNumber", FormulaType.Number, new[] { FormulaType.Number })
            {
            }

            public FormulaValue Execute(NumberValue x)
            {
                return NumberValue.New((double)Math.Sign(x.Value));
            }
        }

        private class TestSignDecimalFunction : ReflectionFunction
        {
            public TestSignDecimalFunction()
                : base("TestSignDecimal", FormulaType.Decimal, new[] { FormulaType.Decimal })
            {
            }

            public FormulaValue Execute(DecimalValue x)
            {
                return DecimalValue.New((decimal)Math.Sign(x.Value));
            }
        }

        private class TestColorInvertFunction : ReflectionFunction
        {
            public TestColorInvertFunction()
                : base("TestColorInvert", FormulaType.Color, new[] { FormulaType.Color })
            {
            }

            public FormulaValue Execute(ColorValue x)
            {
                return ColorValue.New(System.Drawing.Color.FromArgb(x.Value.A, x.Value.R ^ 0xff, x.Value.G ^ 0xff, x.Value.B ^ 0xff));
            }
        }

        private class STE_TestColorBlueRampInvertFunction : ReflectionFunction
        {
            public STE_TestColorBlueRampInvertFunction()
                : base("TestColorBlueRampInvert", FormulaType.Color, new[] { _testBlueRampColors.FormulaType })
            {
            }

            public FormulaValue Execute(OptionSetValue x)
            {
                var value = Convert.ToUInt32((double)x.ExecutionValue);
                var c = Color.FromArgb(
                            (byte)((value >> 24) & 0xFF),
                            (byte)((value >> 16) & 0xFF),
                            (byte)((value >> 8) & 0xFF),
                            (byte)(value & 0xFF));
                return ColorValue.New(Color.FromArgb(c.A, c.R ^ 0xff, c.G ^ 0xff, c.B ^ 0xff));
            }
        }

        private class STE_TestColorBlueRampInvert_OptionSetFunction : ReflectionFunction
        {
            public STE_TestColorBlueRampInvert_OptionSetFunction()
                : base("TestColorBlueRampInvert", FormulaType.Color, new[] { _testBlueRampColors_OptionSet.FormulaType })
            {
            }

            public FormulaValue Execute(OptionSetValue x)
            {
                var value = Convert.ToUInt32((double)x.ExecutionValue);
                var c = Color.FromArgb(
                            (byte)((value >> 24) & 0xFF),
                            (byte)((value >> 16) & 0xFF),
                            (byte)((value >> 8) & 0xFF),
                            (byte)(value & 0xFF));
                return ColorValue.New(Color.FromArgb(c.A, c.R ^ 0xff, c.G ^ 0xff, c.B ^ 0xff));
            }
        }

        private class Color_TestColorBlueRampInvertFunction : ReflectionFunction
        {
            public Color_TestColorBlueRampInvertFunction()
                : base("TestColorBlueRampInvert", FormulaType.Color, new[] { FormulaType.Color })
            {
            }

            public FormulaValue Execute(ColorValue x)
            {
                return ColorValue.New(Color.FromArgb(x.Value.A, x.Value.R ^ 0xff, x.Value.G ^ 0xff, x.Value.B ^ 0xff));
            }
        }

        private class STE_TestXORYesNoFunction : ReflectionFunction
        {
            public STE_TestXORYesNoFunction()
                : base("TestXORYesNo", FormulaType.Boolean, new[] { _testYesNo.FormulaType, _testYesNo.FormulaType })
            {
            }

            public FormulaValue Execute(OptionSetValue x, OptionSetValue y)
            {
                return BooleanValue.New((bool)x.ExecutionValue ^ (bool)y.ExecutionValue);
            }
        }

        private class STE_TestXORYesNo_OptionSetFunction : ReflectionFunction
        {
            public STE_TestXORYesNo_OptionSetFunction()
                : base("TestXORYesNo", FormulaType.Boolean, new[] { _testYesNo_OptionSet.FormulaType, _testYesNo_OptionSet.FormulaType })
            {
            }

            public FormulaValue Execute(OptionSetValue x, OptionSetValue y)
            {
                return BooleanValue.New((bool)x.ExecutionValue ^ (bool)y.ExecutionValue);
            }
        }

        // Reflection functions don't know how to coerce an enum to a Boolean, if STE is turned off
        private class Boolean_TestXORYesNoFunction : ReflectionFunction
        {
            public Boolean_TestXORYesNoFunction()
                : base("TestXORYesNo", FormulaType.Boolean, new[] { FormulaType.Boolean, FormulaType.Boolean })
            {
            }

            public FormulaValue Execute(BooleanValue x, BooleanValue y)
            {
                return BooleanValue.New(x.Value ^ y.Value);
            }
        }

        private class STE_TestXORNoCoerceFunction : ReflectionFunction
        {
            public STE_TestXORNoCoerceFunction()
                : base("TestXORNoCoerce", FormulaType.Boolean, new[] { _testBooleanNoCoerce.FormulaType, _testBooleanNoCoerce.FormulaType })
            {
            }

            public FormulaValue Execute(OptionSetValue x, OptionSetValue y)
            {
                return BooleanValue.New((bool)x.ExecutionValue ^ (bool)y.ExecutionValue);
            }
        }

        private class STE_TestXORNoCoerce_OptionSetFunction : ReflectionFunction
        {
            public STE_TestXORNoCoerce_OptionSetFunction()
                : base("TestXORNoCoerce", FormulaType.Boolean, new[] { _testBooleanNoCoerce_OptionSet.FormulaType, _testBooleanNoCoerce_OptionSet.FormulaType })
            {
            }

            public FormulaValue Execute(OptionSetValue x, OptionSetValue y)
            {
                return BooleanValue.New((bool)x.ExecutionValue ^ (bool)y.ExecutionValue);
            }
        }

        private class Boolean_TestXORNoCoerceFunction : ReflectionFunction
        {
            public Boolean_TestXORNoCoerceFunction()
                : base("TestXORNoCoerce", FormulaType.Boolean, new[] { FormulaType.Boolean, FormulaType.Boolean })
            {
            }

            public FormulaValue Execute(BooleanValue x, BooleanValue y)
            {
                return BooleanValue.New(x.Value ^ y.Value);
            }
        }

        private static object OptionSetTestSetup1(PowerFxConfig config, SymbolTable symbolTable)
        {
            OptionSet optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" },
                    { "option-3", "Option-3" },
                    { "Is", "Option IS" }, // Reserved word
                    { "Self", "Option SELF" }, // Keyword
                    { "is", "Option is low case" }, // Not a reserved word
                    { "self", "Option self low case" }, // Not a keyword
            }));

            OptionSet otherOptionSet = new OptionSet("OtherOptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "99", "OptionA" },
                    { "112", "OptionB" },
                    { "35694", "OptionC" },
                    { "123412983", "OptionD" },
            }));

            config.AddOptionSet(optionSet);
            config.AddOptionSet(otherOptionSet);

            return (optionSet, otherOptionSet);
        }

        private static RecordValue OptionSetTestSetup2(object obj)
        {
            (OptionSet optionSet, OptionSet otherOptionSet) = ((OptionSet, OptionSet))obj;

            optionSet.TryGetValue(new DName("option_1"), out var o1Val);
            otherOptionSet.TryGetValue(new DName("123412983"), out var o2Val);

            RecordValue parameters = FormulaValue.NewRecordFromFields(
                    new NamedValue("TopOptionSetField", o1Val),
                    new NamedValue("Nested", FormulaValue.NewRecordFromFields(
                        new NamedValue("InnerOtherOptionSet", o2Val))));

            return parameters;
        }

        private static object OptionSetSortTestSetup(PowerFxConfig config, SymbolTable symbolTable)
        {
            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));

            config.AddOptionSet(optionSet);

            optionSet.TryGetValue(new DName("option_1"), out var o1Val);
            optionSet.TryGetValue(new DName("option_2"), out var o2Val);

            var r1 = FormulaValue.NewRecordFromFields(new NamedValue("OptionSetField1", o1Val), new NamedValue("StrField1", FormulaValue.New("test1")));
            var r2 = FormulaValue.NewRecordFromFields(new NamedValue("OptionSetField1", o2Val), new NamedValue("StrField1", FormulaValue.New("test2")));
            var r3 = FormulaValue.NewRecordFromFields(new NamedValue("OptionSetField1", o1Val), new NamedValue("StrField1", FormulaValue.New("test3")));
            var r4 = FormulaValue.NewRecordFromFields(new NamedValue("OptionSetField1", o2Val), new NamedValue("StrField1", FormulaValue.New("test4")));

            // Testing with missing/blank option set field is throwing an exception. Once that is resolved uncomment and fix the test case in Sort.txt
            var r5 = FormulaValue.NewRecordFromFields(new NamedValue("StrField1", FormulaValue.New("test5")));

            var rType = RecordType.Empty()
                .Add(new NamedFormulaType("OptionSetField1", FormulaType.OptionSetValue, "DisplayNameField1"))
                .Add(new NamedFormulaType("StrField1", FormulaType.String, "DisplayNameField2"));

            var t1 = FormulaValue.NewTable(rType, r1, r2);
            var t2 = FormulaValue.NewTable(rType, r1, r2, r3, r4);
            var t3 = FormulaValue.NewTable(rType, r1, r2, r3, r5, r4);

            var symbol = config.SymbolTable;
            symbol.AddConstant("t1", t1);
            symbol.AddConstant("t2", t2);
            symbol.AddConstant("t3", t3);

            return null;
        }

        private static void MutationFunctionsTestSetup(RecalcEngine engine, bool numberIsFloat)
        {
            /*
             * Record r1 => {![Field1:n, Field2:s, Field3:d, Field4:b]}
             * Record r2 => {![Field1:n, Field2:s, Field3:d, Field4:b]}
             * Record rwr1 => {![Field1:n, Field2:![Field2_1:n, Field2_2:s, Field2_3:![Field2_3_1:n, Field2_3_2:s]], Field3:b]}
             * Record rwr2 => {![Field1:n, Field2:![Field2_1:n, Field2_2:s, Field2_3:![Field2_3_1:n, Field2_3_2:s]], Field3:b]}
             * Record rwr3 => {![Field1:n, Field2:![Field2_1:n, Field2_2:s, Field2_3:![Field2_3_1:n, Field2_3_2:s]], Field3:b]}
             * Record r_empty => {}
             * Record r3 => ![a:*[b:n]]
             * Table t1(r1) => Type (Field1, Field2, Field3, Field4)
             * Table t2(rwr1, rwr2, rwr3)
             * Table t_empty: *[Value:n] = []
             * Table t_empty2: *[Value:n] = []
             * Table t_an_bs: *[a:n,b:s] = []
             * Table t_name: *[name:s] = []
             * Table t_bs1: *[b:s] 
             * Table t_bs2: *[b:s] 
             */

            var numberType = numberIsFloat ? FormulaType.Number : FormulaType.Decimal;

            Func<double, FormulaValue> newNumber = number => numberIsFloat ? FormulaValue.New(number) : FormulaValue.New((decimal)number);

            var rType = RecordType.Empty()
                .Add(new NamedFormulaType("Field1", numberType, "DisplayNameField1"))
                .Add(new NamedFormulaType("Field2", FormulaType.String, "DisplayNameField2"))
                .Add(new NamedFormulaType("Field3", FormulaType.DateTime, "DisplayNameField3"))
                .Add(new NamedFormulaType("Field4", FormulaType.Boolean, "DisplayNameField4"));

            var r1Fields = new List<NamedValue>()
            {
                new NamedValue("Field1", newNumber(1)),
                new NamedValue("Field2", FormulaValue.New("earth")),
                new NamedValue("Field3", FormulaValue.New(DateTime.Parse("1/1/2022").Date)),
                new NamedValue("Field4", FormulaValue.New(true))
            };

            var r2Fields = new List<NamedValue>()
            {
                new NamedValue("Field1", newNumber(2)),
                new NamedValue("Field2", FormulaValue.New("moon")),
                new NamedValue("Field3", FormulaValue.New(DateTime.Parse("2/1/2022").Date)),
                new NamedValue("Field4", FormulaValue.New(false))
            };

            var r1 = FormulaValue.NewRecordFromFields(rType, r1Fields);
            var r2 = FormulaValue.NewRecordFromFields(rType, r2Fields);
            var rEmpty = RecordValue.Empty();

            var t1 = FormulaValue.NewTable(rType, new List<RecordValue>() { r1 });

#pragma warning disable SA1117 // Parameters should be on same line or separate lines
            var recordWithRecordType = RecordType.Empty()
                .Add(new NamedFormulaType("Field1", numberType, "DisplayNameField1"))
                .Add(new NamedFormulaType("Field2", RecordType.Empty()
                    .Add(new NamedFormulaType("Field2_1", numberType, "DisplayNameField2_1"))
                    .Add(new NamedFormulaType("Field2_2", FormulaType.String, "DisplayNameField2_2"))
                    .Add(new NamedFormulaType("Field2_3", RecordType.Empty()
                        .Add(new NamedFormulaType("Field2_3_1", numberType, "DisplayNameField2_3_1"))
                        .Add(new NamedFormulaType("Field2_3_2", FormulaType.String, "DisplayNameField2_3_2")),
                    "DisplayNameField2_3")),
                "DisplayNameField2"))
                .Add(new NamedFormulaType("Field3", FormulaType.Boolean, "DisplayNameField3"));
#pragma warning restore SA1117 // Parameters should be on same line or separate lines

            var recordWithRecordFields1 = new List<NamedValue>()
            {
                new NamedValue("Field1", newNumber(1)),
                new NamedValue("Field2", FormulaValue.NewRecordFromFields(new List<NamedValue>()
                {
                    new NamedValue("Field2_1", newNumber(121)),
                    new NamedValue("Field2_2", FormulaValue.New("2_2")),
                    new NamedValue("Field2_3", FormulaValue.NewRecordFromFields(new List<NamedValue>()
                    {
                        new NamedValue("Field2_3_1", newNumber(1231)),
                        new NamedValue("Field2_3_2", FormulaValue.New("common")),
                    }))
                })),
                new NamedValue("Field3", FormulaValue.New(false))
            };

            var recordWithRecordFields2 = new List<NamedValue>()
            {
                new NamedValue("Field1", newNumber(2)),
                new NamedValue("Field2", FormulaValue.NewRecordFromFields(new List<NamedValue>()
                {
                    new NamedValue("Field2_1", newNumber(221)),
                    new NamedValue("Field2_2", FormulaValue.New("2_2")),
                    new NamedValue("Field2_3", FormulaValue.NewRecordFromFields(new List<NamedValue>()
                    {
                        new NamedValue("Field2_3_1", newNumber(2231)),
                        new NamedValue("Field2_3_2", FormulaValue.New("common")),
                    }))
                })),
                new NamedValue("Field3", FormulaValue.New(false))
            };

            var recordWithRecordFields3 = new List<NamedValue>()
            {
                new NamedValue("Field1", newNumber(3)),
                new NamedValue("Field2", FormulaValue.NewRecordFromFields(new List<NamedValue>()
                {
                    new NamedValue("Field2_1", newNumber(321)),
                    new NamedValue("Field2_2", FormulaValue.New("2_2")),
                    new NamedValue("Field2_3", FormulaValue.NewRecordFromFields(new List<NamedValue>()
                    {
                        new NamedValue("Field2_3_1", newNumber(3231)),
                        new NamedValue("Field2_3_2", FormulaValue.New("common")),
                    }))
                })),
                new NamedValue("Field3", FormulaValue.New(true))
            };

            var recordWithRecord1 = FormulaValue.NewRecordFromFields(recordWithRecordType, recordWithRecordFields1);
            var recordWithRecord2 = FormulaValue.NewRecordFromFields(recordWithRecordType, recordWithRecordFields2);
            var recordWithRecord3 = FormulaValue.NewRecordFromFields(recordWithRecordType, recordWithRecordFields3);

            var t2 = FormulaValue.NewTable(recordWithRecordType, new List<RecordValue>()
            {
                recordWithRecord1,
                recordWithRecord2,
                recordWithRecord3
            });

            var symbol = engine._symbolTable;

            symbol.EnableMutationFunctions();

            engine.UpdateVariable("t1", t1);
            engine.UpdateVariable("r1", r1);

            engine.UpdateVariable("r2", r2);
            engine.UpdateVariable("t2", t2);
            engine.UpdateVariable("rwr1", recordWithRecord1);
            engine.UpdateVariable("rwr2", recordWithRecord2);
            engine.UpdateVariable("rwr3", recordWithRecord3);
            engine.UpdateVariable("r_empty", rEmpty);

            var valueTableType = TableType.Empty().Add("Value", numberType);
            var tEmpty = FormulaValue.NewTable(valueTableType.ToRecord());
            var tEmpty2 = FormulaValue.NewTable(valueTableType.ToRecord());
            engine.UpdateVariable("t_empty", tEmpty);
            engine.UpdateVariable("t_empty2", tEmpty2);

            var abTableType = TableType.Empty().Add("a", numberType).Add("b", FormulaType.String);
            engine.UpdateVariable("t_an_bs", FormulaValue.NewTable(abTableType.ToRecord()));

            var nameTableType = TableType.Empty().Add("name", FormulaType.String);
            engine.UpdateVariable("t_name", FormulaValue.NewTable(nameTableType.ToRecord()));

            var r3Table = TableType.Empty().Add("b", numberType);
            var r3Type = RecordType.Empty()
                .Add(new NamedFormulaType("a", r3Table, "DisplayNamea"));

            var r3Fields = new List<NamedValue>()
            {
                new NamedValue("a", FormulaValue.NewTable(r3Table.ToRecord())),
            };

            var r3Empty = FormulaValue.NewRecordFromFields(r3Type, r3Fields);
            engine.UpdateVariable("r3", r3Empty);

            var t_bsType = TableType.Empty().Add("b", FormulaType.String);
            engine.UpdateVariable("t_bs1", FormulaValue.NewTable(t_bsType.ToRecord()));
            engine.UpdateVariable("t_bs2", FormulaValue.NewTable(t_bsType.ToRecord()));
        }        

        private static void TraceSetup(RecalcEngine engine, bool numberIsFloat)
        {
            var rType = RecordType.Empty()
                .Add("message", FormulaType.String)
                .Add("severity", FormulaType.Decimal)
                .Add("customRecord", FormulaType.String);

            var rFields = new NamedValue[]
                {
                    new NamedValue("message", FormulaValue.NewBlank()),
                    new NamedValue("severity", FormulaValue.New(0)),
                    new NamedValue("customRecord", FormulaValue.New(string.Empty))
                };

            var rValue = FormulaValue.NewRecordFromFields(rType, rFields);

            engine.UpdateVariable("traceRecord", rValue);
        }

        private class Tracer : ITracer
        {
            private readonly RecordValue _result;

            public Tracer(RecordValue result)
            {
                _result = result;
            }

            public Task LogAsync(string message, TraceSeverity severity, RecordValue customRecord, CancellationToken ct)
            {
                _result.UpdateField("message", FormulaValue.New(message));
                _result.UpdateField("severity", FormulaValue.New((int)severity));
                _result.UpdateField("customRecord", FormulaValue.New(customRecord.ToExpression()));
                return Task.CompletedTask;
            }
        }

        // Interpret each test case independently
        // Supports #setup directives. 
        internal class InterpreterRunner : BaseRunner
        {
            // For async tests, run in special mode. 

            // This does _not_ change evaluation semantics, but does verify .Result isn't called by checking

            // task completion status.. 

            private async Task<FormulaValue> RunVerifyAsync(string expr, PowerFxConfig config, InternalSetup setup)
            {
                var verify = new AsyncVerify();

                // Add Async(),WaitFor() functions 
                var asyncHelper = new AsyncFunctionsHelper(verify);
                config.AddFunction(asyncHelper.GetFunction());

                var waitForHelper = new WaitForFunctionsHelper(verify);
                config.AddFunction(waitForHelper.GetFunction());

                config.EnableSetFunction();
                var engine = new RecalcEngine(config);
                engine.UpdateVariable("varNumber", 9999);

                // Run in special mode that ensures we're not calling .Result
                var result = await verify.EvalAsync(engine, expr, setup).ConfigureAwait(false);
                return result;
            }

            protected override async Task<RunResult> RunAsyncInternal(string expr, string setupHandlerName)
            {
                RecalcEngine engine = null;
                RecordValue parameters = null;
                var iSetup = InternalSetup.Parse(setupHandlerName, Features, NumberIsFloat);

                var config = new PowerFxConfig(features: iSetup.Features);
                config.EnableJsonFunctions();

                if (iSetup.HandlerNames?.Any(hn => string.Equals(hn, "AsyncTestSetup", StringComparison.OrdinalIgnoreCase)) == true)
                {
                    return new RunResult(await RunVerifyAsync(expr, config, iSetup).ConfigureAwait(false));
                }

                List<Action<RuntimeConfig>> runtimeConfiguration = new List<Action<RuntimeConfig>>();                
                SymbolTable symbolTable = new SymbolTable();
                List<(ISymbolSlot slot, FormulaValue value)> slots = new ();

                if (iSetup.HandlerNames != null && iSetup.HandlerNames.Any())
                {
                    // Execute actions in order
                    foreach (var k in iSetup.HandlerNames.Select(hn => SetupHandlers[hn]).OrderBy(kvp => kvp.initPfxConfig != null ? 1 : kvp.updatePfxConfig != null ? 2 : kvp.parameters != null ? 3 : 4))
                    {
                        if (k.initPfxConfig != null)
                        {
                            config = k.initPfxConfig(config);
                        }

                        object o = k.updatePfxConfig?.Invoke(config, symbolTable);

                        if (o is List<(ISymbolSlot slot, FormulaValue value)> slotList)
                        {
                            slots.AddRange(slotList);
                        }

                        if (k.parameters != null)
                        {
                            parameters = k.parameters(o);
                        }

                        if (k.configureEngine != null)
                        {
                            engine ??= new RecalcEngine(config);
                            k.configureEngine(engine, NumberIsFloat);
                        }                                                     
                    }

                    engine ??= new RecalcEngine(config);
                }
                else
                {
                    engine = new RecalcEngine(config);
                    parameters = null;
                }

                if (parameters == null)
                {
                    parameters = RecordValue.Empty();
                }

                var symbolTableFromParams = ReadOnlySymbolTable.NewFromRecord(parameters.Type);
                var combinedSymbolTable = new ComposedReadOnlySymbolTable(symbolTableFromParams, symbolTable);

                // These tests are only run in en-US locale for now
                var options = iSetup.Flags.ToParserOptions(new CultureInfo("en-US"));
                var check = engine.Check(expr, options: options, symbolTable: combinedSymbolTable);                
                if (!check.IsSuccess)
                {
                    return new RunResult(check);
                }

                Log?.Invoke($"IR: {check.PrintIR()}");

                var symbolValuesFromParams = SymbolValues.NewFromRecord(symbolTableFromParams, parameters);
                var symbolValues = new SymbolValues(symbolTable);
                
                foreach ((ISymbolSlot slot, FormulaValue value) in slots)
                {
                    symbolValues.Set(slot, value);                    
                }

                var composedSymbolValues = SymbolValues.Compose(symbolValuesFromParams, symbolValues);
                var runtimeConfig = new RuntimeConfig(composedSymbolValues);

                if (iSetup.TimeZoneInfo != null)
                {
                    runtimeConfig.AddService(iSetup.TimeZoneInfo);
                }

                if (engine.TryGetByName("traceRecord", out _))
                {
                    var traceRecord = engine.GetValue("traceRecord");
                    if (traceRecord != null)
                    {
                        runtimeConfig.AddService<ITracer>(new Tracer((RecordValue)traceRecord));
                    }
                }

                foreach (Action<RuntimeConfig> rc in runtimeConfiguration)
                {
                    rc(runtimeConfig);
                }            

                // Ensure tests can run with governor on. 
                // Some tests that use large memory can disable via:
                //    #SETUP: DisableMemChecks
                if (!iSetup.DisableMemoryChecks)
                {
                    var kbytes = 1000;
                    var mem = new SingleThreadedGovernor(10 * 1000 * kbytes);
                    runtimeConfig.AddService<Governor>(mem);
                }

                var newValue = await check.GetEvaluator().EvalAsync(CancellationToken.None, runtimeConfig).ConfigureAwait(false);

                // UntypedObjectType type is currently not supported for serialization.
                if (newValue.Type is UntypedObjectType)
                {
                    return new RunResult(newValue);
                }

                FormulaValue newValueDeserialized;

                var sb = new StringBuilder();
                var settings = new FormulaValueSerializerSettings()
                {
                    UseCompactRepresentation = true,
                };
                newValue.ToExpression(sb, settings);

                try
                {
                    // Serialization test. Serialized expression must produce an identical result.
                    // Serialization can't use TextFirst if enabled for the test, strings for example would have the wrong syntax.
                    options.TextFirst = false;
                    newValueDeserialized = await engine.EvalAsync(sb.ToString(), CancellationToken.None, options, runtimeConfig: runtimeConfig).ConfigureAwait(false);
                }
                catch (InvalidOperationException e)
                {
                    // If we failed because of range limitations with decimal, retry with NumberIsFloat enabled
                    // This is for tests that return 1e100 as a result, verifying proper floating point operation
                    if (!NumberIsFloat && e.Message.Contains("value is too large"))
                    {
                        // Serialization test. Serialized expression must produce an identical result.
                        options.NumberIsFloat = true;
                        newValueDeserialized = await engine.EvalAsync(sb.ToString(), CancellationToken.None, options, runtimeConfig: runtimeConfig).ConfigureAwait(false);
                    }
                    else if (e.Message.Contains("Name isn't valid. 'CalculatedOptionSetValue' isn't recognized."))
                    {
                        // This will only be displayed if the result of a formula is an option set value, for example
                        // "StartOfWeek.Tuesday" on a line by itself.  In the case, the line below effectively skips the
                        // deserialization test as we can't serialize that value, it is a runtime only value.
                        newValueDeserialized = newValue;
                    }
                    else
                    {
                        throw;
                    }
                }

                return new RunResult(newValueDeserialized, newValue);
            }
        }

        // Run through a .txt in sequence, allowing Set() functions that can create state. 
        // Useful for testing mutation functions. 
        // The .txt format still provides an expected return value after each expression.
        internal class ReplRunner : BaseRunner
        {
            private readonly RecalcEngine _engine;

            // Repl engine does all the policy around declaring variables via Set().
#pragma warning disable CS0618 // Type or member is obsolete
            public readonly PowerFxREPL _repl;
#pragma warning restore CS0618 // Type or member is obsolete

            public ReplRunner(RecalcEngine engine)
            {
                _engine = engine;

#pragma warning disable CS0618 // Type or member is obsolete
                _repl = new PowerFxREPL
                {
                    Engine = _engine,
                    AllowSetDefinitions = true
                };
#pragma warning restore CS0618 // Type or member is obsolete
            }

            protected override async Task<RunResult> RunAsyncInternal(string expr, string setupHandlerName = null)
            {
                var replResult = await _repl.HandleCommandAsync(expr, default).ConfigureAwait(false);

                // .txt output format - if there are any Set(), compare those.
                if (replResult.DeclaredVars.Count > 0)
                {
                    var newVar = replResult.DeclaredVars.First().Value;
                    var runResult = new RunResult(newVar);
                    return runResult;
                }

                var check = replResult.CheckResult;
                if (!check.IsSuccess)
                {
                    return new RunResult(check);
                }

                var result = replResult.EvalResult;
                return new RunResult(result);
            }
        }

        // These option set classes are internal because they aren't final.
        // They rely on converting back and forth from strings which isn't very efficient.
        // They are heare for testing purposes only, matching behavior for hosts that expose Dataverse option sets.

        private class NumberOptionSet : IExternalOptionSet
        {
            private readonly DisplayNameProvider _displayNameProvider;
            private readonly DType _type;

            private readonly bool _canCoerceFromBackingKind;
            private readonly bool _canCoerceToBackingKind;
            private readonly bool _canCompareNumeric;

            /// <summary>
            /// Initializes a new instance of the <see cref="NumberOptionSet"/> class.
            /// </summary>
            /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="options">The members of the option set. Enumerable of pairs of logical name to display name.
            /// <param name="canCoerceFromBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="canCoerceToBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="canCompareNumeric">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// NameCollisionException is thrown if display and logical names for options are not unique.
            /// </param>
            public NumberOptionSet(string name, ImmutableDictionary<double, DName> options, bool canCoerceFromBackingKind = false, bool canCoerceToBackingKind = false, bool canCompareNumeric = false)
                : this(name, IntDisplayNameProvider(options), canCoerceFromBackingKind, canCoerceToBackingKind, canCompareNumeric)
            {
            }

            private static DisplayNameProvider IntDisplayNameProvider(ImmutableDictionary<double, DName> optionSetValues)
            {
                return DisplayNameUtility.MakeUnique(optionSetValues.Select(kvp => new KeyValuePair<string, string>(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value)));
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="NumberOptionSet"/> class.
            /// </summary>
            /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="displayNameProvider">The DisplayNameProvider for the members of the OptionSet.
            /// <param name="canCoerceFromBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="canCoerceToBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="canCompareNumeric">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// Consider using <see cref="DisplayNameUtility.MakeUnique(IEnumerable{KeyValuePair{string, string}})"/> to generate
            /// the DisplayNameProvider.
            /// </param>
            public NumberOptionSet(string name, DisplayNameProvider displayNameProvider, bool canCoerceFromBackingKind = false, bool canCoerceToBackingKind = false, bool canCompareNumeric = false)
            {
                EntityName = new DName(name);
                Options = displayNameProvider.LogicalToDisplayPairs;

                _canCoerceFromBackingKind = canCoerceFromBackingKind;
                _canCoerceToBackingKind = canCoerceToBackingKind;
                _canCompareNumeric = canCompareNumeric;

                _displayNameProvider = displayNameProvider;
                FormulaType = new OptionSetValueType(this);
                _type = DType.CreateOptionSetType(this);
            }

            /// <summary>
            /// Name of the option set, referenceable from expressions.
            /// </summary>
            public DName EntityName { get; }

            /// <summary>
            /// Contains the members of the option set.
            /// Key is logical/invariant name, value is display name.
            /// </summary>
            public IEnumerable<KeyValuePair<DName, DName>> Options { get; }

            /// <summary>
            /// Formula Type corresponding to this option set.
            /// Use in record/table contexts to define the type of fields using this option set.
            /// </summary>
            public OptionSetValueType FormulaType { get; }

            public bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue)
            {
                if (!Options.Any(option => option.Key == fieldName))
                {
                    optionSetValue = null;
                    return false;
                }

                var osft = new OptionSetValueType(_type.OptionSetInfo);
                optionSetValue = new OptionSetValue(fieldName.Value, osft, double.Parse(fieldName.Value, CultureInfo.InvariantCulture));
                return true;
            }

            IEnumerable<DName> IExternalOptionSet.OptionNames => Options.Select(option => option.Key);

            DisplayNameProvider IExternalOptionSet.DisplayNameProvider => _displayNameProvider;

            bool IExternalOptionSet.IsConvertingDisplayNameMapping => false;

            DType IExternalEntity.Type => _type;

            DKind IExternalOptionSet.BackingKind => DKind.Number;

            bool IExternalOptionSet.CanCoerceFromBackingKind => _canCoerceFromBackingKind;

            bool IExternalOptionSet.CanCoerceToBackingKind => _canCoerceToBackingKind;

            bool IExternalOptionSet.CanCompareNumeric => _canCompareNumeric;

            bool IExternalOptionSet.CanConcatenateStronglyTyped => false;

            public override bool Equals(object obj)
            {
                return obj is NumberOptionSet other &&
                    EntityName == other.EntityName &&
                    this._type == other._type;
            }

            public override int GetHashCode()
            {
                return Hashing.CombineHash(EntityName.GetHashCode(), this._type.GetHashCode());
            }
        }

        private class BooleanOptionSet : IExternalOptionSet
        {
            private readonly DisplayNameProvider _displayNameProvider;
            private readonly DType _type;

            private readonly bool _canCoerceFromBackingKind;
            private readonly bool _canCoerceToBackingKind;

            /// <summary>
            /// Initializes a new instance of the <see cref="BooleanOptionSet"/> class.
            /// </summary>
            /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="options">The members of the option set. Enumerable of pairs of logical name to display name.
            /// <param name="canCoerceFromBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="canCoerceToBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// NameCollisionException is thrown if display and logical names for options are not unique.
            /// </param>
            public BooleanOptionSet(string name, ImmutableDictionary<bool, DName> options, bool canCoerceFromBackingKind = false, bool canCoerceToBackingKind = false)
                : this(name, IntDisplayNameProvider(options), canCoerceFromBackingKind, canCoerceToBackingKind)
            {
            }

            private static DisplayNameProvider IntDisplayNameProvider(ImmutableDictionary<bool, DName> optionSetValues)
            {
                return DisplayNameUtility.MakeUnique(optionSetValues.Select(kvp => new KeyValuePair<string, string>(kvp.Key ? "1" : "0", kvp.Value)));
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="BooleanOptionSet"/> class.
            /// </summary>
            /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="displayNameProvider">The DisplayNameProvider for the members of the OptionSet.
            /// <param name="canCoerceFromBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="canCoerceToBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// Consider using <see cref="DisplayNameUtility.MakeUnique(IEnumerable{KeyValuePair{string, string}})"/> to generate
            /// the DisplayNameProvider.
            /// </param>
            public BooleanOptionSet(string name, DisplayNameProvider displayNameProvider, bool canCoerceFromBackingKind = false, bool canCoerceToBackingKind = false)
            {
                EntityName = new DName(name);
                Options = displayNameProvider.LogicalToDisplayPairs;

                Contracts.Assert(Options.Count() == 2);
                Contracts.Assert((Options.First().Key == "0" && Options.Last().Key == "1") || (Options.First().Key == "1" && Options.Last().Key == "0"));

                _canCoerceFromBackingKind = canCoerceFromBackingKind;
                _canCoerceToBackingKind = canCoerceToBackingKind;

                _displayNameProvider = displayNameProvider;
                FormulaType = new OptionSetValueType(this);
                _type = DType.CreateOptionSetType(this);
            }

            /// <summary>
            /// Name of the option set, referenceable from expressions.
            /// </summary>
            public DName EntityName { get; }

            /// <summary>
            /// Contains the members of the option set.
            /// Key is logical/invariant name, value is display name.
            /// </summary>
            public IEnumerable<KeyValuePair<DName, DName>> Options { get; }

            /// <summary>
            /// Formula Type corresponding to this option set.
            /// Use in record/table contexts to define the type of fields using this option set.
            /// </summary>
            public OptionSetValueType FormulaType { get; }

            public bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue)
            {
                if (!Options.Any(option => option.Key == fieldName))
                {
                    optionSetValue = null;
                    return false;
                }

                var osft = new OptionSetValueType(_type.OptionSetInfo);
                optionSetValue = new OptionSetValue(fieldName.Value, osft, fieldName.Value != "0");
                return true;
            }

            IEnumerable<DName> IExternalOptionSet.OptionNames => Options.Select(option => option.Key);

            DisplayNameProvider IExternalOptionSet.DisplayNameProvider => _displayNameProvider;

            bool IExternalOptionSet.IsConvertingDisplayNameMapping => false;

            DType IExternalEntity.Type => _type;

            DKind IExternalOptionSet.BackingKind => DKind.Boolean;

            bool IExternalOptionSet.CanCoerceFromBackingKind => _canCoerceFromBackingKind;

            bool IExternalOptionSet.CanCoerceToBackingKind => _canCoerceToBackingKind;

            bool IExternalOptionSet.CanCompareNumeric => false;

            bool IExternalOptionSet.CanConcatenateStronglyTyped => false;

            public override bool Equals(object obj)
            {
                return obj is BooleanOptionSet other &&
                    EntityName == other.EntityName &&
                    this._type == other._type;
            }

            public override int GetHashCode()
            {
                return Hashing.CombineHash(EntityName.GetHashCode(), this._type.GetHashCode());
            }
        }

        private class ColorOptionSet : IExternalOptionSet
        {
            private readonly DisplayNameProvider _displayNameProvider;
            private readonly DType _type;

            private readonly bool _canCoerceFromBackingKind;
            private readonly bool _canCoerceToBackingKind;

            /// <summary>
            /// Initializes a new instance of the <see cref="ColorOptionSet"/> class.
            /// </summary>
            /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="options">The members of the option set. Enumerable of pairs of logical name to display name.
            /// <param name="canCoerceFromBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="canCoerceToBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// NameCollisionException is thrown if display and logical names for options are not unique.
            /// </param>
            public ColorOptionSet(string name, ImmutableDictionary<double, DName> options, bool canCoerceFromBackingKind = false, bool canCoerceToBackingKind = false)
                : this(name, IntDisplayNameProvider(options), canCoerceFromBackingKind, canCoerceToBackingKind)
            {
            }

            private static DisplayNameProvider IntDisplayNameProvider(ImmutableDictionary<double, DName> optionSetValues)
            {
                return DisplayNameUtility.MakeUnique(optionSetValues.Select(kvp => new KeyValuePair<string, string>(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value)));
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ColorOptionSet"/> class.
            /// </summary>
            /// <param name="name">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="displayNameProvider">The DisplayNameProvider for the members of the OptionSet.
            /// <param name="canCoerceFromBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// <param name="canCoerceToBackingKind">The name of the option set. Will be available as a global name in Power Fx expressions.</param>
            /// Consider using <see cref="DisplayNameUtility.MakeUnique(IEnumerable{KeyValuePair{string, string}})"/> to generate
            /// the DisplayNameProvider.
            /// </param>
            public ColorOptionSet(string name, DisplayNameProvider displayNameProvider, bool canCoerceFromBackingKind = false, bool canCoerceToBackingKind = false)
            {
                EntityName = new DName(name);
                Options = displayNameProvider.LogicalToDisplayPairs;

                _canCoerceFromBackingKind = canCoerceFromBackingKind;
                _canCoerceToBackingKind = canCoerceToBackingKind;

                _displayNameProvider = displayNameProvider;
                FormulaType = new OptionSetValueType(this);
                _type = DType.CreateOptionSetType(this);
            }

            /// <summary>
            /// Name of the option set, referenceable from expressions.
            /// </summary>
            public DName EntityName { get; }

            /// <summary>
            /// Contains the members of the option set.
            /// Key is logical/invariant name, value is display name.
            /// </summary>
            public IEnumerable<KeyValuePair<DName, DName>> Options { get; }

            /// <summary>
            /// Formula Type corresponding to this option set.
            /// Use in record/table contexts to define the type of fields using this option set.
            /// </summary>
            public OptionSetValueType FormulaType { get; }

            public bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue)
            {
                if (!Options.Any(option => option.Key == fieldName))
                {
                    optionSetValue = null;
                    return false;
                }

                var osft = new OptionSetValueType(_type.OptionSetInfo);
                optionSetValue = new OptionSetValue(fieldName.Value, osft, double.Parse(fieldName.Value, CultureInfo.InvariantCulture));
                return true;
            }

            IEnumerable<DName> IExternalOptionSet.OptionNames => Options.Select(option => option.Key);

            DisplayNameProvider IExternalOptionSet.DisplayNameProvider => _displayNameProvider;

            bool IExternalOptionSet.IsConvertingDisplayNameMapping => false;

            DType IExternalEntity.Type => _type;

            DKind IExternalOptionSet.BackingKind => DKind.Color;

            bool IExternalOptionSet.CanCoerceFromBackingKind => _canCoerceFromBackingKind;

            bool IExternalOptionSet.CanCoerceToBackingKind => _canCoerceToBackingKind;

            bool IExternalOptionSet.CanCompareNumeric => false;

            bool IExternalOptionSet.CanConcatenateStronglyTyped => false;

            public override bool Equals(object obj)
            {
                return obj is ColorOptionSet other &&
                    EntityName == other.EntityName &&
                    this._type == other._type;
            }

            public override int GetHashCode()
            {
                return Hashing.CombineHash(EntityName.GetHashCode(), this._type.GetHashCode());
            }
        }
    }

    public static class UserInfoTestSetup
    {
        public static BasicUserInfo UserInfo = new BasicUserInfo
        {
            FullName = "Susan Burk",
            Email = "susan@contoso.com",
            DataverseUserId = new Guid("aa1d4f65-044f-4928-a95f-30d4c8ebf118"),
            TeamsMemberId = "29:1DUjC5z4ttsBQa0fX2O7B0IDu30R",
        };

        public static SymbolTable GetUserInfoSymbolTable()
        {
            var props = new Dictionary<string, object>
            {
                { "FullName", UserInfo.FullName },
                { "Email", UserInfo.Email },
                { "DataverseUserId", UserInfo.DataverseUserId },
                { "TeamsMemberId", UserInfo.TeamsMemberId }
            };

            var allKeys = props.Keys.ToArray();
            SymbolTable userSymbolTable = new SymbolTable();

            userSymbolTable.AddUserInfoObject(allKeys);

            return userSymbolTable;
        }
    }
}
