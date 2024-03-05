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
    public class TexlTests : PowerFxTest
    {
        private readonly CultureInfo _defaultLocale = new ("en-US");

        private const string ChurnDataFunctionName = "ChurnData";

        [Theory]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + 5", "d")]
        [InlineData("Date(2000,1,1) + Time(2000,1,1)", "d")]
        [InlineData("Time(2000,1,1) + Date(2000,1,1)", "d")]
        [InlineData("Time(2000,1,1) + 5", "T")]
        [InlineData("5 + DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("5 + Time(2000,1,1)", "T")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - DateTimeValue(\"1 Jan 2015\")", "n")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - Date(2000,1,1)", "n")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - 5", "d")]
        [InlineData("Date(2000,1,1) - DateTimeValue(\"1 Jan 2015\")", "n")]
        [InlineData("Date(2000,1,1) - Date(1999,1,1)", "n")]
        [InlineData("Time(2,1,1) - Time(2,1,1)", "n")]
        [InlineData("5 - DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("5 - Time(2000,1,1)", "T")]
        [InlineData("-Date(2001,1,1)", "D")]
        [InlineData("-Time(2,1,1)", "T")]
        [InlineData("-DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("Date(2000,1,1) + 5", "D")]
        [InlineData("5 + Date(2000,1,1)", "D")]
        [InlineData("5 - Date(2000,1,1)", "D")]
        [InlineData("Time(20,1,1) + Time(19,1,1)", "T")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + Time(20,1,1)", "d")]
        [InlineData("Time(20,1,1) + DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - Time(20,1,1)", "d")]
        [InlineData("DateValue(\"1 Jan 2015\") - Time(20,1,1)", "d")]
        public void TexlDateOverloads(string script, string expectedType)
        {
            Assert.True(DType.TryParse(expectedType, out var type), script);
            Assert.True(type.IsValid, script);

            TestSimpleBindingSuccess(script, TestUtils.DT(expectedType));
        }

        [Theory]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + Date(2000,1,1)")]
        [InlineData("Date(2000,1,1) + Date(1999,1,1)")]
        [InlineData("Date(2000,1,1) + DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("Time(20,1,1) - DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("Time(20,1,1) - Date(2000,1,1)")]
        public void TexlDateOverloads_Negative(string script)
        {
            // TestBindingErrors(script, DType.Error);
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(script);

            Assert.Equal(DType.Error, result.Binding.ResultType);
            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("DateAdd(Date(2000,1,1), 1)", "D")]
        [InlineData("DateAdd(Date(2000,1,1), 1, TimeUnit.Months)", "D")]
        [InlineData("DateAdd(Date(2000,1,1), \"2\")", "D")] // Coercion on delta argument from string
        [InlineData("DateAdd(Date(2000,1,1), true)", "D")] // Coercion on delta argument from boolean
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"), 2)", "d")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"), 2, TimeUnit.Years)", "d")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"), \"hello\")", "d")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"), \"hello\", 3)", "d")]
        [InlineData("DateAdd(\"2000-01-01\", 1)", "d")] // Coercion on date argument from string
        [InlineData("DateAdd(45678, 1)", "d")] // Coercion on date argument from number
        [InlineData("DateAdd(Time(12,34,56), 1)", "T")] // Coercion on date argument from time
        public void TexlDateAdd(string script, string expectedType)
        {
            Assert.True(DType.TryParse(expectedType, out var type));
            Assert.True(type.IsValid);

            TestSimpleBindingSuccess(script, type);
        }

        [Theory]
        [InlineData("DateAdd([Date(2000,1,1)],1)", "*[Value:D]")]
        [InlineData("DateAdd([Date(2000,1,1)],[3])", "*[Value:D]")]
        [InlineData("DateAdd(Table({a:Date(2000,1,1)}),[3])", "*[a:D]")]
        [InlineData("DateAdd(Date(2000,1,1),[1])", "*[Result:D]")]
        [InlineData("DateAdd(\"2021-02-03\",[1])", "*[Result:d]")] // Coercion from string
        [InlineData("DateAdd(44955,[1])", "*[Result:d]")] // Coercion from number
        [InlineData("DateAdd(Time(12,0,0),[1])", "*[Result:T]")] // Coercion from time
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],1)", "*[Value:d]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],[3])", "*[Value:d]")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"),[1])", "*[Result:d]")]
        [InlineData("DateAdd([\"2011-02-03\"],1)", "*[Value:d]")] // Coercion from string
        [InlineData("DateAdd([44900],1)", "*[Value:d]")] // Coercion from number
        [InlineData("DateAdd([Time(12,0,0)],1)", "*[Value:T]")] // Coercion from time
        [InlineData("DateDiff([Date(2000,1,1)],[Date(2001,1,1)],\"years\")", "*[Result:n]")]
        [InlineData("DateDiff(Date(2000,1,1),[Date(2001,1,1)],\"years\")", "*[Result:n]")]
        [InlineData("DateDiff([Date(2000,1,1)],Date(2001,1,1),\"years\")", "*[Result:n]")]
        public void TexlDateTableFunctions_Float(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig(Features.None));
            var options = new ParserOptions() { NumberIsFloat = true };
            var result = engine.Check(expression, options);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("DateDiff([Date(2000,1,1)],[Date(2001,1,1)],\"years\")", "*[Result:w]")]
        [InlineData("DateDiff(Date(2000,1,1),[Date(2001,1,1)],\"years\")", "*[Result:w]")]
        [InlineData("DateDiff([Date(2000,1,1)],Date(2001,1,1),\"years\")", "*[Result:w]")]
        public void TexlDateTableFunctions(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig(Features.None));
            var result = engine.Check(expression);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("DateAdd([Date(2000,1,1)],1)", "*[Value:D]")]
        [InlineData("DateAdd([Date(2000,1,1)],[3])", "*[Value:D]")]
        [InlineData("DateAdd(Table({a:Date(2000,1,1)}),[3])", "*[Value:D]")]
        [InlineData("DateAdd(Date(2000,1,1),[1])", "*[Value:D]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],1)", "*[Value:d]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],[3])", "*[Value:d]")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"),[1])", "*[Value:d]")]
        [InlineData("DateDiff([Date(2000,1,1)],[Date(2001,1,1)],\"years\")", "*[Value:w]")]
        [InlineData("DateDiff(Date(2000,1,1),[Date(2001,1,1)],\"years\")", "*[Value:w]")]
        [InlineData("DateDiff([Date(2000,1,1)],Date(2001,1,1),\"years\")", "*[Value:w]")]
        public void TexlDateTableFunctions_ConsistentOneColumnTableResult(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig(new Features { ConsistentOneColumnTableResult = true }));
            var options = new ParserOptions() { NumberIsFloat = false };
            var result = engine.Check(expression, options);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("DateAdd([Date(2000,1,1)],1)", "*[Value:D]")]
        [InlineData("DateAdd([Date(2000,1,1)],[3])", "*[Value:D]")]
        [InlineData("DateAdd(Table({a:Date(2000,1,1)}),[3])", "*[Value:D]")]
        [InlineData("DateAdd(Date(2000,1,1),[1])", "*[Value:D]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],1)", "*[Value:d]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],[3])", "*[Value:d]")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"),[1])", "*[Value:d]")]
        [InlineData("DateDiff([Date(2000,1,1)],[Date(2001,1,1)],\"years\")", "*[Value:n]")]
        [InlineData("DateDiff(Date(2000,1,1),[Date(2001,1,1)],\"years\")", "*[Value:n]")]
        [InlineData("DateDiff([Date(2000,1,1)],Date(2001,1,1),\"years\")", "*[Value:n]")]
        public void TexlDateTableFunctions_ConsistentOneColumnTableResult_Float(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig(new Features { ConsistentOneColumnTableResult = true }));
            var options = new ParserOptions() { NumberIsFloat = true };
            var result = engine.Check(expression, options);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("DateAdd(Table({v:Date(2022,1,1),s:9},{v:Date(2022,2,2),s:25}), 2, TimeUnit.Days)")] // Not a single-column table
        [InlineData("DateAdd(DropColumns([Date(2022,1,1),Date(2022,2,2)],\"Value\"), 2, TimeUnit.Days)")] // Not a single-column table
        [InlineData("DateDiff(Table({v:Date(2022,1,1),s:9},{v:Date(2022,2,2),s:25}), Date(2022,12,12), TimeUnit.Days)")]
        [InlineData("DateDiff(DropColumns([Date(2022,1,1),Date(2022,2,2)],\"Value\"), Date(2022,2,2), TimeUnit.Days)")]
        public void TexlDateTableFunctions_Negative(string expression)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression);

            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("DropColumns([{A:\"hello\",B:1}], A)", "*[B:n]")]
        [InlineData("DropColumns([{A:\"hello\",B:1}], B)", "*[A:s]")]
        [InlineData("DropColumns([{A:\"hello\",B:1}], A, B)", "*[]")]
        [InlineData("DropColumns({A:\"hello\",B:1}, A)", "![B:n]")]
        [InlineData("DropColumns({A:\"hello\",B:1}, B)", "![A:s]")]
        [InlineData("DropColumns({A:\"hello\",B:1}, A, B)", "![]")]
        public void TexlFunctionTypeSemanticsDropColumns(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig(Features.PowerFxV1));
            var options = new ParserOptions() { NumberIsFloat = true };
            var result = engine.Check(expression, options);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("DropColumns([{A:\"hello\",B:1}], \"A\")", "*[B:n]")]
        [InlineData("DropColumns([{A:\"hello\",B:1}], \"B\")", "*[A:s]")]
        [InlineData("DropColumns([{A:\"hello\",B:1}], \"A\", \"B\")", "*[]")]
        [InlineData("DropColumns({A:\"hello\",B:1}, \"A\")", "![B:n]")]
        [InlineData("DropColumns({A:\"hello\",B:1}, \"B\")", "![A:s]")]
        [InlineData("DropColumns({A:\"hello\",B:1}, \"A\", \"B\")", "![]")]
        public void TexlFunctionTypeSemanticsDropColumns_ColumnNamesAsIdentifiersDisabled(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig(new Features { SupportColumnNamesAsIdentifiers = false, TableSyntaxDoesntWrapRecords = true }));
            var options = new ParserOptions() { NumberIsFloat = true };
            var result = engine.Check(expression, options);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("DropColumns([{A:\"hello\",B:1}], C)")]
        [InlineData("DropColumns([{A:\"hello\",B:1}], AB)")]
        [InlineData("DropColumns([{A:\"hello\",B:1}], A1)")]
        [InlineData("DropColumns({A:\"hello\",B:1}, C)")]
        [InlineData("DropColumns({A:\"hello\",B:1}, AB)")]
        [InlineData("DropColumns({A:\"hello\",B:1}, A1)")]
        [InlineData("DropColumns([{A:\"hello\",B:1}], \"A\")")]
        public void TexlFunctionTypeSemanticsDropColumns_Negative(string expression)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression);

            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("ShowColumns([{A:\"hello\",B:1}], A)", "*[A:s]")]
        [InlineData("ShowColumns([{A:\"hello\",B:1}], B)", "*[B:n]")]
        [InlineData("ShowColumns([{A:\"hello\",B:1}], A, B)", "*[A:s,B:n]")]
        [InlineData("ShowColumns({A:\"hello\",B:1}, A)", "![A:s]")]
        [InlineData("ShowColumns({A:\"hello\",B:1}, B)", "![B:n]")]
        [InlineData("ShowColumns({A:\"hello\",B:1}, A, B)", "![A:s,B:n]")]
        public void TexlFunctionTypeSemanticsShowColumns(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig(Features.PowerFxV1));
            var options = new ParserOptions() { NumberIsFloat = true };
            var result = engine.Check(expression, options);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("ShowColumns([{A:\"hello\",B:1}], \"A\")", "*[A:s]")]
        [InlineData("ShowColumns([{A:\"hello\",B:1}], \"B\")", "*[B:n]")]
        [InlineData("ShowColumns([{A:\"hello\",B:1}], \"A\", \"B\")", "*[A:s,B:n]")]
        [InlineData("ShowColumns({A:\"hello\",B:1}, \"A\")", "![A:s]")]
        [InlineData("ShowColumns({A:\"hello\",B:1}, \"B\")", "![B:n]")]
        [InlineData("ShowColumns({A:\"hello\",B:1}, \"A\", \"B\")", "![A:s,B:n]")]
        public void TexlFunctionTypeSemanticsShowColumns_ColumnNamesAsIdentifiersDisabled(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig(new Features { SupportColumnNamesAsIdentifiers = false, TableSyntaxDoesntWrapRecords = true }));
            var options = new ParserOptions() { NumberIsFloat = true };
            var result = engine.Check(expression, options);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("ShowColumns([{A:\"hello\",B:1}], C)")]
        [InlineData("ShowColumns([{A:\"hello\",B:1}], AB)")]
        [InlineData("ShowColumns([{A:\"hello\",B:1}], A1)")]
        [InlineData("ShowColumns({A:\"hello\",B:1}, C)")]
        [InlineData("ShowColumns({A:\"hello\",B:1}, AB)")]
        [InlineData("ShowColumns({A:\"hello\",B:1}, A1)")]
        [InlineData("ShowColumns([{A:\"hello\",B:1}], \"B\")")]
        public void TexlFunctionTypeSemanticsShowColumns_Negative(string expression)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression);

            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("RenameColumns([{A:\"hello\",B:1}], A, C)", "*[B:n,C:s]")]
        [InlineData("RenameColumns([{A:\"hello\",B:1}], B, C)", "*[A:s,C:n]")]
        [InlineData("RenameColumns([{A:\"hello\",B:1}], A, C, B, D)", "*[C:s,D:n]")]
        [InlineData("RenameColumns({A:\"hello\",B:1}, A, C)", "![B:n,C:s]")]
        [InlineData("RenameColumns({A:\"hello\",B:1}, B, C)", "![A:s,C:n]")]
        [InlineData("RenameColumns({A:\"hello\",B:1}, A, C, B, D)", "![C:s,D:n]")]
        public void TexlFunctionTypeSemanticsRenameColumns(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig(Features.PowerFxV1));
            var options = new ParserOptions() { NumberIsFloat = true };
            var result = engine.Check(expression, options);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("RenameColumns([{A:\"hello\",B:1}], \"A\", \"C\")", "*[B:n,C:s]")]
        [InlineData("RenameColumns([{A:\"hello\",B:1}], \"B\", \"C\")", "*[A:s,C:n]")]
        [InlineData("RenameColumns([{A:\"hello\",B:1}], \"A\", \"C\", \"B\", \"D\")", "*[C:s,D:n]")]
        [InlineData("RenameColumns({A:\"hello\",B:1}, \"A\", \"C\")", "![B:n,C:s]")]
        [InlineData("RenameColumns({A:\"hello\",B:1}, \"B\", \"C\")", "![A:s,C:n]")]
        [InlineData("RenameColumns({A:\"hello\",B:1}, \"A\", \"C\", \"B\", \"D\")", "![C:s,D:n]")]
        public void TexlFunctionTypeSemanticsRenameColumns_ColumnNamesAsIdentifiersDisabled(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig(new Features { SupportColumnNamesAsIdentifiers = false, TableSyntaxDoesntWrapRecords = true }));
            var options = new ParserOptions() { NumberIsFloat = true };
            var result = engine.Check(expression, options);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("RenameColumns([{A:\"hello\",B:1}], C, B)")]
        [InlineData("RenameColumns([{A:\"hello\",B:1}], AB, B)")]
        [InlineData("RenameColumns([{A:\"hello\",B:1}], A1, B)")]
        [InlineData("RenameColumns({A:\"hello\",B:1}, C, B)")]
        [InlineData("RenameColumns({A:\"hello\",B:1}, AB, B)")]
        [InlineData("RenameColumns({A:\"hello\",B:1}, A1, B)")]
        [InlineData("RenameColumns([{A:\"hello\",B:1}], \"A\", \"B\")")]
        [InlineData("RenameColumns([{A:\"hello\",B:1}], A, A1, A, A2)")]
        [InlineData("RenameColumns(true, A, A1)")]
        public void TexlFunctionTypeSemanticsRenameColumns_Negative(string expression)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression);

            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("IfError(\"Hello\", \"one\")", "s")]
        [InlineData("IfError(\"Hello\", 1)", "s")]
        [InlineData("IfError(\"Hello\", 1, 3)", "n")]
        [InlineData("IfError(\"Hello\", \"one\", \"world\", 2, 1)", "n")]
        [InlineData("IfError({a:1}, \"one\", [1,2,3], 2, 1)", "n")]
        public void TexlFunctionTypeSemanticsIfError(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig());
            var options = new ParserOptions() { NumberIsFloat = true };
            var result = engine.Check(expression, options);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("IfError(\"Hello\", {a:\"one\"})")]
        [InlineData("IfError(\"Hello\", 1, {a:2})", false, true)]
        [InlineData("IfError(\"Hello\", 1, 3, [true])")]
        [InlineData("IfError({a:1}, true)")]
        [InlineData("IfError(1, [1], true, {a:1}, \"hello\")", false, true)]
        [InlineData("IfError(IfError({a:1}, true), true)")]
        [InlineData("false; IfError({a:1}, true); true", true)]
        [InlineData("IsError(false; IfError({a:1}, true); true)", true)]
        public void TexlFunctionTypeSemanticsIfError_MismatchedTypes(string expression, bool usesChain = false, bool preV1Bug = false)
        {
            foreach (var usePowerFxV1Rules in new[] { false, true })
            {
                if (!usePowerFxV1Rules && preV1Bug)
                {
                    // Bug on pre-V1 logic; will not fix for legacy reasons
                    continue;
                }

                foreach (var isBehavior in new[] { false, true })
                {
                    var features = new Features(Features.PowerFxV1)
                    {
                        PowerFxV1CompatibilityRules = usePowerFxV1Rules
                    };

                    var engine = new Engine(new PowerFxConfig(features));
                    var parserOptions = new ParserOptions() { NumberIsFloat = true, AllowsSideEffects = isBehavior };
                    var result = engine.Check(expression, parserOptions);

                    if (usePowerFxV1Rules && !usesChain)
                    {
                        Assert.True(result.IsSuccess);
                        Assert.Equal(DType.Void, result.Binding.ResultType);
                    }
                    else
                    {
                        Assert.True(isBehavior == result.IsSuccess, $"For PFxV1={usePowerFxV1Rules} and isBehavior={isBehavior}, expression {expression} should {(isBehavior ? "succeed" : "fail")}");
                    }
                }
            }
        }

        [Theory]
        [InlineData("Switch(1, 2, true, 3, {}, 4, [])", "-")] // Mismatched return types: Void result
        [InlineData("Switch(11;If(true, 123, {}); 1, 2, true, 3, false)", "b")] // Void inside a chain - ok
        [InlineData("Switch(1, 2, If(1<2, true, 1<3, []), 3, Color.Blue)", "-")] // Void as one of return types: Void result
        public void TexlFunctionTypeSemanticsSwitch_VoidExpressions(string expression, string expectedType)
        {
            foreach (var usePowerFxV1Rules in new[] { false, true })
            {
                var features = new Features(Features.PowerFxV1)
                {
                    PowerFxV1CompatibilityRules = usePowerFxV1Rules
                };

                var engine = new Engine(new PowerFxConfig(features));
                var parserOptions = new ParserOptions() { NumberIsFloat = true, AllowsSideEffects = true };
                var result = engine.Check(expression, parserOptions);

                Assert.True(result.IsSuccess, $"For PFxV1={usePowerFxV1Rules}, expression {expression} should succeed");
                Assert.Equal(TestUtils.DT(expectedType), result.Binding.ResultType);
            }
        }

        [Theory]
        [InlineData("Switch(If(true, true, {}), true, 1, 2)")]
        [InlineData("Switch(11;22;If(true, 10;20;If(true, 33, 123;{}), false), true, 1, false, 2)")]
        [InlineData("Switch(If(1<2, true, 1<3, [], 1<4, {},1<5, Color.Blue), true, true, 1, false, 2)")]
        public void TexlFunctionTypeSemanticsSwitch_MismatchedTypes_Negative(string expression)
        {
            foreach (var usePowerFxV1Rules in new[] { false, true })
            {
                var features = new Features(Features.PowerFxV1)
                {
                    PowerFxV1CompatibilityRules = usePowerFxV1Rules
                };

                var engine = new Engine(new PowerFxConfig(features));
                var parserOptions = new ParserOptions() { NumberIsFloat = true, AllowsSideEffects = true };
                var result = engine.Check(expression, parserOptions);

                Assert.False(result.IsSuccess, $"For PFxV1={usePowerFxV1Rules}, expression {expression} should fail");
            }
        }

        [Theory]
        [InlineData("Average(\"3\")")]
        [InlineData("Average(\"3\", 4)")]
        [InlineData("Average(true, 4)")]
        [InlineData("Average(true, \"5\", 6)")]
        public void TexlFunctionTypeSemanticsAverageWithCoercion(string script)
        {
            TestSimpleBindingSuccess(script, DType.Number);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsAverageTypedGlobalWithCoercion()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Boolean);
            symbol.AddVariable("B", FormulaType.String);
            symbol.AddVariable("C", FormulaType.Number);
            TestSimpleBindingSuccess("Average(1, 2, A, B, C)", DType.Number, symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsChar()
        {
            TestSimpleBindingSuccess("Char(65)", DType.String);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Value:n]")));
            TestSimpleBindingSuccess("Char(T)", TestUtils.DT("*[Result:s]"), symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsChar_ConsistentOneColumnTableResult()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Value:n]")));
            TestSimpleBindingSuccess("Char(T)", TestUtils.DT("*[Value:s]"), symbol, new Features { ConsistentOneColumnTableResult = true });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsConcatenate()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("myString", FormulaType.String);
            TestSimpleBindingSuccess(
                "Concatenate(\"abcdef\", myString)",
                DType.String,
                symbol);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "Concatenate(ShowColumns(Table,\"B\"), \" ending\")",
                TestUtils.DT("*[Result:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Concatenate(\" Begining\", myString, \" simple\", \"\", \" ending\")",
                DType.String,
                symbol);

            symbol.RemoveVariable("Table");
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:s]")));
            TestSimpleBindingSuccess(
                "Concatenate(Table!B, \" ending\", Table!D)",
                TestUtils.DT("*[Result:s]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsConcatenate_ConsistentOneColumnTableResult()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("myString", FormulaType.String);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "Concatenate(ShowColumns(Table,\"B\"), \" ending\")",
                TestUtils.DT("*[Value:s]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });

            symbol.RemoveVariable("Table");
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:s]")));
            TestSimpleBindingSuccess(
                "Concatenate(ShowColumns(Table,\"B\"), \" ending\", ShowColumns(Table,\"D\"))",
                TestUtils.DT("*[Value:s]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsCount()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "Count(Table)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "Count(ShowColumns(Table,\"A\"))",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsCountA()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "CountA(Table)",
                DType.Number,
                symbol);

            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:s]")));
            TestSimpleBindingSuccess(
                "CountA(Table2)",
                DType.Number,
                symbol);

            symbol.AddVariable("Table3", new TableType(TestUtils.DT("*[A:s, B:n, C:b]")));
            TestSimpleBindingSuccess(
                "CountA(ShowColumns(Table3,\"C\"))",
                DType.Number,
                symbol);
        }

        internal class BooleanOptionSet : IExternalOptionSet
        {
            public DisplayNameProvider DisplayNameProvider => DisplayNameUtility.MakeUnique(new Dictionary<string, string>
            {
                { "Yes", "Yes" },
                { "No", "No" },
            });

            public IEnumerable<DName> OptionNames => new[] { new DName("No"), new DName("Yes") };

            public DKind BackingKind => DKind.Boolean;

            public bool IsConvertingDisplayNameMapping => false;

            public DName EntityName => new DName("BoolOptionSet");

            public DType Type => DType.CreateOptionSetType(this);

            public OptionSetValueType OptionSetValueType => new OptionSetValueType(this);

            public bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue)
            {
                if (fieldName.Value == "No" || fieldName.Value == "Yes")
                {
                    optionSetValue = new OptionSetValue(fieldName.Value, this.OptionSetValueType, fieldName.Value == "Yes");
                    return true;
                }

                optionSetValue = null;
                return false;
            }
        }

        [Theory]
        [InlineData("CountIf(Table, A < 10)")]
        [InlineData("CountIf(Table, A < 10, A > 0, A <> 2)")]
        [InlineData("CountIf([1,2,3], Value) // Coercion from number to boolean")]
        [InlineData("CountIf([\"false\",\"true\",\"false\"], Value) // Coercion from text to boolean")]
        [InlineData("CountIf([1,2,3], BoolOptionSet.Yes) // Coercion from 2-valued option set to boolean")]
        [InlineData("CountIf([1,2,3], If(true, 0 > 1, \"true\"))")]
        [InlineData("CountIf([{a:\"true\",b:BoolOptionSet.Yes},{a:\"false\",b:BoolOptionSet.No}], a, b) // Coercion from fields in table")]
        public void TexlFunctionTypeSemanticsCountIf(string expression)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));

            TestSimpleBindingSuccess(
                expression,
                DType.Number,
                symbol,
                features: Features.PowerFxV1,
                optionSets: new[] { new BooleanOptionSet() });
        }

        [Theory]
        [InlineData("CountIf(Table, Today() + A)")]
        [InlineData("CountIf(Table, A > 0, Today() + A)")]
        [InlineData("CountIf(Table, {Result:A})")]
        [InlineData("CountIf([1,3,4], NonBoolOptionSet.First")]
        [InlineData("CountIf(First(Table), true)")]
        public void TexlFunctionTypeSemanticsCountIf_Negative(string expression)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));

            var nonBoolOptionSetDisplayNameProvider = DisplayNameUtility.MakeUnique(new Dictionary<string, string>
            {
                { "First", "One" },
                { "Second", "Two" },
                { "Third", "Three" },
            });

            TestBindingErrors(
                expression,
                DType.Number,
                symbol,
                optionSets: new[] { new OptionSet("NonBoolOptionSet", nonBoolOptionSetDisplayNameProvider) });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsCountRows()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:b, C:s]")));

            TestSimpleBindingSuccess(
                "CountRows(Table)",
                DType.Number,
                symbol);

            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:b, C:s, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "CountRows(Table2)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "CountRows(First(Table2).D)",
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsFilter()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "Filter(Table, A < 10)",
                TestUtils.DT("*[A:n]"),
                symbol);

            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:s, C:b]")));
            TestSimpleBindingSuccess(
                "Filter(Table2, A < 10, B = \"foo\", C = true)",
                TestUtils.DT("*[A:n, B:s, C:b]"),
                symbol);

            symbol.AddVariable("Table3", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]")));
            TestSimpleBindingSuccess(
                "Filter(Table3, D.X < 10, B = \"foo\", C = true)",
                TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]"),
                symbol);

            symbol.AddVariable("Table4", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "Filter(Table4, CountRows(ShowColumns(D,\"X\")) < 10, B = \"foo\", C = true)",
                TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]"),
                symbol);

            symbol.AddVariable("Table5", new TableType(TestUtils.DT("*[A:g]")));
            TestSimpleBindingSuccess(
                "Filter(Table5, A = GUID(\"43cb2147-c701-4981-b8ed-f0dd56e3fdde\"))",
                TestUtils.DT("*[A:g]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsFilter_Negative()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:g]")));
            TestBindingErrors(
                "Filter(Table, A = \"43cb2147-c701-4981-b8ed-f0dd56e3fdde\")",
                TestUtils.DT("*[A:g]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsFirst()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "First(Table)",
                TestUtils.DT("![A:n]"),
                symbol);

            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:*[C:n]]")));
            TestSimpleBindingSuccess(
                "First(Table2)",
                TestUtils.DT("![A:n, B:*[C:n]]"),
                symbol);

            symbol.AddVariable("Table3", new TableType(TestUtils.DT("*[A:n, B:*[C:*[D:*[E:![F:s, G:n]]]]]")));
            TestSimpleBindingSuccess(
                "First(Table3)",
                TestUtils.DT("![A:n, B:*[C:*[D:*[E:![F:s, G:n]]]]]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsFirstN()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));

            TestSimpleBindingSuccess(
                "FirstN(Table)",
                TestUtils.DT("*[A:n]"),
                symbol);
            TestSimpleBindingSuccess(
                "FirstN(Table, 1234)",
                TestUtils.DT("*[A:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIf()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("B", new TableType(TestUtils.DT("*[X:n, Y:s, Z:b]")));
            symbol.AddVariable("C", new TableType(TestUtils.DT("*[Y:s, XX:n, Z:b]")));

            TestSimpleBindingSuccess(
                "If(A < 10, 1, 2)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 10, B, C)",
                TestUtils.DT("*[Y:s, Z:b]"),
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 10, 1, A < 20, 2, A < 30, 3)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 10, 1, A < 20, 2, A < 30, 3, 4)",
                DType.Number,
                symbol);

            TestSimpleBindingSuccess(
                "If(A < 10, [[1,2,3],[3,2,1]], [[1,3,2],[3,2,3],[1,1,3]])",
                TestUtils.DT("*[Value:*[Value:n]]"),
                symbol);
        }

        [Theory]
        [InlineData("If(A < 10, 1, \"2\")", "n", true)]
        [InlineData("If(A < 1, \"one\", A < 2, 2, A < 3, true, false)", "s", true)]
        [InlineData("If(A < 1, true, A < 2, 2, A < 3, false, \"true\")", "b", true)]
        [InlineData("If(A < 10, 1, [1,2,3])", "-", true)]
        [InlineData("If(A < 10, 1, {Value: 2})", "-", true)]
        [InlineData("If(0 < 1, [1], 2)", "-", true)]

        // negative cases, when if produces void type
        // If(1 < 0, [1], 2) => V which is void value

        // void type is not allowed in aggregate type.
        // {test: V}
        [InlineData("{test: If(1 < 0, [1], 2)}", "![]", false)]

        // [V]
        [InlineData("[If(1 < 0, [1], 2)]", "*[]", false)]

        // void type can't be consumed.
        // V + 1 
        [InlineData("If(1 < 0, [1], 2) + 1", "n", false)]

        // Abs(V)
        [InlineData("Abs(If(1 < 0, [1], 2))", "n", false)]

        // Len(V)
        [InlineData("Len(If(1 < 0, [1], 2))", "n", false)]

        // If(V, 0, 1)
        [InlineData("If(If(1 < 0, [1], 2), 0, 1)", "n", false)]

        // Hour(V)
        [InlineData("Hour(If(1 < 0, [1], 2))", "n", false)]

        // ForAll([1,2,3], V)
        [InlineData("ForAll([1,2,3], If(1 < 0, [1], 2))", "-", true)]
        public void TexlFunctionTypeSemanticsIfWithArgumentCoercion(string expression, string expectedType, bool checkSuccess)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);

            if (checkSuccess)
            {
                TestSimpleBindingSuccess(
                                    expression,
                                    TestUtils.DT(expectedType),
                                    symbol);
            }
            else
            {
                TestBindingErrors(
                    expression,
                    TestUtils.DT(expectedType),
                    symbol);
            }
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIsBlank()
        {
            TestSimpleBindingSuccess("IsBlank(\"foo\")", DType.Boolean);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "IsBlank(T)",
                DType.Boolean,
                symbol);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "IsBlank(Table)",
                DType.Boolean,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIsBlankOrError()
        {
            TestSimpleBindingSuccess(
                "IsBlankOrError(\"foo\")",
                DType.Boolean);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "IsBlankOrError(T)",
                DType.Boolean,
                symbol);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "IsBlankOrError(Table)",
                DType.Boolean,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsIsNumeric()
        {
            TestSimpleBindingSuccess(
                "IsNumeric(\"12\")",
                DType.Boolean);

            TestSimpleBindingSuccess(
                "IsNumeric(12)",
                DType.Boolean);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "IsNumeric(T)",
                DType.Boolean,
                symbol);

            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            TestSimpleBindingSuccess(
                "IsNumeric(Table)",
                DType.Boolean,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsLast()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "Last(Table)",
                TestUtils.DT("![A:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsLastN()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "LastN(Table)",
                TestUtils.DT("*[A:n]"),
                symbol);
            TestSimpleBindingSuccess(
                "LastN(Table, 1234)",
                TestUtils.DT("*[A:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsLeft()
        {
            TestSimpleBindingSuccess(
                "Left(\"foo\", 3)",
                DType.String);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Count:n]")));

            TestSimpleBindingSuccess(
                "Left(T, 3)",
                TestUtils.DT("*[Name:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Left(T, T2)",
                TestUtils.DT("*[Name:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Left(\"foo\", T2)",
                TestUtils.DT("*[Result:s]"),
                symbol);
        }

        [Theory]
        [InlineData("Left(T, 3)")]
        [InlineData("Left(\"foo\", T2)")]
        [InlineData("Left(T, T2)")]
        public void TexlFunctionTypeSemanticsLeft_ConsistentOneColumnTableResult(string script)
        {
            TestSimpleBindingSuccess(
                "Left(\"foo\", 3)",
                DType.String);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Count:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:s]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsLen()
        {
            TestSimpleBindingSuccess(
                "Len(\"foo\")",
                DType.Number);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "Len(T)",
                TestUtils.DT("*[Result:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsLen_ConsistentOneColumnTableResult()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "Len(T)",
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("LookUp(T, A = 3, B)", "s", "*[A:n, B:s]")]
        [InlineData("LookUp(T, (A + 2) / C = 3, B & D)", "s", "*[A:n, B:s, C:n, D:s]")]
        [InlineData("LookUp(T, A = 3)", "![A:n, B:s]", "*[A:n, B:s]")]
        [InlineData("LookUp(T, (A + 2) / C = 3)", "![A:n, B:s, C:n, D:s]", "*[A:n, B:s, C:n, D:s]")]
        public void TexlFunctionTypeSemanticsLookUp(string script, string returnTypeString, string nameResolverTypeString)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT(nameResolverTypeString)));
            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(returnTypeString),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsLower()
        {
            TestSimpleBindingSuccess(
                "Lower(\"FOO\")",
                DType.String);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "Lower(T)",
                TestUtils.DT("*[Name:s]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsLower_ConsistentOneColumnTableResult()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            TestSimpleBindingSuccess(
                "Lower(T)",
                TestUtils.DT("*[Value:s]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsMid()
        {
            TestSimpleBindingSuccess(
                "Mid(\"hello\", 2, 3)",
                DType.String);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Start:n]")));
            symbol.AddVariable("T3", new TableType(TestUtils.DT("*[Count:n]")));

            TestSimpleBindingSuccess(
                "Mid(T, 2, 3)",
                TestUtils.DT("*[Name:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Mid(T, T2, 3)",
                TestUtils.DT("*[Name:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Mid(T, 2, T3)",
                TestUtils.DT("*[Name:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Mid(T, T2, T3)",
                TestUtils.DT("*[Name:s]"),
                symbol);

            TestSimpleBindingSuccess(
                "Mid(\"hello\", T2, T3)",
                TestUtils.DT("*[Result:s]"),
                symbol);
        }

        [Theory]
        [InlineData("Mid(T, 2, 3)")]
        [InlineData("Mid(T, T2, 3)")]
        [InlineData("Mid(T, 2, T3)")]
        [InlineData("Mid(T, T2, T3)")]
        [InlineData("Mid(\"hello\", T2, T3)")]
        public void TexlFunctionTypeSemanticsMid_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Start:n]")));
            symbol.AddVariable("T3", new TableType(TestUtils.DT("*[Count:n]")));

            TestSimpleBindingSuccess(
               script,
               TestUtils.DT("*[Value:s]"),
               symbol,
               new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Max(1, 2, 3, 4)")]
        [InlineData("Max(1, A, 2, A)")]
        [InlineData("Max(Table, A)")]
        [InlineData("Min(1, 2, 3, 4)")]
        [InlineData("Min(1, A, 2, A)")]
        [InlineData("Min(Table, A)")]

        public void TexlFunctionTypeSemanticsMinMax(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));

            TestSimpleBindingSuccess(
                script,
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("Max(\"3\")")]
        [InlineData("Max(\"3\", 4)")]
        [InlineData("Max(true, 4)")]
        [InlineData("Max(true, \"5\", 6)")]
        [InlineData("Min(\"3\")")]
        [InlineData("Min(\"3\", 4)")]
        [InlineData("Min(true, 4)")]
        [InlineData("Min(true, \"5\", 6)")]
        public void TexlFunctionTypeSemanticsMinMaxWithCoercion(string script)
        {
            TestSimpleBindingSuccess(script, DType.Number);
        }

        [Theory]
        [InlineData("Min(1, 2, A, B, C)")]
        [InlineData("Max(1, 2, A, B, C)")]
        public void TexlFunctionTypeSemanticsMinMaxTypedGlobalWithCoercion(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("B", FormulaType.String);
            symbol.AddVariable("C", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                DType.Number,
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsNot()
        {
            TestSimpleBindingSuccess(
                "Not(true)",
                DType.Boolean);

            TestSimpleBindingSuccess(
                "Not(1)",
                DType.Boolean);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsNow()
        {
            TestSimpleBindingSuccess(
                "Now()",
                DType.DateTime);
        }

        [Theory]
        [InlineData("Or(true)")]
        [InlineData("Or(true, false, true)")]
        [InlineData("Or(1, 0)")]
        [InlineData("Or(1, And(0, 2))")]
        public void TexlFunctionTypeSemanticsOr(string script)
        {
            TestSimpleBindingSuccess(
                script,
                DType.Boolean);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsRand()
        {
            TestSimpleBindingSuccess(
                "Rand()",
                DType.Number);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsRandBetween()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("B", FormulaType.Number);

            TestSimpleBindingSuccess(
                    "RandBetween(A, B)",
                    DType.Number,
                    symbol);
        }

        [Theory]
        [InlineData("Replace(\"hello\", 2, 3, \"X\")", "s")]
        [InlineData("Replace(T, 2, 3, \"X\")", "*[Name:s]")]
        [InlineData("Replace(T, T2, 3, \"X\")", "*[Name:s]")]
        [InlineData("Replace(T, 2, T3, \"X\")", "*[Name:s]")]
        [InlineData("Replace(T, T2, T3, \"X\")", "*[Name:s]")]
        [InlineData("Replace(T, T2, T3, TX)", "*[Name:s]")]
        [InlineData("Replace(\"hello\", T2, T3, TX)", "*[Result:s]")]
        public void TexlFunctionTypeSemanticsReplace(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Start:n]")));
            symbol.AddVariable("T3", new TableType(TestUtils.DT("*[Count:n]")));
            symbol.AddVariable("TX", new TableType(TestUtils.DT("*[Replacement:s]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Replace(T, 2, 3, \"X\")")]
        [InlineData("Replace(T, T2, 3, \"X\")")]
        [InlineData("Replace(T, 2, T3, \"X\")")]
        [InlineData("Replace(T, T2, T3, \"X\")")]
        [InlineData("Replace(T, T2, T3, TX)")]
        [InlineData("Replace(\"hello\", T2, T3, TX)")]
        public void TexlFunctionTypeSemanticsReplace_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Start:n]")));
            symbol.AddVariable("T3", new TableType(TestUtils.DT("*[Count:n]")));
            symbol.AddVariable("TX", new TableType(TestUtils.DT("*[Replacement:s]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:s]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsInt()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[A:n]")));

            TestSimpleBindingSuccess(
                "Int(1234)",
                DType.Number);

            TestSimpleBindingSuccess(
                "Int(T)",
                TestUtils.DT("*[A:n]"),
                symbol);
        }

        [Theory]
        [InlineData("Int(\"3\")", "n", null)]
        [InlineData("Int(true)", "n", null)]
        [InlineData("Int(T)", "*[Booleans:n]", "*[Booleans:b]")]
        [InlineData("Int(T)", "*[Strings:n]", "*[Strings:s]")]
        [InlineData("Int([true, false, true])", "*[Value:n]", null)]
        [InlineData("Int([\"5\", \"6\"])", "*[Value:n]", null)]
        public void TexlFunctionTypeSemanticsIntWithCoercion(string script, string expectedType, string typedGlobal)
        {
            var symbol = new SymbolTable();
            
            if (typedGlobal != null)
            {
                symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal)));
                TestSimpleBindingSuccess(
                    script,
                    TestUtils.DT(expectedType),
                    symbol);
            }
            else
            {
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedType));
            }
        }

        [Theory]
        [InlineData("Int(T)", "*[Booleans:b]")]
        [InlineData("Int(T)", "*[Strings:s]")]
        [InlineData("Int(T)", "*[Number:n]")]
        public void TexlFunctionTypeSemanticsIntWithCoercion__ConsistentOneColumnTableResult(string script, string typedGlobal)
        {
            var symbol = new SymbolTable();

            symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal)));
            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsTruncOneParam()
        {
            TestSimpleBindingSuccess(
                "Trunc(1234)",
                DType.Number);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "Trunc(T)",
                TestUtils.DT("*[A:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsTruncOneParam_ConsistentOneColumnTableResult()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[A:n]")));
            TestSimpleBindingSuccess(
                "Trunc(T)",
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsTruncTwoParams()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[digits:n]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                "Trunc(1234.567, 4)",
                DType.Number);

            TestSimpleBindingSuccess(
                "Trunc(1234.567, T)",
                TestUtils.DT("*[Result:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "Trunc(X, T)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "Trunc(X, 4)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsRound()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[digits:n]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                "Round(1234.567, 4)",
                DType.Number);

            TestSimpleBindingSuccess(
                "Round(1234.567, T)",
                TestUtils.DT("*[Result:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "Round(X, T)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "Round(X, 4)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);
        }

        [Theory]
        [InlineData("Round(1234.567, T)")]
        [InlineData("Round(4, X)")]
        [InlineData("Round(X, 4)")]
        [InlineData("Round(X, T)")]
        public void TexlFunctionTypeSemanticsRound_ConsistentOneColumnTableResult(string expression)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[digits:n]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                expression,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsRoundUp()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[digits:n]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                "RoundUp(1234.567, 4)",
                DType.Number);

            TestSimpleBindingSuccess(
                "RoundUp(1234.567, T)",
                TestUtils.DT("*[Result:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "RoundUp(X, T)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "RoundUp(X, 4)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);
        }

        [Theory]
        [InlineData("RoundUp(1234.567, T)")]
        [InlineData("RoundUp(X, 4)")]
        [InlineData("RoundUp(X, T)")]
        public void TexlFunctionTypeSemanticsRoundUp_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[digits:n]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsRoundDown()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[digits:n]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                "RoundDown(1234.567, 4)",
                DType.Number);

            TestSimpleBindingSuccess(
                "RoundDown(1234.567, T)",
                TestUtils.DT("*[Result:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "RoundDown(X, T)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);

            TestSimpleBindingSuccess(
                "RoundDown(X, 4)",
                TestUtils.DT("*[Nnnuuummm:n]"),
                symbol);
        }

        [Theory]
        [InlineData("RoundDown(1234.567, T)")]
        [InlineData("RoundDown(X, T)")]
        [InlineData("RoundDown(X, 4)")]
        public void TexlFunctionTypeSemanticsRoundDown_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[digits:n]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Sort(Table, A)", "*[A:n, B:s, C:b, D:*[X:n]]")]
        [InlineData("Sort(Table, A, \"Ascending\")", "*[A:n, B:s, C:b, D:*[X:n]]")]
        [InlineData("Sort(Table, A, \"Descending\")", "*[A:n, B:s, C:b, D:*[X:n]]")]
        [InlineData("Sort(Table, B)", "*[A:n, B:s, C:b, D:*[X:n]]")]
        [InlineData("Sort(Table, C)", "*[A:n, B:s, C:b, D:*[X:n]]")]
        [InlineData("Sort(ShowColumns(Table,\"A\"), A)", "*[A:n]")]
        [InlineData("Sort(Table, B & \"hello\")", "*[A:n, B:s, C:b, D:*[X:n]]")]
        [InlineData("Sort(Table2, D.X & \"hello\")", "*[A:n, B:s, C:b, D:![X:n]]")]
        [InlineData("Sort(Table2, D.X + 2)", "*[A:n, B:s, C:b, D:![X:n]]")]
        public void TexlFunctionTypeSemanticsSort(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]")));
            symbol.AddVariable("X", new TableType(TestUtils.DT("*[Nnnuuummm:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("SortByColumns([1,2,3,4,5], \"Value\", \"Ascending\")", "*[Value:n]")]
        [InlineData("SortByColumns([1,2,3,4,5], \"Value\")", "*[Value:n]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), \"a\", \"Ascending\")", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), \"a\")", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\"}), \"a\", \"Ascending\", \"b\")", "*[a:n,b:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\"}), \"a\", \"Ascending\", \"b\", \"Descending\")", "*[a:n,b:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\", c:3}), \"a\", \"Ascending\", \"b\", \"Descending\", \"c\")", "*[a:n,b:s,c:n]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\", c:3}), \"a\", \"Ascending\", \"b\", \"Descending\", \"c\", \"Ascending\")", "*[a:n,b:s,c:n]")]
        [InlineData("SortByColumns([1,2,3,4,5], First([\"Value\"]).Value, \"Ascending\")", "*[Value:n]")]
        [InlineData("SortByColumns([1,2,3,4,5], First([\"Value\"]).Value)", "*[Value:n]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), If(true,\"a\"), \"Ascending\")", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), If(true,\"a\"))", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\"}), If(true,\"a\"), \"Ascending\", \"b\")", "*[a:n,b:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\"}), \"a\", \"Ascending\", If(true,\"b\"), \"Descending\")", "*[a:n,b:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\", c:3}), If(true,\"a\"), \"Ascending\", If(true,\"b\"), \"Descending\", \"c\")", "*[a:n,b:s,c:n]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\", c:3}), If(true,\"c\"), \"Ascending\", If(true,\"a\"), \"Descending\", If(true,\"b\"), \"Ascending\")", "*[a:n,b:s,c:n]")]
        [InlineData("SortByColumns([1,2,3,4,5], \"Value\", [3,4,5])", "*[Value:n]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), \"a\", Table({q:\"hello\"}))", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:1}), \"a\", Table({c:1}))", "*[a:n]")]
        [InlineData("SortByColumns(Table({a:true}), \"a\", Table({d:true}))", "*[a:b]")]
        [InlineData("SortByColumns(Table({a:DateTimeValue(\"21 Jan 2014\")}), \"a\", Table({e:DateTimeValue(\"21 Jan 2014\")}))", "*[a:d]")]
        [InlineData("SortByColumns(Table({a:TimeValue(\"11:15\")}), \"a\", Table({b:TimeValue(\"12:15\")}))", "*[a:T]")]
        [InlineData("SortByColumns(Table({a:DateValue(\"21 Jan 2014\")}), \"a\", Table({b:DateValue(\"21 Jan 2014\")}))", "*[a:D]")]
        [InlineData("SortByColumns(Table({a:2}), If(true,\"a\"),[3,4,5])", "*[a:n]")]
        [InlineData("SortByColumns(Table({a:2}), stringVar, SortOrder.Ascending)", "*[a:n]")]
        [InlineData("SortByColumns(T, \"A\", SortOrder.Ascending, \"B\", SortOrder.Ascending)", "*[A:n, B:b, C:s]", "*[A:n, B:b, C:s]")]
        [InlineData("SortByColumns(T, \"A\", SortOrder.Ascending, \"B\", SortOrder.Ascending)", "*[A:n, B:b, C:s, H:h, I:i]", "*[A:n, B:b, C:s, H:h, I:i]")]
        public void TexlFunctionTypeSemanticsSortByColumns_PowerFxV1Disabled(string script, string expectedType, string tVariableType = null)
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddVariable("stringVar", FormulaType.String);
            if (tVariableType != null)
            {
                symbolTable.AddVariable("T", new TableType(TestUtils.DT(tVariableType)));
            }

            foreach (var columnNamesAsIdentifiers in new[] { false, true })
            {
                var features = new Features
                {
                    SupportColumnNamesAsIdentifiers = columnNamesAsIdentifiers,
                    PowerFxV1CompatibilityRules = false
                };

                var expectedDType = TestUtils.DT(expectedType);
                TestSimpleBindingSuccess(script, expectedDType, symbolTable: symbolTable, features: features);
            }
        }

        [Theory]
        [InlineData("SortByColumns([1,2,3,4,5], Value, SortOrder.Ascending)", "*[Value:n]")]
        [InlineData("SortByColumns([1,2,3,4,5], Value)", "*[Value:n]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), a, SortOrder.Ascending)", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), a)", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\"}), a, SortOrder.Ascending, b)", "*[a:n,b:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\"}), a, SortOrder.Ascending, b, SortOrder.Descending)", "*[a:n,b:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\", c:3}), a, SortOrder.Ascending, b, SortOrder.Descending, c)", "*[a:n,b:s,c:n]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\", c:3}), a, SortOrder.Ascending, b, SortOrder.Descending, c, SortOrder.Ascending)", "*[a:n,b:s,c:n]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\", c:3}), \"a\", SortOrder.Ascending, b, SortOrder.Descending, c)", "*[a:n,b:s,c:n]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\"}), If(true,\"a\"), SortOrder.Ascending, b)", "*[a:n,b:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\"}), a, SortOrder.Ascending, If(true,\"b\"), SortOrder.Descending)", "*[a:n,b:s]")]
        [InlineData("SortByColumns([1,2,3,4,5], Value, [3,4,5])", "*[Value:n]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), a, Table({q:\"hello\"}))", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:1}), a, Table({c:1}))", "*[a:n]")]
        [InlineData("SortByColumns(Table({a:true}), a, Table({d:true}))", "*[a:b]")]
        [InlineData("SortByColumns(Table({a:DateTimeValue(\"21 Jan 2014\")}), a, Table({e:DateTimeValue(\"21 Jan 2014\")}))", "*[a:d]")]
        [InlineData("SortByColumns(Table({a:TimeValue(\"11:15\")}), a, Table({b:TimeValue(\"12:15\")}))", "*[a:T]")]
        [InlineData("SortByColumns(Table({a:DateValue(\"21 Jan 2014\")}), a, Table({b:DateValue(\"21 Jan 2014\")}))", "*[a:D]")]
        public void TexlFunctionTypeSemanticsSortByColumns_ColumnsAsIdentifiersOn_PFxV1Off_Negative(string script, string expectedType)
        {
            var symbolTable = new SymbolTable();

            var features = new Features
            {
                SupportColumnNamesAsIdentifiers = true,
                PowerFxV1CompatibilityRules = false
            };

            var expectedDType = TestUtils.DT(expectedType);
            TestBindingErrors(script, expectedDType, symbolTable: symbolTable, features: features);
        }

        [Theory]
        [InlineData("SortByColumns([1,2,3,4,5], Value, SortOrder.Ascending)", "*[Value:n]")]
        [InlineData("SortByColumns([1,2,3,4,5], Value)", "*[Value:n]")]
        [InlineData("SortByColumns([1,2,3,4,5], \"Value\")", "*[Value:n]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), a, SortOrder.Ascending)", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), a)", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\"}), a, SortOrder.Ascending, b)", "*[a:n,b:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\"}), a, SortOrder.Ascending, b, SortOrder.Descending)", "*[a:n,b:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\", c:3}), a, SortOrder.Ascending, b, SortOrder.Descending, c)", "*[a:n,b:s,c:n]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\", c:3}), a, SortOrder.Ascending, b, SortOrder.Descending, c, SortOrder.Ascending)", "*[a:n,b:s,c:n]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\", c:3}), \"a\", SortOrder.Ascending, b, SortOrder.Descending, c)", "*[a:n,b:s,c:n]")]
        [InlineData("SortByColumns([1,2,3,4,5], First([\"Value\"]).Value, SortOrder.Ascending)", "*[Value:n]")]
        [InlineData("SortByColumns([1,2,3,4,5], First([\"Value\"]).Value)", "*[Value:n]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), If(true,\"a\"), SortOrder.Ascending)", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), If(true,\"a\"))", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\"}), If(true,\"a\"), SortOrder.Ascending, b)", "*[a:n,b:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\"}), a, SortOrder.Ascending, If(true,\"b\"), SortOrder.Descending)", "*[a:n,b:s]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\", c:3}), If(true,\"a\"), SortOrder.Ascending, If(true,\"b\"), SortOrder.Descending, \"c\")", "*[a:n,b:s,c:n]")]
        [InlineData("SortByColumns(Table({a:1, b:\"hello\", c:3}), If(true,\"c\"), SortOrder.Ascending, If(true,\"a\"), SortOrder.Descending, If(true,\"b\"), SortOrder.Ascending)", "*[a:n,b:s,c:n]")]
        [InlineData("SortByColumns([1,2,3,4,5], \"Value\", [3,4,5])", "*[Value:n]")]
        [InlineData("SortByColumns([1,2,3,4,5], Value, [3,4,5])", "*[Value:n]")]
        [InlineData("SortByColumns(Table({a:\"hello\"}), a, Table({q:\"hello\"}))", "*[a:s]")]
        [InlineData("SortByColumns(Table({a:1}), a, Table({c:1}))", "*[a:n]")]
        [InlineData("SortByColumns(Table({a:true}), a, Table({d:true}))", "*[a:b]")]
        [InlineData("SortByColumns(Table({a:DateTimeValue(\"21 Jan 2014\")}), a, Table({e:DateTimeValue(\"21 Jan 2014\")}))", "*[a:d]")]
        [InlineData("SortByColumns(Table({a:TimeValue(\"11:15\")}), a, Table({b:TimeValue(\"12:15\")}))", "*[a:T]")]
        [InlineData("SortByColumns(Table({a:DateValue(\"21 Jan 2014\")}), a, Table({b:DateValue(\"21 Jan 2014\")}))", "*[a:D]")]
        [InlineData("SortByColumns(Table({a:2}), If(true,\"a\"),[3,4,5])", "*[a:n]")]
        [InlineData("SortByColumns(Table({a:2}), stringVar, SortOrder.Ascending)", "*[a:n]")]
        [InlineData("SortByColumns(T, \"A\", SortOrder.Ascending, \"B\", SortOrder.Ascending)", "*[A:n, B:b, C:s]", "*[A:n, B:b, C:s]")]
        [InlineData("SortByColumns(T, \"A\", SortOrder.Ascending, \"B\", SortOrder.Ascending)", "*[A:n, B:b, C:s, H:h, I:i]", "*[A:n, B:b, C:s, H:h, I:i]")]
        [InlineData("SortByColumns(T, A, SortOrder.Ascending, B, SortOrder.Ascending)", "*[A:n, B:b, C:s]", "*[A:n, B:b, C:s]")]
        [InlineData("SortByColumns(T, A, SortOrder.Ascending, B, SortOrder.Ascending)", "*[A:n, B:b, C:s, H:h, I:i]", "*[A:n, B:b, C:s, H:h, I:i]")]
        public void TexlFunctionTypeSemanticsSortByColumns_ColumnsAsIdentifiersOn_PFxV1On(string script, string expectedType, string tVariableType = null)
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddVariable("stringVar", FormulaType.String);
            if (tVariableType != null)
            {
                symbolTable.AddVariable("T", new TableType(TestUtils.DT(tVariableType)));
            }

            var features = new Features
            {
                SupportColumnNamesAsIdentifiers = true,
                PowerFxV1CompatibilityRules = true
            };

            var expectedDType = TestUtils.DT(expectedType);
            TestSimpleBindingSuccess(script, expectedDType, symbolTable: symbolTable, features: features);
        }

        [Theory]
        [InlineData("SortByColumns([Now()], Value, [false, true])", "*[Value:d]")]
        [InlineData("SortByColumns([{a:[1,2,3]}], a)", "*[a:*[Value:n]]")]
        [InlineData("SortByColumns([1,2,3], Value2)", "*[Value:n]")]
        public void TexlFunctionTypeSemanticsSortByColumns_Negative_ColumnsAsIdentifiersEnabled(string script, string expectedType)
        {
            foreach (var pfxV1 in new[] { false, true })
            {
                var features = new Features
                {
                    TableSyntaxDoesntWrapRecords = true,
                    SupportColumnNamesAsIdentifiers = true,
                    PowerFxV1CompatibilityRules = pfxV1
                };

                var expectedDType = TestUtils.DT(expectedType);
                TestBindingErrors(script, expectedDType, features: features);
            }
        }

        [Theory]
        [InlineData("SortByColumns(T, \"a\")", "*[a:![b:n]]")]
        [InlineData("SortByColumns(T, \"a\")", "*[a:*[b:n]]")]
        [InlineData("SortByColumns(T, \"a\")", "*[a:O]")]
        public void TexlFunctionTypeSemanticsSortByColumns_Negative(string script, string tVariableType)
        {
            foreach (var pfxV1 in new[] { false, true })
            {
                var symbolTable = new SymbolTable();
                var tType = TestUtils.DT(tVariableType);
                symbolTable.AddVariable("T", new TableType(tType));

                var features = new Features
                {
                    SupportColumnNamesAsIdentifiers = true,
                    PowerFxV1CompatibilityRules = pfxV1
                };

                TestBindingErrors(script, tType, symbolTable: symbolTable, features: features);
            }
        }

        [Theory]
        [InlineData("SortByColumns(T, \"a\")", "*[a:i]")]
        [InlineData("SortByColumns(T, \"a\")", "*[a:h]")]
        [InlineData("SortByColumns(T, \"a\")", "*[a:o]")]
        [InlineData("SortByColumns(T, \"a\")", "*[a:m]")]
        [InlineData("SortByColumns(T, \"a\")", "*[a:g]")]
        public void TexlFunctionTypeSemanticsSortByColumns_Negative_PowerFxV1Enabled(string script, string tVariableType)
        {
            var features = Features.PowerFxV1;
            var symbolTable = new SymbolTable();
            var tType = TestUtils.DT(tVariableType);
            symbolTable.AddVariable("stringVar", FormulaType.String);
            if (tVariableType != null)
            {
                symbolTable.AddVariable("T", new TableType(tType));
            }

            TestBindingErrors(script, tType, symbolTable: symbolTable, features: features);
        }

        [Theory]
        [InlineData("Sqrt(1234.567)", "n")]
        [InlineData("Sqrt(T)", "*[A:n]")]
        [InlineData("Sqrt(ShowColumns(T2,\"Value\"))", "*[Value:n]")]
        public void TexlFunctionTypeSemanticsSqrt(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[A:n]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Name:s, Price:n, Value:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Sqrt(\"3\")", "n", null)]
        [InlineData("Sqrt(true)", "n", null)]
        [InlineData("Sqrt(T)", "*[Booleans:n]", "*[Booleans:b]")]
        [InlineData("Sqrt(T)", "*[Strings:n]", "*[Strings:s]")]
        [InlineData("Sqrt([true, false, true])", "*[Value:n]", null)]
        [InlineData("Sqrt([\"5\", \"6\"])", "*[Value:n]", null)]
        public void TexlFunctionTypeSemanticsSqrtWithCoercion(string script, string expectedType, string typedGlobal)
        {
            if (typedGlobal != null)
            {
                var symbol = new SymbolTable();
                symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal)));

                TestSimpleBindingSuccess(
                    script, 
                    TestUtils.DT(expectedType), 
                    symbol);
            }
            else
            {
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedType));
            }
        }

        [Theory]
        [InlineData("Sqrt(T)", "*[Booleans:b]")]
        [InlineData("Sqrt(T)", "*[Number:n]")]
        [InlineData("Sqrt(T)", "*[Strings:s]")]
        public void TexlFunctionTypeSemanticsSqrtWithCoercion_ConsistentOneColumnTableResult(string script, string typedGlobal)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal)));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value: n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Sum(1, 2, 3, 4)")]
        [InlineData("Sum(1, A, 2, A)")]
        [InlineData("Sum(Table, A)")]
        [InlineData("Sum(Table2, D.X)")]
        [InlineData("Sum(Table, A + CountA(D.X))")]
        public void TexlFunctionTypeSemanticsSum(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]")));

            TestSimpleBindingSuccess(
                script,
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("Sum(\"3\")")]
        [InlineData("Sum(\"3\", 4)")]
        [InlineData("Sum(true, 4)")]
        [InlineData("Sum(true, \"5\", 6)")]
        public void TexlFunctionTypeSemanticsSumWithCoercion(string script)
        {
            TestSimpleBindingSuccess(script, DType.Number);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsSumTypedGlobalWithCoercion()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Boolean);
            symbol.AddVariable("B", FormulaType.String);
            symbol.AddVariable("C", FormulaType.Number);

            TestSimpleBindingSuccess(
                "Sum(1, 2, A, B, C)",
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("StdevP(1, 2, 3, 4)")]
        [InlineData("StdevP(1, A, 2, A)")]
        [InlineData("StdevP(Table, A)")]
        [InlineData("StdevP(Table2, D.X)")]
        [InlineData("StdevP(Table, A + CountA(ShowColumns(D,\"X\")))")]
        public void TexlFunctionTypeSemanticsStdevP(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]")));

            TestSimpleBindingSuccess(
                script,
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("StdevP(\"3\")")]
        [InlineData("StdevP(\"3\", 4)")]
        [InlineData("StdevP(true, 4)")]
        [InlineData("StdevP(true, \"5\", 6)")]
        public void TexlFunctionTypeSemanticsStdevPWithCoercion(string script)
        {
            TestSimpleBindingSuccess(script, DType.Number);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsStdevPTypedGlobalWithCoercion()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Boolean);
            symbol.AddVariable("B", FormulaType.String);
            symbol.AddVariable("C", FormulaType.Number);
            
            TestSimpleBindingSuccess(
                "StdevP(1, 2, A, B, C)",
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("VarP(1, 2, 3, 4)")]
        [InlineData("VarP(1, A, 2, A)")]
        [InlineData("VarP(Table, A)")]
        [InlineData("VarP(Table2, D.X)")]
        [InlineData("VarP(Table, A + CountA(ShowColumns(D,\"X\")))")]
        public void TexlFunctionTypeSemanticsVarP(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Number);
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:*[X:n]]")));
            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[A:n, B:s, C:b, D:![X:n]]")));

            TestSimpleBindingSuccess(
                script,
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("VarP(\"3\")")]
        [InlineData("VarP(\"3\", 4)")]
        [InlineData("VarP(true, 4)")]
        [InlineData("VarP(true, \"5\", 6)")]
        public void TexlFunctionTypeSemanticsVarPWithCoercion(string script)
        {
            TestSimpleBindingSuccess(script, DType.Number);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsVarPTypedGlobalWithCoercion()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("A", FormulaType.Boolean);
            symbol.AddVariable("B", FormulaType.String);
            symbol.AddVariable("C", FormulaType.Number);

            TestSimpleBindingSuccess(
                "VarP(1, 2, A, B, C)",
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("Text(123)")]
        [InlineData("Text(123, \"#.#\")")]
        [InlineData("Text(123, \"$000.000\")")]
        [InlineData("Text(123, \"dddd, mm/dd/yy, at hh:mm:ss am/pm\")")]
        [InlineData("Text(\"hello world\")")]
        [InlineData("Text(1.23, \"[$-en-us]0.00\")")]
        [InlineData("Text(1.23, \"[$-fr-fr]0,00\")")]
        [InlineData("Text(123, \"yyyy-mm-dd hh:mm:ss.000\") // 0 is valid if after seconds")]
        [InlineData("Text(Now(), \"yyyy-mm-dd hh:mm:ss.000\") // 0 is valid if after seconds")]
        public void TexlFunctionTypeSemanticsText(string script)
        {
            TestSimpleBindingSuccess(
                script,
                DType.String);
        }

        [Theory]
        [InlineData("Text(123, \"###.####    dddd, mm/dd/yy, at hh:mm:ss am/pm\")")]
        [InlineData("Text(123, \"###.#### \" & \"   dddd, mm/dd/yy, at hh:mm:ss am/pm\")")]
        [InlineData("Text(Now(), \"yyyy-mm-dd 0 hh:mm:ss.000\") // 0 is only valid after seconds")]
        [InlineData("Text(\"hello world\", \"mm/dd/yyyy\") // not support format string for a string input")]
        public void TexlFunctionTypeSemanticsText_Negative(string script)
        {
            // Can't use both numeric formatting and date/time formatting within the same format string.
            TestBindingErrors(
                script,
                DType.String,
                expectedErrorCount: 2);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsToday()
        {
            TestSimpleBindingSuccess("Today()", DType.Date);
        }

        [Theory]
        [InlineData("IsToday(Date(2000, 1, 1))")]
        [InlineData("IsToday(DateTimeValue(\"Today\"))")]
        [InlineData("IsToday(\"now\")")]
        [InlineData("IsToday(1)")]
        public void TexlFunctionTypeSemanticsIsToday(string script)
        {
            TestSimpleBindingSuccess(script, DType.Boolean);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsWeekNum()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("S", FormulaType.Date);
            symbol.AddVariable("R", FormulaType.Number);
            symbol.AddVariable("W", FormulaType.Decimal);

            TestSimpleBindingSuccess(
                "WeekNum(S)",
                DType.Decimal,
                symbol,
                numberIsFloat: false);

            TestSimpleBindingSuccess(
                "WeekNum(S, R)",
                DType.Decimal,
                symbol,
                numberIsFloat: false);

            TestSimpleBindingSuccess(
                "WeekNum(S, W)",
                DType.Decimal,
                symbol,
                numberIsFloat: false);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsWeekNum_Float()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("S", FormulaType.Date);
            symbol.AddVariable("R", FormulaType.Number);
            symbol.AddVariable("W", FormulaType.Decimal);

            TestSimpleBindingSuccess(
                "WeekNum(S)",
                DType.Number,
                symbol,
                numberIsFloat: true);

            TestSimpleBindingSuccess(
                "WeekNum(S, R)",
                DType.Number,
                symbol,
                numberIsFloat: true);

            TestSimpleBindingSuccess(
                "WeekNum(S, W)",
                DType.Number,
                symbol,
                numberIsFloat: true);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsISOWeekNum()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("S", FormulaType.Date);

            TestSimpleBindingSuccess(
                "ISOWeekNum(S)",
                DType.Decimal,
                symbol,
                numberIsFloat: false);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsISOWeekNum_Float()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("S", FormulaType.Date);

            TestSimpleBindingSuccess(
                "ISOWeekNum(S)",
                DType.Number,
                symbol,
                numberIsFloat: true);
        }

        [Theory]
        [InlineData("IsToday(true)")]
        [InlineData("IsToday(Time(1,2,3))")]
        public void TexlFunctionTypeSemanticsIsToday_Negative(string script)
        {
            TestBindingErrors(script, DType.Boolean);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsTrim()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));

            TestSimpleBindingSuccess(
                "Trim(\"hello\")",
                DType.String);

            TestSimpleBindingSuccess(
                "Trim(T)",
                TestUtils.DT("*[Name:s]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsTrim_ConsistentOneColumnTableResult()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));

            TestSimpleBindingSuccess(
                "Trim(T)",
                TestUtils.DT("*[Value:s]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsTrimEnds()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));

            TestSimpleBindingSuccess(
                "TrimEnds(\" hello  \")",
                DType.String);

            TestSimpleBindingSuccess(
                "TrimEnds(T)",
                TestUtils.DT("*[Name:s]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsTrimEnds_ConsistentOneColumnTableResult()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));

            TestSimpleBindingSuccess(
                "TrimEnds(T)",
                TestUtils.DT("*[Value:s]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsUpper()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));

            TestSimpleBindingSuccess(
                "Upper(\"foo\")",
                DType.String);

            TestSimpleBindingSuccess(
                "Upper(T)",
                TestUtils.DT("*[Name:s]"),
                symbol);
        }

        [Fact]
        public void TexlFunctionTypeSemanticsUpper_ConsistentOneColumnTableResult()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));

            TestSimpleBindingSuccess(
                "Upper(T)",
                TestUtils.DT("*[Value:s]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Fact]
        public void TexlFunctionTypeSemanticsValue()
        {
            TestSimpleBindingSuccess(
                "Value(\"123.456\")",
                DType.Number);

            TestSimpleBindingSuccess(
                "Value(123.456)",
                DType.Number);
        }

        [Theory]
        [InlineData("a;")]
        [InlineData("a;b;c;d;e;")]
        [InlineData("a;a;a;a;")]
        [InlineData("If(A;B;,C;D;/*asdf*/,F;)")]
        public void TexlTestParsingChainRuleEndingSemicolon(string inputText)
        {
            var result = TexlParser.ParseScript(inputText, _defaultLocale, flags: TexlParser.Flags.EnableExpressionChaining);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.Root);
        }

        [Theory]
        [InlineData(";")]
        [InlineData("a;b;;")]
        public void TexlTestParsingChainRuleEndingSemicolon_negative(string inputText)
        {
            var result = TexlParser.ParseScript(inputText, _defaultLocale, flags: TexlParser.Flags.EnableExpressionChaining);
            Assert.True(result.HasError);
            Assert.NotNull(result.Root);
        }

        [Fact]
        public void TestTexlFunctionIsLambdaParamApi()
        {
            foreach (var func in BuiltinFunctionsCore.BuiltinFunctionsLibrary)
            {
                var maxArg = Math.Min(func.MaxArity, 1000);
                var foundLambdaParams = false;
                for (var i = 0; i < maxArg; i++)
                {
                    foundLambdaParams |= func.IsLambdaParam(i);
                }

                Assert.Equal(func.HasLambdas, foundLambdaParams);
            }
        }

        [Fact]
        public void TestTexlFunctionGetSignaturesApi()
        {
            foreach (var func in BuiltinFunctionsCore.BuiltinFunctionsLibrary)
            {
                IEnumerable<TexlStrings.StringGetter[]> signatures = func.GetSignatures();
                Assert.NotNull(signatures);
                Assert.True(func.MaxArity == 0 || signatures.Any(), "Failed on GetSignatures() for function " + func.Name);
            }
        }

        [Fact]
        public void TestWarningsOnEqualWithIncompatibleTypes()
        {
            TestBindingWarning("1 = \"hello\"", DType.Boolean, expectedErrorCount: 1);
            TestBindingWarning("1 <> \"hello\"", DType.Boolean, expectedErrorCount: 1);
            TestBindingWarning("true = 123", DType.Boolean, expectedErrorCount: 1);
            TestBindingWarning("true <> 123", DType.Boolean, expectedErrorCount: 1);
            TestBindingWarning("false = \"false\"", DType.Boolean, expectedErrorCount: 1);
            TestBindingWarning("false <> \"false\"", DType.Boolean, expectedErrorCount: 1);
        }

        [Fact]
        public void TexlBindingLambdaAs()
        {
            TestSimpleBindingSuccess("Filter([1,2,3] As Input, Input.Value > 2)", TestUtils.DT("*[Value:n]"));
            TestSimpleBindingSuccess("Filter([1,2,3], ThisRecord.Value > 2)", TestUtils.DT("*[Value:n]"));
            TestSimpleBindingSuccess("Filter([1,2,3] As ThisRecord, ThisRecord.Value > 2)", TestUtils.DT("*[Value:n]"));
            TestSimpleBindingSuccess("Filter([[1,2,3],[2,3,4],[4,5,6]] As Outer, CountRows(Filter(Outer.Value As Inner, Inner.Value > 4)) > 1)", TestUtils.DT("*[Value:*[Value:n]]"));
            TestSimpleBindingSuccess("Filter(Filter([1,2,3] As Left, Left.Value > 1) As Right, Right.Value > 2)", TestUtils.DT("*[Value:n]"));
            TestSimpleBindingSuccess("ForAll(Filter([1,2,3] As Input, Input.Value > 2) As Outer, {Inner: Outer.Value})", TestUtils.DT("*[Inner:n]"));
            TestSimpleBindingSuccess("ForAll(Filter([1,2,3] As Input, Input.Value > 2), {Inner: ThisRecord.Value})", TestUtils.DT("*[Inner:n]"));

            // Required ident after rename
            TestBindingErrors("Filter([1,2,3] As Input, Value > 2)", TestUtils.DT("*[Value:n]"));

            // Required ident after rename
            TestBindingErrors("Filter([1,2,3] As Input, ThisRecord.Value > 2)", TestUtils.DT("*[Value:n]"));

            // As must be immediate child of call node
            TestBindingErrors("Filter([1,2,3] As First As Second, Value > 2)", TestUtils.DT("*[Value:n]"));

            // If As is used, field access must be qualified
            TestBindingErrors("ForAll(Filter([1,2,3] As Input, Input.Value > 2) As Outer, {Inner: Value})", TestUtils.DT("*[Inner:e]"));

            // If As is used, field access must be qualified
            TestBindingErrors("Filter([1,2,3] As ThisRecord, Value > 2)", TestUtils.DT("*[Value:n]"));

            // If As is used, field access must be qualified
            TestBindingErrors("Filter([1,2,3] As Renamed, IsBlank(Text(Renamed.Value)) = Value)", TestUtils.DT("*[Value:n]"));
        }

        [Theory]
        [InlineData("[]", "*[]")]
        [InlineData("[1, 2, 3]", "*[Value:n]")]
        [InlineData("[true, true, false, true]", "*[Value:b]")]
        [InlineData("[\"a\", \"b\", \"c\"]", "*[Value:s]")]
        [InlineData("[\"a\", 2, 3, true, false, 123, \"hello\"]", "*[Value:s]")]
        [InlineData("[1, 2, \"3\", \"1234.243\", 6564.254, Abs(-123.22)]", "*[Value:n]")]
        public void TexlBindingTables(string script, string expectedType)
        {
            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType));
        }

        [Fact]
        public void TexlBindingHeterogeneousTables()
        {
            TestSimpleBindingSuccess(
                "[{a:1}, {b:2}, {c:\"hello\"}, {a:3, c:\"goodbye\"}, {a:0, b:2}, {b:123, c:\"foo\"}]",
                TestUtils.DT("*[Value:![a:n, b:n, c:s]]"));

            TestSimpleBindingSuccess(
                "[" +
                "{ id: 0, nestedData: [{ nestedId: \"a\" }, { nestedId: \"b\" }, { nestedId: \"c\" }] }," +
                "{ id: 1, nestedData: [{ nestedId: \"d\" }, { nestedId: \"e\" }] }," +
                "{ id: 2, nestedData: [{ nestedId: \"f\" }, { nestedId: \"g\" }, { nestedId: \"h\" }] }," +
                "{ id: 3, nestedData: [] }," +
                "{ id: 4, nestedData: [{ nestedId: \"i\" }] }]",
                TestUtils.DT("*[Value:![id:n, nestedData:*[Value:![nestedId:s]]]]"));
        }

        [Fact]
        public void TexlBindingPurity()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Ratings", new TableType(TestUtils.DT("*[A:n]")));
            symbol.AddVariable("Movies", new TableType(TestUtils.DT("*[Ratings:n]")));

            // Basic literal and operator bindings
            TestBindingPurity("2", true);
            TestBindingPurity("\"String literal\"", true);
            TestBindingPurity("false", true);
            TestBindingPurity("false or true", true);
            TestBindingPurity("2 + 2 = 6", true);
            TestBindingPurity("\"str1\" & \"str2\"", true);
            TestBindingPurity("error cond - 2", true);
            TestBindingPurity("-2", true);

            // Introduce function calls that don't have any impurity
            TestBindingPurity("If(true, 2 + 2, 3 + 3)", true);
            TestBindingPurity("If(false, 18 * 9)", true);
            TestBindingPurity("If(If(true, true, false), 12, 5)", true);
            TestBindingPurity("If(false, -2, -3)", true);

            // Introduce impurity and test how it propagates through various types of nodes
            TestBindingPurity("Rand()", false);
            TestBindingPurity("Rand() + 3", false);
            TestBindingPurity("\"hi\" & Rand()", false);
            TestBindingPurity("-Rand()", false);
            TestBindingPurity("If(true, Rand(), 2)", false);
            TestBindingPurity("If(false, 2, If(false, 3, If(true, Rand())))", false);

            // Test using lambdas with impure expressions
            TestBindingPurity("Filter(Ratings, A < CountIf(Movies, Ratings > Rand()))", false, symbol);
            TestBindingPurity("CountIf(Filter(Movies, Ratings < Rand()), true)", false, symbol);
        }

        [Fact]
        public void TexlBindingDisambiguatedGlobals()
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("foo", new TableType(TestUtils.DT("*[A:n, B:n, C:b]")));
            symbol.AddVariable("foo bar", new TableType(TestUtils.DT("*[A:n, B:n, C:b]")));

            TestSimpleBindingSuccess(
                "[@foo]",
                TestUtils.DT("*[A:n, B:n, C:b]"),
                symbol);
            TestSimpleBindingSuccess(
                "[@'foo bar']",
                TestUtils.DT("*[A:n, B:n, C:b]"),
                symbol);
        }

        [Fact]
        public void TestBindingErrorsOnDuplicateFieldNames()
        {
            TestBindingErrors("{A:1, A:2}", TestUtils.DT("![A:n]"));
            TestBindingErrors("{A:1, A:2, A:3}", TestUtils.DT("![A:n]"));
            TestBindingErrors("{A:1, B:true, A:2, B:3, A:3}", TestUtils.DT("![A:n, B:b]"));
            TestBindingErrors("{A:1, A:\"hello\"}", TestUtils.DT("![A:n]"));
            TestBindingErrors("{A:1, B:2, C:3, D:4, E:5, F:true, A:\"hello\"}", TestUtils.DT("![A:n, B:n, C:n, D:n, E:n, F:b]"));
        }

        [Theory]
        [InlineData("Find(\",\", \"LastName, FirstName\")", "n")]
        [InlineData("Find(\",\", \"LastName, FirstName\", 2)", "n")]
        [InlineData("Find(T, \"ll\")", "*[Result:n]")]
        [InlineData("Find(T, \"ll\", 2)", "*[Result:n]")]
        [InlineData("Find(T, \"ll\", T1)", "*[Result:n]")]
        [InlineData("Find(T, T2)", "*[Result:n]")]
        [InlineData("Find(T, T2, 2)", "*[Result:n]")]
        [InlineData("Find(T, T2, T1)", "*[Result:n]")]
        [InlineData("Find(\"ll\", T2)", "*[Result:n]")]
        [InlineData("Find(\"ll\", T2, 2)", "*[Result:n]")]
        [InlineData("Find(\"ll\", T2, T1)", "*[Result:n]")]
        [InlineData("Find(\"ll\", \"ll\", T1)", "*[Result:n]")]
        public void TexlFunctionTypeSemanticsFind(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[FindText:s]")));
            symbol.AddVariable("T1", new TableType(TestUtils.DT("*[StartIndex:n]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[WithinText:s]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Find(T, \"ll\")", "*[Value:n]")]
        [InlineData("Find(T, \"ll\", 2)", "*[Value:n]")]
        [InlineData("Find(T, \"ll\", T1)", "*[Value:n]")]
        [InlineData("Find(T, T2)", "*[Value:n]")]
        [InlineData("Find(T, T2, 2)", "*[Value:n]")]
        [InlineData("Find(T, T2, T1)", "*[Value:n]")]
        [InlineData("Find(\"ll\", T2)", "*[Value:n]")]
        [InlineData("Find(\"ll\", T2, 2)", "*[Value:n]")]
        [InlineData("Find(\"ll\", T2, T1)", "*[Value:n]")]
        [InlineData("Find(\"ll\", \"ll\", T1)", "*[Value:n]")]
        public void TexlFunctionTypeSemanticsFind_ConsistentOneColumnTableResult(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[FindText:s]")));
            symbol.AddVariable("T1", new TableType(TestUtils.DT("*[StartIndex:n]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[WithinText:s]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("a", "a")]
        [InlineData("'test'", "'''test'''")]
        [InlineData("te st", "'te st'")]
        [InlineData("te'st", "'te''st'")]
        [InlineData("'te' st'", "'''te'' st'''")]
        [InlineData("te!st", "'te!st'")]
        [InlineData("te.st", "'te.st'")]
        [InlineData("_", "_")]
        [InlineData("_a", "_a")]
        [InlineData("12", "'12'")]
        [InlineData("1-2", "'1-2'")]
        [InlineData("@", "'@'")]
        public void TexlEscapeNameTest(string input, string expected)
        {
            Assert.Equal(expected, TexlLexer.EscapeName(input));
        }

        [Theory]
        [InlineData("DateValue(\"21 Jan 2014\")")]
        [InlineData("DateValue(\"25 agosto 2014\", \"it\")")]
        public void TexlFunctionTypeSemanticsDateValue(string script)
        {
            TestSimpleBindingSuccess(script, DType.Date);
        }

        [Theory]
        [InlineData("TimeValue(\"21 Jan 2014 9:50 AM\")")]
        [InlineData("TimeValue(\"25 agosto 2014 9:50 AM\", \"it\")")]
        public void TexlFunctionTypeSemanticsTimeValue(string script)
        {
            TestSimpleBindingSuccess(script, DType.Time);
        }

        [Theory]
        [InlineData("Table()", "*[]")]
        [InlineData("Table({})", "*[]")]
        [InlineData("Table({X:1})", "*[X:n]")]
        [InlineData("Table({X:\"hello\"})", "*[X:s]")]
        [InlineData("Table({X:true})", "*[X:b]")]
        [InlineData("Table({X:true}, {})", "*[X:b]")]
        [InlineData("Table({}, {X:true})", "*[X:b]")]
        [InlineData("Table({X:false}, {}, {X:true})", "*[X:b]")]
        [InlineData("Table({X:false}, {}, {X:true}, {})", "*[X:b]")]
        [InlineData("Table({X:false}, {}, {}, {}, {}, {X:true}, {})", "*[X:b]")]
        [InlineData("Table({X:false, Y:123}, {X:true})", "*[X:b, Y:n]")]
        [InlineData("Table({X:false, Y:123}, {X:true}, {})", "*[X:b, Y:n]")]
        [InlineData("Table({X:false, Y:123}, {Y:99.9}, {X:true}, {})", "*[X:b, Y:n]")]
        [InlineData("Table({X:false, Y:123}, {Y:7, X:true}, {Y:99.9}, {X:true}, {})", "*[X:b, Y:n]")]
        [InlineData("Table({Y:123}, {Y:7}, {Y:99.9}, {X:true})", "*[X:b, Y:n]")]
        [InlineData("Table({Y:123}, {Z:true}, {X:\"hello\"}, {W:{A:1, B:true, C:\"hello\"}})", "*[X:s, Y:n, Z:b, W:![A:n, B:b, C:s]]")]
        [InlineData("Table({Nested:Table({Nested:Table({X:1})})})", "*[Nested:*[Nested:*[X:n]]]")]
        public void TexlFunctionTypeSemanticsTable(string script, string expectedType)
        {
            Assert.True(DType.TryParse(expectedType, out DType type));
            Assert.True(type.IsValid);
            TestSimpleBindingSuccess(script, type);
        }

        [Theory]
        [InlineData("Table(1, 2, 3)", "*[]")]
        [InlineData("Table(true, false)", "*[]")]
        [InlineData("Table(true, 2, \"hello\")", "*[]")]
        [InlineData("Table(\"hello\", \"world\")", "*[]")]
        public void TexlFunctionTypeSemanticsTable_Negative(string script, string expectedType)
        {
            Assert.True(DType.TryParse(expectedType, out DType type));
            Assert.True(type.IsValid);
            TestBindingErrors(script, type);
        }

        [Theory]
        [InlineData("Concat([], \"\")")]
        [InlineData("Concat([1, 2, 3], Text(Value))")]
        [InlineData("Concat(Table({a:1, b:\"hello\"}, {b:\"world\"}), b)")]
        [InlineData("Concat([1, 2, 3], Text(Value), \",\")")]
        [InlineData("Concat([1, 2, 3], Text(Value), Text(Today()))")]
        [InlineData("Concat([], 1)")]
        [InlineData("Concat([1, 2, 3], Value)")]        
        [InlineData("Concat([\"a\", \"b\", \"C\"], Value, 1)")]
        public void TexlFunctionTypeSemanticsConcat(string script)
        {
            TestSimpleBindingSuccess(script, DType.String);
        }

        [Theory]
        [InlineData("Concat(Table({a:1, b:\"hello\"}, {b:\"world\"}), [\"hello\", \"world\"])")]
        [InlineData("Concat([1, 2, 3], {Value:Value})")]
        [InlineData("Concat([1, 2, 3], Value, {a:1})")]
        [InlineData("Concat({a:1,b:\"hello\"}, b)")]
        public void TexlFunctionTypeSemanticsConcat_Negative(string script)
        {
            TestBindingErrors(script, DType.String);
        }

        [Theory]
        [InlineData("Mod(2, 3)", "n")]
        [InlineData("Mod(2, -2)", "n")]
        [InlineData("Mod(-2, 2)", "n")]
        [InlineData("Mod(-2, -2)", "n")]
        [InlineData("Mod(T, 2)", "*[Result:n]")]
        [InlineData("Mod(T, T2)", "*[Result:n]")]
        [InlineData("Mod(3, T2)", "*[Result:n]")]
        public void TexlFunctionTypeSemanticsModOverloads(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Number:n]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Dividend:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Mod(-2, -2)", "n")]
        [InlineData("Mod(T, 2)", "*[Value:n]")]
        [InlineData("Mod(T, T2)", "*[Value:n]")]
        [InlineData("Mod(3, T2)", "*[Value:n]")]
        public void TexlFunctionTypeSemanticsModOverloads_ConsistentOneColumnTableResult(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Number:n]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Dividend:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Mod(\"3\", 2)", "n", null, null)]
        [InlineData("Mod(\"3\", true)", "n", null, null)]
        [InlineData("Mod(\"5\", T)", "*[Result:n]", "*[Booleans:b]", null)]
        [InlineData("Mod(T1, T2)", "*[Result:n]", "*[Booleans:b]", "*[Strings:s]")]
        [InlineData("Mod(T, false)", "*[Result:n]", "*[Strings:s]", null)]
        [InlineData("Mod([true, false, true], \"2\")", "*[Result:n]", null, null)]
        [InlineData("Mod(12.5656242, [\"5\", \"6\"])", "*[Result:n]", null, null)]
        public void TexlFunctionTypeSemanticsModOverloadsWithCoercion(string script, string expectedType, string typedGlobal1, string typedGlobal2)
        {
            if (typedGlobal1 != null)
            {
                if (typedGlobal2 != null)
                {
                    var symbol = new SymbolTable();
                    symbol.AddVariable("T1", new TableType(TestUtils.DT(typedGlobal1)));
                    symbol.AddVariable("T2", new TableType(TestUtils.DT(typedGlobal2)));
                    TestSimpleBindingSuccess(script, TestUtils.DT(expectedType), symbol);
                }
                else
                {
                    var symbol = new SymbolTable();
                    symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal1)));
                    TestSimpleBindingSuccess(script, TestUtils.DT(expectedType), symbol);
                }
            }
            else
            {
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedType));
            }
        }

        [Theory]
        [InlineData("Mod(\"5\", T)", "*[Value:n]", "*[Booleans:b]", null)]
        [InlineData("Mod(T1, T2)", "*[Value:n]", "*[Booleans:b]", "*[Strings:s]")]
        [InlineData("Mod(T, false)", "*[Value:n]", "*[Strings:s]", null)]
        [InlineData("Mod([true, false, true], \"2\")", "*[Value:n]", null, null)]
        [InlineData("Mod(12.5656242, [\"5\", \"6\"])", "*[Value:n]", null, null)]
        public void TexlFunctionTypeSemanticsModOverloadsWithCoercion_ConsistentOneColumnTableResult(string script, string expectedType, string typedGlobal1, string typedGlobal2)
        {
            if (typedGlobal1 != null)
            {
                if (typedGlobal2 != null)
                {
                    var symbol = new SymbolTable();
                    symbol.AddVariable("T1", new TableType(TestUtils.DT(typedGlobal1)));
                    symbol.AddVariable("T2", new TableType(TestUtils.DT(typedGlobal2)));
                    TestSimpleBindingSuccess(script, TestUtils.DT(expectedType), symbol, new Features { ConsistentOneColumnTableResult = true });
                }
                else
                {
                    var symbol = new SymbolTable();
                    symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal1)));
                    TestSimpleBindingSuccess(script, TestUtils.DT(expectedType), symbol, new Features { ConsistentOneColumnTableResult = true });
                }
            }
            else
            {
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedType), features: new Features { ConsistentOneColumnTableResult = true });
            }
        }

        [Theory]
        [InlineData("Time(1,2,3)")]
        [InlineData("Time(1,2,3,4)")]
        [InlineData("Time(1,2, \"hello\")")]
        [InlineData("Time(1,2,3,\"hello\")")]
        public void TexlFunctionSemanticsTimeOverloads(string script)
        {
            TestSimpleBindingSuccess(script, DType.Time);
        }

        [Theory]
        [InlineData("Time(1)")]
        [InlineData("Time(1,2)")]
        public void TexlFunctionSemanticsTimeOverloadsNegative(string script)
        {
            TestBindingErrors(script, DType.Time);
        }

        [Theory(Timeout = 1000)]
        [InlineData(
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+" +
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+" +
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+" +
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+" +
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+" +
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+" +
            "0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9+0+1+2+3+4+5+6+7+8+9", "n")]
        public void TexlExcessivelyLongButFlatRulesParseCorrectly(string script, string expectedType)
        {
            TestSimpleBindingSuccess(script, TestUtils.DT(expectedType));
        }

        [Theory]
        [InlineData("123", 1)]
        [InlineData("true", 1)]
        [InlineData("\"hello\"", 1)]
        [InlineData("ABC", 1)]
        [InlineData("A!B", 2)]
        [InlineData("A.B", 2)]
        [InlineData("A!B!C", 3)]
        [InlineData("A.B.C", 3)]
        [InlineData("A!B!C!D!E!A!B!C!D!E!A!B!C!D!E!A!B!C!D!E!A!B!C!D!E!A!B!C!D!E!A!B!C!D!E", 35)]
        [InlineData("A.B.C.D.E.A.B.C.D.E.A.B.C.D.E.A.B.C.D.E.A.B.C.D.E.A.B.C.D.E.A.B.C.D.E", 35)]
        [InlineData("First(T)", 3)]
        [InlineData("First(Last(T))", 5)]
        [InlineData("A(B(C(D(E(A(B(C(D(E(A(B(C(D(E(A(B(C(D(E(T))))))))))))))))))))", 41)]
        [InlineData("A(B(), C(), D(), E(), F(), G(), H(), I(), J())", 4)]
        [InlineData("1 + 2", 2)]
        [InlineData("1 + 2 + 3 + 4 + 5", 5)]
        [InlineData("(((1 + 2) + 3) + 4) + 5", 5)]
        [InlineData("1 + (2 + (3 + (4 + 5)))", 5)]
        [InlineData("1 + (2 + 3)", 3)]
        [InlineData("(1 + 2) + (3 + 4)", 3)]
        [InlineData("(1 + 2) * 8 + (3 + 4) * 9", 4)]
        [InlineData("!X", 2)]
        [InlineData("-X", 2)]
        [InlineData("-123", 2)]
        [InlineData("1 - 2", 3)]
        [InlineData("1 - X", 3)]
        [InlineData("X in Y", 2)]
        [InlineData("A; B; C; D; E", 2)]
        [InlineData("1 + 2; B; true || false; D; E", 3)]
        [InlineData("1 + (2 + 3); B; true || false; D; E", 4)]
        [InlineData("Collect(T, 1); Navigate(S1,\"\")", 4)]
        [InlineData("{}", 1)]
        [InlineData("{A:1, B:2}", 2)]
        [InlineData("{A:1, B:2, C:{X:1, Y:2}}", 3)]
        [InlineData("[]", 1)]
        [InlineData("[1, 2, 3, 4, 5]", 2)]
        [InlineData("[{A:1}]", 3)]
        [InlineData("{A:[1,2,3]}", 3)]
        [InlineData("[1 + 2, 3 + 4]", 3)]
        public void TexlTreeDepth(string script, int expectedDepth)
        {
            var result = TexlParser.ParseScript(script, _defaultLocale, flags: TexlParser.Flags.EnableExpressionChaining);
            var node = result.Root;
            Assert.NotNull(node);
            Assert.Equal(expectedDepth, node.Depth);
        }

        [Fact]
        public void TexlFunctionThatSupportsCoercedParams()
        {
            const TestUtils.FunctionFlags functionFlags = TestUtils.FunctionFlags.IsStrict | TestUtils.FunctionFlags.IsStateless | TestUtils.FunctionFlags.IsSelfContained | TestUtils.FunctionFlags.SupportsParamCoercion;

            var churnDataFunction = new TestUtils.MockFunction(ChurnDataFunctionName, string.Empty, FunctionCategories.Text, functionFlags, DType.Number, 0, 2, 2, DType.Number, DType.Number);
            var symbol = new SymbolTable();
            
            try
            {
                symbol.AddFunction(churnDataFunction);
                TestSimpleBindingSuccess("ChurnData(\"123\", true)", DType.Number, symbol);
            }
            finally
            {
                symbol.RemoveFunction(churnDataFunction);
            }
        }

        [Theory]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride(\"123\", true)", true)]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride(123, true)", false)]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride([1,2,3], true)", false)]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride([\"1\",\"2\",\"3\"], \"2\")", false)]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride([\"1\",\"2\",\"3\"], true)", false)]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride([true,false,false], true)", false)]
        [InlineData("MockFunctionThatSupportsCoercedParamsWithOverride([\"1\",\"2\",\"3\"], 2)", false)]
        public void TexlFunctionThatSupportsSomeCoercedParams(string script, bool expectedErrors)
        {
            var mockFunction1 = new TestUtils.MockFunctionThatSupportsCoercedParamsWithOverride("MockFunctionThatSupportsCoercedParamsWithOverride", "_1", true, DType.Number, 0, 2, 2, DType.Number, DType.Number);
            var mockFunction2 = new TestUtils.MockFunctionThatSupportsCoercedParamsWithOverride("MockFunctionThatSupportsCoercedParamsWithOverride", "_2", true, DType.Number, 0, 2, 2, DType.EmptyTable, DType.Number)
            {
                CheckNumericTableOverload = true
            };
            var mockFunction3 = new TestUtils.MockFunctionThatSupportsCoercedParamsWithOverride("MockFunctionThatSupportsCoercedParamsWithOverride", "_3", false, DType.Number, 0, 2, 2, DType.EmptyTable, DType.String)
            {
                CheckStringTableOverload = true
            };
            var mockFunction4 = new TestUtils.MockFunctionThatSupportsCoercedParamsWithOverride("MockFunctionThatSupportsCoercedParamsWithOverride", "_4", false, DType.Number, 0, 2, 2, DType.EmptyTable, DType.Number)
            {
                CheckStringTableOverload = true
            };

            var symbol = new SymbolTable();

            try
            {
                symbol.AddFunction(mockFunction1);
                symbol.AddFunction(mockFunction2);
                symbol.AddFunction(mockFunction3);
                symbol.AddFunction(mockFunction4);

                if (expectedErrors)
                {
                    TestBindingErrors(script, DType.Number, symbol);
                }
                else
                {
                    TestSimpleBindingSuccess(script, DType.Number, symbol);
                }
            }
            finally
            {
                symbol.RemoveFunction(mockFunction1);
                symbol.RemoveFunction(mockFunction2);
                symbol.RemoveFunction(mockFunction3);
                symbol.RemoveFunction(mockFunction4);
            }
        }

        [Theory]
        [InlineData("MockFunction(\"123\", \"123\")", false)]
        [InlineData("MockFunction(true, true)", false)]
        [InlineData("MockFunction(\"123\", true)", false)]
        [InlineData("MockFunction(\"123\", 5)", false)]
        [InlineData("MockFunction([], [])", true)]
        public void TexlOverloadTakesPrecedenceOverCoercion(string script, bool expectedErrors)
        {
            var functionFlags = TestUtils.FunctionFlags.IsStrict | TestUtils.FunctionFlags.IsStateless | TestUtils.FunctionFlags.IsSelfContained;

            var mockFunction1 = new TestUtils.MockFunction("MockFunction", "_1", FunctionCategories.Text, functionFlags | TestUtils.FunctionFlags.SupportsParamCoercion, DType.Number, 0, 2, 2, DType.Number, DType.Number);
            var mockFunction2 = new TestUtils.MockFunction("MockFunction", "_2", FunctionCategories.Text, functionFlags, DType.Number, 0, 2, 2, DType.String, DType.String);
            var mockFunction3 = new TestUtils.MockFunction("MockFunction", "_3", FunctionCategories.Text, functionFlags | TestUtils.FunctionFlags.SupportsParamCoercion, DType.Number, 0, 2, 2, DType.String, DType.Number);
            
            var symbol = new SymbolTable();

            try
            {
                symbol.AddFunction(mockFunction1);
                symbol.AddFunction(mockFunction2);
                symbol.AddFunction(mockFunction3);

                if (expectedErrors)
                {
                    TestBindingErrors(script, DType.Number, symbol);
                }
                else
                {
                    TestSimpleBindingSuccess(script, DType.Number, symbol);
                }
            }
            finally
            {
                symbol.RemoveFunction(mockFunction1);
                symbol.RemoveFunction(mockFunction2);
                symbol.RemoveFunction(mockFunction3);
            }
        }

        [Fact]
        public void TestBlankFunction_Positive()
        {
            TestSimpleBindingSuccess("Blank()", TestUtils.DT("N"));
        }

        [Fact]
        public void TestBlankFunction_Negative()
        {
            TestBindingErrors("Blank(\"null\")", TestUtils.DT("N"));
        }

        [Theory]
        [InlineData(@"{%%:""hi""}", "![%%:s]", "![_:e]")]
        [InlineData(@"With( { %%: 3 }, %% )", "w", "?")]
        [InlineData(@"With( { %%: 3 }, { Value: %% } )", "![Value:w]", "?")]
        [InlineData(@"AddColumns( [1,2,3], %%, Value*3 )", "*[%%:w, Value:w]", "*[Value:w]")]
        public void TestReservedWords_Disallowed(string script, string successType, string failType)
        {
            // OkToUse should work, all others include As (an existing keyword) should fail
            string[] words = new string[] { "OkToUse", "As", "This", "blank", "null", "nothing", "undefined", "none", "empty", "Is", "Child", "Children", "Siblings" };

            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new Engine(config);
            var opts = new ParserOptions();

            foreach (var word in words)
            {
                var scriptWord = script.Replace("%%", word);
                var successTypeWord = TestUtils.DT(successType.Replace("%%", word));
                var failTypeWord = TestUtils.DT(failType.Replace("%%", word));
                var result = engine.Check(scriptWord, opts);

                if (word == "OkToUse")
                {
                    Assert.True(result.IsSuccess, $"should succeed: {word}");
                    Assert.Equal(successTypeWord, result.Binding.ResultType);
                }
                else
                {
                    Assert.False(result.IsSuccess, $"should fail: {word}");
                    Assert.Equal(failTypeWord, result.Binding.ResultType);
                }

                // should always work if enclosed in single quotes
                scriptWord = script.Replace("%%", $"'{word}'");
                successTypeWord = TestUtils.DT(successType.Replace("%%", word));
                failTypeWord = TestUtils.DT(failType.Replace("%%", word));
                result = engine.Check(scriptWord, opts);

                Assert.True(result.IsSuccess, $"should succeed with single quotes: {word}");
                Assert.Equal(successTypeWord, result.Binding.ResultType);
            }
        }

        [Theory]
        [InlineData(@"{%%:""hi""}", "![%%:s]", "![_:e]")]
        [InlineData(@"With( { %%: 3 }, %% )", "w", "?")]
        [InlineData(@"With( { %%: 3 }, { Value: %% } )", "![Value:w]", "?")]
        public void TestReservedWords_Allowed(string script, string successType, string failType)
        {
            // As should fail (an existing keyword), all others should work
            string[] words = new string[] { "OkToUse", "As", "This", "blank", "null", "nothing", "undefined", "none", "empty", "Is", "Child", "Children", "Siblings" };

            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var opts = new ParserOptions() { DisableReservedKeywords = true };

            foreach (var word in words)
            {
                var scriptWord = script.Replace("%%", word);
                var successTypeWord = TestUtils.DT(successType.Replace("%%", word));
                var failTypeWord = TestUtils.DT(failType.Replace("%%", word));
                var result = engine.Check(scriptWord, opts);

                if (word != "As")
                {
                    Assert.True(result.IsSuccess, $"should succeed: {word}");
                    Assert.Equal(successTypeWord, result.Binding.ResultType);
                }
                else
                {
                    Assert.False(result.IsSuccess, $"should fail: {word}");
                    Assert.Equal(failTypeWord, result.Binding.ResultType);
                }

                // should always work if enclosed in single quotes
                scriptWord = script.Replace("%%", $"'{word}'");
                successTypeWord = TestUtils.DT(successType.Replace("%%", word));
                failTypeWord = TestUtils.DT(failType.Replace("%%", word));
                result = engine.Check(scriptWord, opts);

                Assert.True(result.IsSuccess, $"should succeed with single quotes: {word}");
                Assert.Equal(successTypeWord, result.Binding.ResultType);
            }
        }

        [Theory]
        [InlineData("With({A: 1, B: \"test\"}, B & \" \" & A)", "![A:n, B:s]", "s")]
        [InlineData("With({table: [{name: \"first\"},{name: \"first\"}]}, ForAll(table, Value))", "![table:*[value:s]]", "*[name:s]")]
        [InlineData("With({date: Today()}, date)", "![date:D]", "D")]
        [InlineData("With({a: true, b: 5}, If(a, b+2, b-2))", "![a:b, b:n]", "n")]
        [InlineData("With({color: RGBA(255, 255, 255, 1)}, ThisRecord)", "", "![color:c]")]
        [InlineData("With({color: RGBA(255, 255, 255, 1)}, ThisRecord.color)", "![color:c]", "c")]
        [InlineData("With({color: RGBA(255, 255, 255, 1)} As Outer, Outer.color)", "![color:c]", "c")]
        [InlineData("With({r: 255, g: 255, b: 255, a: 1}, {color: RGBA(r, g, b, a)})", "![r:n, g:n, b:n, a:n]", "![color: c]")]
        public void TexlFunctionTypeSemanticsWith(string script, string typedGlobal, string expectedTypeString)
        {
            if (string.IsNullOrEmpty(typedGlobal))
            {
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedTypeString));
            }
            else
            {
                var symbol = new SymbolTable();
                symbol.AddVariable("T", new KnownRecordType(TestUtils.DT(typedGlobal)));
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedTypeString), symbol);
            }
        }

        [Theory]
        [InlineData("With({table: DoesntExist}, 1)", "![table:e]", "n")]
        [InlineData("With(1)", "", "?")]
        public void TexlFunctionTypeSemanticsWith_Negative(string script, string typedGlobal, string expectedTypeString)
        {
            if (string.IsNullOrEmpty(typedGlobal))
            {
                TestBindingErrors(script, TestUtils.DT(expectedTypeString));
            }
            else
            {
                var symbol = new SymbolTable();
                symbol.AddVariable("T", new KnownRecordType(TestUtils.DT(typedGlobal)));
                TestBindingErrors(script, TestUtils.DT(expectedTypeString), symbol);
            }
        }

        [Theory]
        [InlineData("ForAll(T, \"A: \" & A)", "*[A:n]", "*[Value:s]")]
        [InlineData("ForAll(T, Rec.Item)", "*[Rec:![Item:o]]", "*[Value:o]")]
        [InlineData("ForAll(T, Rec)", "*[Rec:![Item:o]]", "*[Item:o]")]
        [InlineData("ForAll(T, {Num:A+5, Image:img})", "*[A:n, img:i, unused:s]", "*[Num:n, Image:i]")]
        [InlineData("ForAll(T, FirstN(table, 3))", "*[table:*[A:n]]", "*[Value:*[A:n]]")]
        [InlineData("ForAll(Table({Str:\"hello\", Num:5}), Num + 5)", "", "*[Value:n]")]
        public void TexlFunctionTypeSemanticsForAll(string script, string typedGlobal, string expectedTypeString)
        {
            if (string.IsNullOrEmpty(typedGlobal))
            {
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedTypeString));
            }
            else
            {
                var symbol = new SymbolTable();
                symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal)));
                TestSimpleBindingSuccess(script, TestUtils.DT(expectedTypeString), symbol);
            }
        }

        [Theory]
        [InlineData("ForAll(DoesntExist, 1)", "", "?")]
        [InlineData("ForAll(T, DoesntExist + 1)", "*[Exists:n]", "*[Value:n]")]
        public void TexlFunctionTypeSemanticsForAll_Negative(string script, string typedGlobal, string expectedTypeString)
        {
            if (string.IsNullOrEmpty(typedGlobal))
            {
                TestBindingErrors(script, TestUtils.DT(expectedTypeString));
            }
            else
            {
                var symbol = new SymbolTable();
                symbol.AddVariable("T", new TableType(TestUtils.DT(typedGlobal)));
                TestBindingErrors(script, TestUtils.DT(expectedTypeString), symbol);
            }
        }

        [Theory]
        [InlineData("Sum(T, 1)", "w")]
        [InlineData("Average(T, \"Item\")", "w")]
        [InlineData("Filter(T, true)", "*[Item:n]")]
        [InlineData("Sum(TW, 1)", "w")]
        [InlineData("Average(TW, \"Item\")", "w")]
        [InlineData("Filter(TW, true)", "*[Item:w]")]
        public void TestWarningOnLiteralPredicate(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Item:n]")));
            symbol.AddVariable("TW", new TableType(TestUtils.DT("*[Item:w]")));
            TestBindingWarning(
                script,
                TestUtils.DT(expectedType),
                expectedErrorCount: null,
                symbolTable: symbol);
        }

        [Theory]
        [InlineData("Sum(T, 1)", "n")]
        [InlineData("Average(T, \"Item\")", "n")]
        [InlineData("Filter(T, true)", "*[Item:n]")]
        [InlineData("Sum(TW, 1)", "n")]
        [InlineData("Average(TW, \"Item\")", "n")]
        [InlineData("Filter(TW, true)", "*[Item:w]")]
        public void TestWarningOnLiteralPredicate_NumberIsFloat(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Item:n]")));
            symbol.AddVariable("TW", new TableType(TestUtils.DT("*[Item:w]")));
            TestBindingWarning(
                script,
                TestUtils.DT(expectedType),
                expectedErrorCount: null,
                symbolTable: symbol,
                numberIsFloat: true);
        }

        [Theory]
        [InlineData("Log(2)")]
        [InlineData("Log(2, 7)")]
        [InlineData("Log(numvar)")]
        [InlineData("Log(numvar, 7)")]
        public void TexlFunctionTypeSemanticsLog(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("numvar", FormulaType.Number);

            TestSimpleBindingSuccess(
                script, 
                DType.Number,
                symbol);
        }

        [Theory]
        [InlineData("Log(numvar, numtable)")]
        [InlineData("Log(numtable, numvar)")]
        [InlineData("Log(3, numtable)")]
        [InlineData("Log(numtable, 3)")]
        [InlineData("Log(numtable, numtable)")]
        public void TexlFunctionTypeSemanticsLog_T(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("numvar", FormulaType.Number);
            symbol.AddVariable("numtable", new TableType(TestUtils.DT("*[Num:n]")));

            TestSimpleBindingSuccess(
                script, 
                TestUtils.DT("*[Num:n]"),
                symbol);
        }

        [Theory]
        [InlineData("Log(3, numtable)")]
        [InlineData("Log(numtable, 3)")]
        [InlineData("Log(numtable, numtable)")]
        public void TexlFunctionTypeSemanticsLog_T_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("numtable", new TableType(TestUtils.DT("*[Num:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Log(errortable)")]
        [InlineData("Log(errortable, 3)")]
        [InlineData("Log(numbertable, errortable)")]
        public void TexlFunctionTypeSemanticsLog_T_Negative(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("errortable", new TableType(TestUtils.DT("*[Num:n, Other:s]")));
            symbol.AddVariable("numtable", new TableType(TestUtils.DT("*[Num:n]")));

            // Expected type is "n" because when we fail typechecking for all overloads, the binder picks the first one
            TestBindingErrors(
                script, 
                TestUtils.DT("n"),
                symbol);
        }

        [Theory]
        [InlineData("StartsWith(\"Hello\", \"He\")")]
        [InlineData("StartsWith(name, \"He\")")]
        [InlineData("StartsWith(\"Hello\", name)")]
        [InlineData("StartsWith(name, otherName)")]
        public void TexlFunctionTypeSemanticsStartsWith(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("name", FormulaType.String);
            symbol.AddVariable("otherName", FormulaType.String);

            TestSimpleBindingSuccess(
                script, 
                DType.Boolean,
                symbol);
        }

        [Theory]
        [InlineData("EndsWith(\"Hello\", \"lo\")")]
        [InlineData("EndsWith(name, \"lo\")")]
        [InlineData("EndsWith(\"Hello\", name)")]
        [InlineData("EndsWith(name, otherName)")]
        public void TexlFunctionTypeSemanticsEndsWith(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("name", FormulaType.String);
            symbol.AddVariable("otherName", FormulaType.String);

            TestSimpleBindingSuccess(
                script, 
                DType.Boolean,
                symbol);
        }

        [Theory]
        [InlineData("Coalesce(\"Hello\", \"He\")", "s")]
        [InlineData("Coalesce(1, Today())", "n")]
        [InlineData("Coalesce(Blank(), 1, Today())", "n")]
        [InlineData("Coalesce(name, url)", "s")]
        [InlineData("Coalesce(url, name)", "s")]
        [InlineData("Coalesce(url)", "h")]
        [InlineData("Coalesce(col)", "*[testname:s]")]
        [InlineData("Coalesce(Blank())", "N")]
        [InlineData("Coalesce(Blank(), Blank())", "N")]
        public void TexlFunctionTypeSemanticsCoalesce(string script, string typeSpec)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("name", FormulaType.String);
            symbol.AddVariable("url", FormulaType.Hyperlink);
            symbol.AddVariable("col", new TableType(TestUtils.DT("*[testname:s]")));

            TestSimpleBindingSuccess(
                script, 
                TestUtils.DT(typeSpec),
                symbol);
        }

        [Theory]
        [InlineData("Coalesce(\"Hello\", Color.Brown)", "s")]
        [InlineData("Coalesce(1, Color.Brown)", "n")]
        public void TexlFunctionTypeSemanticsCoalesce_Negative(string script, string typeSpec)
        {
            TestBindingErrors(script, TestUtils.DT(typeSpec));
        }

        [Theory]
        [InlineData("IsError(\"Hello\")", "b")]
        [InlineData("IsError(7)", "b")]
        [InlineData("IsError(1 / 0)", "b")]
        [InlineData("IsError(Sum(7, 42, 5))", "b")]
        [InlineData("IsError([1,2,3,4])", "b")]
        [InlineData("IsError({a:3, b:4})", "b")]
        public void TexlFunctionTypeSemanticsIsError(string script, string typeSpec)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("name", FormulaType.String);

            TestSimpleBindingSuccess(
                script, 
                TestUtils.DT(typeSpec),
                symbol);
        }

        [Theory]
        [InlineData("IsError(1, 2)", "b")]
        [InlineData("IsError()", "b")]
        public void TexlFunctionTypeSemanticsIsError_Negative(string script, string typeSpec)
        {
            TestBindingErrors(script, TestUtils.DT(typeSpec));
        }

        [Theory]
        [InlineData("IsEmpty(\"Hello\")", true)]
        [InlineData("IsEmpty(7)", true)]
        [InlineData("IsEmpty([1,2,3,4])", false)]
        [InlineData("IsEmpty({a:3, b:4})", true)]
        [InlineData("IsEmpty(Blank())", false)]
        public void TexlFunctionTypeSemanticsIsEmpty(string script, bool expectedError)
        {
            TestSimpleBindingSuccess(script, DType.Boolean); // Without restriction, all succeed

            if (expectedError)
            {
                TestBindingErrors(
                    script,
                    DType.Boolean,
                    symbolTable: null,
                    features: new Features { RestrictedIsEmptyArguments = true });
            }
            else
            {
                TestSimpleBindingSuccess(
                    script,
                    DType.Boolean,
                    features: new Features { RestrictedIsEmptyArguments = true });
            }
        }

        [Theory]
        [InlineData("Sequence(20)", "*[Value:n]")]
        [InlineData("Sequence(20, 30)", "*[Value:n]")]
        [InlineData("Sequence(20, 30, 10)", "*[Value:n]")]
        public void TexlFunctionTypeSemanticsSequence(string script, string typeSpec)
        {
            TestSimpleBindingSuccess(script, TestUtils.DT(typeSpec));
        }

        [Theory]
        [InlineData("\"Hello World\" //Line Comment", "s")]
        [InlineData("6 //Line Comment \n * 4", "n")]
        [InlineData("\"Hello World\" /*Block Comment*/", "s")]
        [InlineData("/*Block Comment*/ \"Hello World\"", "s")]
        [InlineData("1 + 2 //Line Comment", "n")]
        [InlineData("1 + 2/*Block Comment*/", "n")]
        [InlineData("1 + /*Block Comment*/ 2", "n")]
        [InlineData("1 /*Block Comment*/ + 2", "n")]
        [InlineData("/*Block Comment*/ 1 + 2", "n")]
        public void TexlTestCommentingSemantics(string script, string typeSpec)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("name", FormulaType.String);
            var temp = TestUtils.DT("%s[X:\"hi world\"]");
            
            TestSimpleBindingSuccess(
                script, 
                TestUtils.DT(typeSpec),
                symbol);
        }

        [Theory]
        [InlineData("Abs(1)", "n")]
        [InlineData("Abs(A)", "n")]
        [InlineData("Abs(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsAbs(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Abs(Table)")]
        public void TexlFunctionTypeSemanticsAbs_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Acos(1)", "n")]
        [InlineData("Acos(A)", "n")]
        [InlineData("Acos(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsAcos(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Acos(Table)")]
        public void TexlFunctionTypeSemanticsAcos_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Acot(1)", "n")]
        [InlineData("Acot(A)", "n")]
        [InlineData("Acot(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsAcot(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Acot(Table)")]
        public void TexlFunctionTypeSemanticsAcot_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Asin(1)", "n")]
        [InlineData("Asin(A)", "n")]
        [InlineData("Asin(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsAsin(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Asin(Table)")]
        public void TexlFunctionTypeSemanticsAsin_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Atan(1)", "n")]
        [InlineData("Atan(A)", "n")]
        [InlineData("Atan(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsAtan(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Atan(Table)")]
        public void TexlFunctionTypeSemanticsAtan_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Boolean(1)", "b")]
        [InlineData("Boolean(A)", "b")]
        [InlineData("Boolean(Table)", "*[Value:b]")]
        [InlineData("Boolean(TableS)", "*[Value:b]")]
        public void TexlFunctionTypeSemanticsBoolean(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("TableS", new TableType(TestUtils.DT("*[input:s]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Boolean(Table)")]
        [InlineData("Boolean(TableS)")]
        public void TexlFunctionTypeSemanticsBoolean_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("TableS", new TableType(TestUtils.DT("*[input:s]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:b]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("ColorFade(Color.Red, 0.5)", "c")]
        [InlineData("ColorFade(Table, 0.5)", "*[input:c]")]
        [InlineData("ColorFade(Color.Red, TableN)", "*[Result:c]")]
        [InlineData("ColorFade(Table, TableN)", "*[input:c]")]
        public void TexlFunctionTypeSemanticsColorFade(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:c]")));
            symbol.AddVariable("TableN", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("ColorFade(Table, 0.5)")]
        [InlineData("ColorFade(Color.Red, TableN)")]
        [InlineData("ColorFade(Table, TableN)")]
        public void TexlFunctionTypeSemanticsColorFade_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:c]")));
            symbol.AddVariable("TableN", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:c]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Cos(1)", "n")]
        [InlineData("Cos(A)", "n")]
        [InlineData("Cos(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsCos(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Cos(Table)")]
        public void TexlFunctionTypeSemanticsCos_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Cot(1)", "n")]
        [InlineData("Cot(A)", "n")]
        [InlineData("Cot(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsCot(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Cot(Table)")]
        public void TexlFunctionTypeSemanticsCot_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Degrees(1)", "n")]
        [InlineData("Degrees(A)", "n")]
        [InlineData("Degrees(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsDegrees(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Degrees(Table)")]
        public void TexlFunctionTypeSemanticsDegrees_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Exp(1)", "n")]
        [InlineData("Exp(A)", "n")]
        [InlineData("Exp(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsExp(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Exp(Table)")]
        public void TexlFunctionTypeSemanticsExp_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Ln(1)", "n")]
        [InlineData("Ln(A)", "n")]
        [InlineData("Ln(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsLn(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Ln(Table)")]
        public void TexlFunctionTypeSemanticsLn_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Power(4,3)", "n")]
        [InlineData("Power(Table,3)", "*[input:n]")]
        [InlineData("Power(4, Table2)", "*[input:n]")]
        [InlineData("Power(Table, Table2)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsPower(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Power(Table,3)")]
        [InlineData("Power(4, Table2)")]
        [InlineData("Power(Table, Table2)")]
        public void TexlFunctionTypeSemanticsPower_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("Table2", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Radians(1)", "n")]
        [InlineData("Radians(A)", "n")]
        [InlineData("Radians(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsRadians(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Radians(Table)")]
        public void TexlFunctionTypeSemanticsRadians_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            
            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Right(\"foo\", 3)", "s")]
        [InlineData("Right(T, 3)", "*[Name:s]")]
        [InlineData("Right(T, T2)", "*[Name:s]")]
        [InlineData("Right(\"foo\", T2)", "*[Result:s]")]
        public void TexlFunctionTypeSemanticsRight(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Count:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Right(T, 3)")]
        [InlineData("Right(\"foo\", T2)")]
        [InlineData("Right(T, T2)")]
        public void TexlFunctionTypeSemanticsRight_ConsistentOneColumnTableResult(string script)
        {
            TestSimpleBindingSuccess(
                "Right(\"foo\", 3)",
                DType.String);

            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Count:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:s]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Sin(1)", "n")]
        [InlineData("Sin(A)", "n")]
        [InlineData("Sin(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsSin(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Sin(Table)")]
        public void TexlFunctionTypeSemanticsSin_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Tan(1)", "n")]
        [InlineData("Tan(A)", "n")]
        [InlineData("Tan(Table)", "*[input:n]")]
        public void TexlFunctionTypeSemanticsTan(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));
            symbol.AddVariable("A", FormulaType.Number);

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Tan(Table)")]
        public void TexlFunctionTypeSemanticsTan_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("Table", new TableType(TestUtils.DT("*[input:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value:n]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("Substitute(T, T2, T3, TN)", "*[Name:s]")]
        [InlineData("Substitute(T,\"S1\", \"S2\", 1)", "*[Name:s]")]
        [InlineData("Substitute(\"S1\",T, \"S2\", 1)", "*[Result:s]")]
        [InlineData("Substitute(\"S1\",\"S2\", T, 1)", "*[Result:s]")]
        [InlineData("Substitute(\"S1\",\"S2\", \"S3\", TN)", "*[Result:s]")]
        public void TexlFunctionTypeSemanticsSubstitute(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Name2:s]")));
            symbol.AddVariable("T3", new TableType(TestUtils.DT("*[Name3:s]")));
            symbol.AddVariable("TN", new TableType(TestUtils.DT("*[Number:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Substitute(T, T2, T3, TN)")]
        [InlineData("Substitute(T,\"S1\", \"S2\", 1)")]
        [InlineData("Substitute(\"S1\",T, \"S2\", 1)")]
        [InlineData("Substitute(\"S1\",\"S2\", T, 1)")]
        [InlineData("Substitute(\"S1\",\"S2\", \"S3\", TN)")]
        public void TexlFunctionTypeSemanticsSubstitute_ConsistentOneColumnTableResult(string script)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Name2:s]")));
            symbol.AddVariable("T3", new TableType(TestUtils.DT("*[Name3:s]")));
            symbol.AddVariable("TN", new TableType(TestUtils.DT("*[Number:n]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT("*[Value: s]"),
                symbol,
                new Features { ConsistentOneColumnTableResult = true });
        }

        [Theory]
        [InlineData("First(First(DS).Attach).Name", "s")]
        [InlineData("First(First(DS).Attach).Value", "o")]
        [InlineData("If(false, First(DS).Attach, First(DS).Attach)", "*[Value:o, Name:s, Link:s]")]
        [InlineData("First(If(false, DS, DS)).Attach", "*[Value:o, Name:s, Link:s]")]
        [InlineData("First(First(If(false, DS, DS)).Attach).Link", "s")]
        public void TestAttachmentDottedNameNodeBinding(string script, string expectedSchema)
        {
            var schema = DType.CreateTable(new TypedName(DType.CreateAttachmentType(TestUtils.DT("*[Value:o, Name:s, Link:s]")), new DName("Attach")), new TypedName(TestUtils.DT("b"), new DName("Value")));

            var expectedType = TestUtils.DT(expectedSchema);

            var symbol = new SymbolTable();
            symbol.AddEntity(new TestDataSource("DS", schema));

            TestSimpleBindingSuccess(script, expectedType, symbol);
        }

        [Theory]
        [InlineData("Search(T, \"a\", \"Name\")", "*[Name:s]")]
        [InlineData("Search(T, If(1<0,\"a\"), \"Name\")", "*[Name:s]")]
        [InlineData("Search(T2,\"a\", \"Name\", \"Address\")", "*[Name:s,Age:n,Address:s]")]
        public void TexlFunctionTypeSemanticsSearch(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Name:s,Age:n,Address:s]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol);
        }

        [Theory]
        [InlineData("Search(T, \"a\", Name)", "*[Name:s]")]
        [InlineData("Search(T, If(1<0,\"a\"), Name)", "*[Name:s]")]
        [InlineData("Search(T2,\"a\", Name, Address)", "*[Name:s,Age:n,Address:s]")]
        public void TexlFunctionTypeSemanticsSearch_SupportColumnNamesAsIdentifiers(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Name:s,Age:n,Address:s]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol,
                new Features { SupportColumnNamesAsIdentifiers = true });
        }

        [Theory]

        // 1st arg needs to be a table.
        [InlineData("Search({Name: \"test\"}, \"a\", \"Name\")", "*[]")]

        // missing 3rd arg
        [InlineData("Search(T, \"a\")", "*[]")]

        // Age is number, only string supported.
        [InlineData("Search(T2, \"a\", \"Age\")", "*[Address:s, Age:n, Name:s]")]
        [InlineData("Search(T3, \"a\", \"Age\")", "*[Age:n]")]

        // 3rd onwards Arg should always be constant string.
        [InlineData("Search(T2, \"a\", If(1<0, \"Name\"))", "*[Address:s, Age:n, Name:s]")]

        // 2nd Arg should only be a string.
        [InlineData("Search(T2, 1, \"Name\")", "*[]")]

        // missing field.
        [InlineData("Search(T2, \"a\", \"Name\", \"Address\", \"Does not exist\")", "*[Address:s, Age:n, Name:s]")]
        public void TexlFunctionTypeSemanticsSearch_Negative(string script, string expectedSchema)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Name:s,Age:n,Address:s]")));
            symbol.AddVariable("T3", new TableType(TestUtils.DT("*[Age:n]")));

            TestBindingErrors(
                script,
                TestUtils.DT(expectedSchema),
                symbolTable: symbol);
        }

        [Theory]
        
        // 1st arg needs to be a table.
        [InlineData("Search({Name: \"test\"}, \"a\", Name)", "*[]")]

        // missing 3rd arg
        [InlineData("Search(T, \"a\")", "*[]")]

        // Age is number, only string supported.
        [InlineData("Search(T2, \"a\", Age)", "*[Address:s, Age:n, Name:s]")]
        [InlineData("Search(T3, \"a\", Age)", "*[Age:n]")]

        // 3rd onwards Arg should always be identifier.
        [InlineData("Search(T2, \"a\", If(1<0, Name))", "*[Address:s, Age:n, Name:s]")]

        // 2nd Arg should only be a string.
        [InlineData("Search(T2, 1, Name)", "*[]")]

        // missing field.
        [InlineData("Search(T2, \"a\", Name, Address, 'Does not exist')", "*[Address:s, Age:n, Name:s]")]
        public void TexlFunctionTypeSemanticsSearch_SupportColumnNamesAsIdentifiers_Negative(string script, string expectedSchema)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[Name:s]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[Name:s,Age:n,Address:s]")));
            symbol.AddVariable("T3", new TableType(TestUtils.DT("*[Age:n]")));

            TestBindingErrors(
                script,
                TestUtils.DT(expectedSchema),
                symbolTable: symbol,
                features: new Features { SupportColumnNamesAsIdentifiers = true });
        }

        [Fact]
        public void TestAttachmentIf()
        {
            var schema = DType.CreateTable(new TypedName(DType.CreateAttachmentType(TestUtils.DT("*[Value:o, Name:s, Link:s]")), new DName("Attach")), new TypedName(TestUtils.DT("b"), new DName("Value")));

            var symbol = new SymbolTable();
            symbol.AddEntity(new TestDataSource("DS", schema));

            TestSimpleBindingSuccess("If(true, DS, DS)", schema, symbol);
        }

        [Theory]
        [InlineData("*Filter*(DS, StartsWith(Value, \"d\"))", false)]
        [InlineData("*Filter*(DS, Left(Value, 1) = \"d\")", true)]
        [InlineData("*Filter*(DS, Substitute(Value, \"x\", \"y\"))", true)]
        [InlineData("*Filter*(DS, Value(Value) <= 3 Or Value(Value) > 7)", true)]
        [InlineData("*Filter*(DS, IsBlank(First(*Filter*(DS, StartsWith(Value, \"d\")))))", true)]
        public void TestSilentValidDelegatableFilterPredicateNode(string script, bool warnings)
        {
            var schema = DType.CreateTable(new TypedName(TestUtils.DT("s"), new DName("Value")));

            var symbol = new DelegatableSymbolTable();
            symbol.AddEntity(
                new TestDelegableDataSource(
                    "DS",
                    schema,
                    new TestDelegationMetadata(
                        DelegationCapability.Filter,
                        schema,
                        new FilterOpMetadata(
                            schema,
                            new Dictionary<DPath, DelegationCapability>(),
                            new Dictionary<DPath, DelegationCapability>(),
                            new DelegationCapability(DelegationCapability.Equal | DelegationCapability.StartsWith),
                            null))));

            var silentFilterFunction = new TestUtils.MockSilentDelegableFilterFunction("TestSilentFilter", script);

            try
            {
                symbol.AddFunction(silentFilterFunction);

                var config = new PowerFxConfig
                {
                    SymbolTable = symbol
                };

                var engine = new Engine(config);

                // first run using the original Filter
                var filterScript = script.Replace("*Filter*", "Filter");
                var result = engine.Check(filterScript);

                Assert.True(result.IsSuccess);

                if (warnings)
                {
                    Assert.True(result.Errors.Count() > 0, "Expected warnings in original function");
                }
                else
                {
                    Assert.False(result.Errors.Count() > 0, "No warnings expected in original function");
                }

                // then run with the mock filter function that does silent delgation checks
                var silentFilterScript = script.Replace("*Filter*", "TestSilentFilter");
                result = engine.Check(silentFilterScript);

                Assert.True(result.IsSuccess);
                Assert.False(result.Errors.Count() > 0, "No warnings expected in silent function");
            }
            finally
            {
                symbol.RemoveFunction(silentFilterFunction);
            }
        }

        [Theory]

        // Single column access syntax is supported pre PowerFxV1 rules.
        [InlineData("T.A", "*[A:n]", false)]
        [InlineData("T.B", "*[B:![C:s]]", false)]

        // Single column access syntax is not supported PowerFxV1 rules onwards.
        [InlineData("T.A", "e", true)]
        [InlineData("T.B", "e", true)]
        public void TestSingleColumnAccessDependentOnTableType(string script, string expectedSchema, bool usePowerFxV1Rules)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T", new TableType(TestUtils.DT("*[A:n, B:![C:s]]")));

            if (usePowerFxV1Rules)
            {
                var features = new Features() { PowerFxV1CompatibilityRules = true };
                TestBindingErrors(
                    script,
                    TestUtils.DT(expectedSchema),
                    symbol,
                    features: features);
            }
            else
            {
                TestSimpleBindingSuccess(
                                   script,
                                   TestUtils.DT(expectedSchema),
                                   symbol,
                                   features: Features.None);
            }
        }

        [Theory]
        [InlineData("ColumnNames(ParseJSON('Hello'))")]
        [InlineData("ColumnNames(ParseJSON('[]'))")]
        [InlineData("ColumnNames(ParseJSON('Hello').a)")]
        [InlineData("ColumnNames(ParseJSON('{''a'':1}').a)")]
        [InlineData("ColumnNames(Blank())")]
        [InlineData("ColumnNames(ParseJSON('{''a'':{''b'':1}}').a)")]
        public void TexlFunctionTypeSemanticsColumnNames(string expression)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression.Replace('\'', '\"'));

            Assert.True(DType.TryParse("*[Value:s]", out var expectedDType));
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("ColumnNames({a:1,b:true})")] // Does not work with records
        [InlineData("ColumnNames([1,2,3])")] // Does not work with arrays
        [InlineData("ColumnNames(1)")]
        public void TexlFunctionTypeSemanticsColumnNames_Negative(string expression)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression);

            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("Column(ParseJSON('Hello'), 'a')")] // Compiles, would fail at runtime
        [InlineData("Column(ParseJSON('[]'), 'Value')")] // Compiles, would fail at runtime
        [InlineData("Column(ParseJSON('Hello').a, 'a')")] // Compiles, would fail at runtime
        [InlineData("Column(ParseJSON('{''a'':1}'), 'a')")]
        [InlineData("Column(ParseJSON('{''a'':1}').a, 'a')")] // Compiles, would fail at runtime
        [InlineData("Column(Blank(), 'a')")]
        [InlineData("Column(ParseJSON('{''a'':{''b'':1}}'), 'a')")]
        public void TexlFunctionTypeSemanticsColumn(string expression)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression.Replace('\'', '\"'));

            Assert.Equal(DType.UntypedObject, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("Column({a:1,b:true}, 'a')")] // Does not work with records
        [InlineData("Column([1,2,3], 'Value')")] // Does not work with arrays
        [InlineData("Column(1)")]
        [InlineData("Column(1, 'Value')")]
        public void TexlFunctionTypeSemanticColumn_Negative(string expression)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression.Replace('\'', '\"'));

            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("in")]
        [InlineData("exactin")]
        public void PrettyPrintTest(string op)
        {
            string expr = $"1 {op} [0]";
            
            CheckResult check = new CheckResult(new Engine()).SetText(expr);
            ParseResult parse = check.ApplyParse();

            string str = parse.Root.ToString();

            Assert.Equal($"1 {op} [ 0 ]", str);
        }

        internal class BlobFunc : TexlFunction
        {
            public override bool IsSelfContained => false;

            public BlobFunc()
                : base(DPath.Root, "BlobFunc", "BlobFunc", (_) => "Returns a blob", FunctionCategories.Behavior, DType.Blob, 0, 0, 0)
            {
            }

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                throw new NotImplementedException();
            }
        }

        [Theory]
        [InlineData("BlobFunc()", "o")]
        [InlineData("blobVar", "o")]
        public void TestBlobFunction(string expression, string expectedType)
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddFunction(new BlobFunc());
            symbolTable.AddVariable("blobVar", FormulaType.Blob);

            var config = new PowerFxConfig()
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var opts = new ParserOptions() { AllowsSideEffects = true };
            var result = engine.Check(expression, opts);
            var expectedDType = TestUtils.DT(expectedType);
            Assert.Equal(expectedDType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("Table(Blank(), Blank())", "*[]")]
        [InlineData("Table([1, 2, 3, 4], Blank())", "*[Value:n]")]
        [InlineData("Table([{a:0, b:false, c:\"Hello\"}], [{a:1, b:true, c:\"World\"}])", "*[a:n, b:b, c:s]")]
        [InlineData("Table([{a:0}], [{b:true}], [{c:\"Hello\"}])", "*[a:n, b:b, c:s]")]
        [InlineData("Table(Blank(), T2, Blank(), T1)", "*[a:n, b:b, c:s]")]
        [InlineData("Table([{a:0}], [{b:true}], [{c:\"Hello\", d: {x: \"World\"}}])", "*[a:n, b:b, c:s, d:![x:s]]")]
        [InlineData("Table(T1, T2)", "*[a:n, b:n, c:n]")]
        [InlineData("Table(T2, T1)", "*[a:n, b:b, c:s]")]
        [InlineData("Table(T2, Table(T1, T2))", "*[a:n, b:b, c:s]")]
        [InlineData("Table(T1, Table(T2, T3))", "*[a:n, b:n, c:n, d:n]")]
        [InlineData("Table(Table(T1, T2), T3)", "*[a:n, b:n, c:n, d:n]")]
        [InlineData("Table(T1, Table(T2, T4))", "*[a:n, b:n, c:n]")]
        [InlineData("Table(Table(T1, T2), T4)", "*[a:n, b:n, c:n]")]
        [InlineData("Table([1,2], If(1/0<2,[3,4]), [5,6])", "*[Value: n]")]
        public void TexlFunctionTypeSemanticsTableConcatenate(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T1", new TableType(TestUtils.DT("*[a:n, b:n, c:n]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[a:n, b:b, c:s]")));
            symbol.AddVariable("T3", new TableType(TestUtils.DT("*[d:n]")));
            symbol.AddVariable("T4", new TableType(TestUtils.DT("*[a:s]")));

            TestSimpleBindingSuccess(
                script,
                TestUtils.DT(expectedType),
                symbol,
                Features.PowerFxV1);
        }

        [Theory]
        [InlineData("Table(T1, T2)", "*[V: n]")]
        [InlineData("Table([{a:Date(2024,1,1)}], [{a:GUID(\"some-guid-value-1234\")}])", "*[a: D]")]
        public void TexlFunctionTypeSemanticsTableConcatenate_Negative(string script, string expectedType)
        {
            var symbol = new SymbolTable();
            symbol.AddVariable("T1", new TableType(TestUtils.DT("*[V:n]")));
            symbol.AddVariable("T2", new TableType(TestUtils.DT("*[V:![a:n]]")));

            TestBindingErrors(
                script,
                TestUtils.DT(expectedType),
                symbol,
                features: Features.PowerFxV1);
        }

        [Theory]
        [InlineData("Table(DS, Blank())", "*[Id:n, Name:s, Age:n]", 1)]
        [InlineData("Table(DS, T1)", "*[Id:n, Name:s, Age:n, a:n, b:s]", 1)]
        [InlineData("Table(DS, Filter(DS, \"Foo\" in Name))", "*[Id:n, Name:s, Age:n]", 2)]
        [InlineData("Table([], Table(DS, []))", "*[Id:n, Name:s, Age:n]", 2)]
        [InlineData("Table([], Table([], Search(DS, \"Foo\", Name)))", "*[Id:n, Name:s, Age:n]", 2)]
        [InlineData("Table([], FirstN(DS, 5))", "*[Id:n, Name:s, Age:n]", 1)]
        [InlineData("Table(Search(DS, \"Foo\", Name), FirstN(LastN(DS, 10), 5))", "*[Id:n, Name:s, Age:n]", 2)]
        [InlineData("Table(Filter(DS, Sqrt(Age) > 5), FirstN(LastN(DS, 10), 5))", "*[Id:n, Name:s, Age:n]", 2)]
        public void TexlFunctionTypeSemanticsTableConcatenate_Delegation_Negative(string script, string expectedSchema, int errorCount)
        {
            var dataSourceSchema = TestUtils.DT("*[Id:n, Name:s, Age:n]");

            var symbol = new SymbolTable();
            symbol.AddEntity(new TestDataSource("DS", dataSourceSchema));
            symbol.AddVariable("T1", new TableType(TestUtils.DT("*[a:n, b:s]")));

            TestBindingWarning(
                script,
                TestUtils.DT(expectedSchema),
                errorCount,
                symbol,
                features: Features.PowerFxV1);
        }

        private void TestBindingPurity(string script, bool isPure, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script);

            Assert.NotNull(result.Binding);        
            Assert.Equal(isPure, result.Binding.IsPure(result.Parse.Root));
        }

        private void TestBindingWarning(string script, DType expectedType, int? expectedErrorCount, SymbolTable symbolTable = null, bool numberIsFloat = false, Features features = null)
        {
            var config = features != null ? new PowerFxConfig(features) : new PowerFxConfig(Features.None);
            var parserOptions = new ParserOptions()
            {
                NumberIsFloat = numberIsFloat
            };

            var engine = new Engine(config);
            var result = engine.Check(script, parserOptions, symbolTable);
            
            Assert.Equal(expectedType, result.Binding.ResultType);
            Assert.True(result.Binding.ErrorContainer.HasErrors());
            if (expectedErrorCount != null)
            {
                Assert.Equal(expectedErrorCount, result.Binding.ErrorContainer.GetErrors().Count());
            }

            Assert.True(result.IsSuccess);
        }

        private void TestBindingErrors(string script, DType expectedType, int expectedErrorCount, SymbolTable symbolTable = null)
        {
            var config = new PowerFxConfig
            {
                SymbolTable = symbolTable
            };

            var engine = new Engine(config);
            var result = engine.Check(script);

            Assert.Equal(expectedType, result.Binding.ResultType);
            Assert.Equal(expectedErrorCount, result.Binding.ErrorContainer.GetErrors().Count());
            Assert.False(result.IsSuccess);
        }

        private void TestBindingErrors(string script, DType expectedType, SymbolTable symbolTable = null, bool numberIsFloat = true, OptionSet[] optionSets = null, Features features = null)
        {
            features = features ?? Features.None;
            var config = new PowerFxConfig(features)
            {
                SymbolTable = symbolTable
            };

            if (optionSets != null)
            {
                foreach (var optionSet in optionSets)
                {
                    config.AddOptionSet(optionSet);
                }
            }

            var engine = new Engine(config);
            var opts = new ParserOptions() { NumberIsFloat = numberIsFloat };
            var result = engine.Check(script, opts);

            Assert.Equal(expectedType, result.Binding.ResultType);
            Assert.False(result.IsSuccess);
        }

        private static void TestSimpleBindingSuccess(string script, DType expectedType, SymbolTable symbolTable = null, Features features = null, bool numberIsFloat = true, IExternalOptionSet[] optionSets = null)
        {
            features ??= Features.None;
            var config = new PowerFxConfig(features)
            {
                SymbolTable = symbolTable
            };

            if (symbolTable != null)
            {
                if (optionSets != null)
                {
                    foreach (var optionSet in optionSets)
                    {
                        config.AddEntity(optionSet);
                    }
                }
            }

            var engine = new Engine(config);
            var opts = new ParserOptions() { NumberIsFloat = numberIsFloat };
            var result = engine.Check(script, opts);
            Assert.Equal(expectedType, result.Binding.ResultType);
            Assert.True(result.IsSuccess);
        }
    }
}
