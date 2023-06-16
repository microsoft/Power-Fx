// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Threading;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests.IRTests
{
    public class IRTests
    {
        [Fact]
        public void ValidateNoRecordToRecordAggregateCoercionCurrency()
        {
            var tableType = TableType.Empty().Add(new NamedFormulaType(new TypedName(DType.Currency, new DName("Currency"))));

            var symbols = new SymbolTable { DebugName = "ST1 " };
            symbols.EnableMutationFunctions();
            var slot = symbols.AddVariable("MyTable", tableType, mutable: true);

            var engine = new RecalcEngine(new PowerFxConfig());
            var checkResult = engine.Check("Patch(MyTable, { Currency: 1.2 }, { Currency: 1.5 })", new ParserOptions() { AllowsSideEffects = true, NumberIsFloat = true }, symbolTable: symbols);

            checkResult.ThrowOnErrors();

            var runtimeConfig = new SymbolValues(symbols) { DebugName = "SV1" };
            runtimeConfig.Set(slot, FormulaValue.NewTable(tableType.ToRecord()));

            var evalResult = checkResult.GetEvaluator().EvalAsync(CancellationToken.None, runtimeConfig).Result;
            Assert.IsType<ErrorValue>(evalResult);

            var ir = IRTranslator.Translate(checkResult.Binding).ToString();
            Assert.DoesNotContain("AggregateCoercionNode", ir);
        }

        [Fact]
        public void ValidateNoRecordToRecordAggregateCoercionDecimal()
        {
            var tableType = TableType.Empty().Add(new NamedFormulaType(new TypedName(DType.Decimal, new DName("Decimal"))));

            var symbols = new SymbolTable { DebugName = "ST1 " };
            symbols.EnableMutationFunctions();
            var slot = symbols.AddVariable("MyTable", tableType, mutable: true);

            var engine = new RecalcEngine(new PowerFxConfig());
            var checkResult = engine.Check("Patch(MyTable, { Decimal: 1.2 }, { Decimal: 1.5 })", new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbols);

            checkResult.ThrowOnErrors();

            var runtimeConfig = new SymbolValues(symbols) { DebugName = "SV1" };
            runtimeConfig.Set(slot, FormulaValue.NewTable(tableType.ToRecord()));

            var evalResult = checkResult.GetEvaluator().EvalAsync(CancellationToken.None, runtimeConfig).Result;
            Assert.IsType<ErrorValue>(evalResult);

            var ir = IRTranslator.Translate(checkResult.Binding).ToString();
            Assert.DoesNotContain("AggregateCoercionNode", ir);
        }

        [Theory]

        [InlineData("With({t1:Table({a:stringVar})},Patch(t1,First(t1),{a:integerVar}))")]
        [InlineData("With({t1:Table({a:5})},Patch(t1,First(t1),{a:datetimeVar}))")]
        public void DoNotUpdateScopeDefinedVariables(string expr)
        {
            var engine = new RecalcEngine(new PowerFxConfig());

            var stringVar = FormulaValue.New("lichess.org");
            var integerVar = FormulaValue.New(1);
            var datetimeVar = FormulaValue.New(DateTime.Now);

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.Config.SymbolTable.AddConstant("stringVar", stringVar);
            engine.Config.SymbolTable.AddConstant("integerVar", integerVar);
            engine.Config.SymbolTable.AddConstant("datetimeVar", datetimeVar);

            var checkResult = engine.Check(expr, options: new ParserOptions { AllowsSideEffects = true });
            Assert.False(checkResult.IsSuccess);
        }

        [Theory]
        [InlineData("Table({a:stringVar})", "Patch(t1,First(t1),{a:integerVar})")]
        [InlineData("Table({a:5})", "Patch(t1,First(t1),{a:datetimeVar})")]
        public void RecordToRecordAggregateCoercionTest(string initialTableValue, string expr)
        {
            var engine = new RecalcEngine(new PowerFxConfig());

            var stringVar = FormulaValue.New("lichess.org");
            var integerVar = FormulaValue.New(1);
            var datetimeVar = FormulaValue.New(DateTime.Now);

            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.Config.SymbolTable.AddConstant("stringVar", stringVar);
            engine.Config.SymbolTable.AddConstant("integerVar", integerVar);
            engine.Config.SymbolTable.AddConstant("datetimeVar", datetimeVar);

            var tableResult = engine.Eval(initialTableValue);
            engine.UpdateVariable("t1", tableResult);
            var parserOptions = new ParserOptions { AllowsSideEffects = true };
            var result = engine.Eval(expr, options: parserOptions);

            Assert.IsNotType<ErrorValue>(result);
        }

        private class BooleanOptionSet : OptionSet, IExternalOptionSet
        {
            public BooleanOptionSet(string name, DisplayNameProvider displayNameProvider)
                : base(name, displayNameProvider)
            {
            }

            public new bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue)
            {
                if (!Options.Any(option => option.Key == fieldName))
                {
                    optionSetValue = null;
                    return false;
                }

                optionSetValue = new OptionSetValue(fieldName, FormulaType, fieldName == "1");
                return true;
            }

            DKind IExternalOptionSet.BackingKind => DKind.Boolean;
        }

        private class InvalidBooleanOptionSet : OptionSet, IExternalOptionSet
        {
            public InvalidBooleanOptionSet(string name, DisplayNameProvider displayNameProvider)
                : base(name, displayNameProvider)
            {
            }

            public new bool TryGetValue(DName fieldName, out OptionSetValue optionSetValue)
            {
                if (!Options.Any(option => option.Key == fieldName))
                {
                    optionSetValue = null;
                    return false;
                }

                // Invalid, not passing a bool value for `value`
                optionSetValue = new OptionSetValue(fieldName, FormulaType);
                return true;
            }

            DKind IExternalOptionSet.BackingKind => DKind.Boolean;
        }

        [Theory]
        [InlineData("If(BoolOptionSet.Negative, \"YES\",\"NO\")", "NO")]
        [InlineData("BoolOptionSet.Positive & \" TEXT\"", "Positive TEXT")]
        public void BooleanOptionSetTest(string expression, string expected)
        {
            var engine = new RecalcEngine(new PowerFxConfig());
            var symbol = new SymbolTable();

            var boolOptionSetDisplayNameProvider = DisplayNameUtility.MakeUnique(new Dictionary<string, string>
            {
                { "1", "Positive" },
                { "0", "Negative" },
            });

            engine.Config.AddOptionSet(new BooleanOptionSet("BoolOptionSet", boolOptionSetDisplayNameProvider));

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = check.GetEvaluator().Eval() as StringValue;

            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("If(BoolOptionSet.Negative, \"YES\",\"NO\")")]
        public void BooleanOptionSetErrorTest(string expression)
        {
            var engine = new RecalcEngine(new PowerFxConfig());
            var symbol = new SymbolTable();

            var boolOptionSetDisplayNameProvider = DisplayNameUtility.MakeUnique(new Dictionary<string, string>
            {
                { "error", "Positive" },
                { "invalid", "Negative" },
            });

            engine.Config.AddOptionSet(new InvalidBooleanOptionSet("BoolOptionSet", boolOptionSetDisplayNameProvider));

            var check = engine.Check(expression);
            Assert.True(check.IsSuccess);

            var result = check.GetEvaluator().Eval();

            Assert.IsType<ErrorValue>(result);
            Assert.Contains("The value of option", ((ErrorValue)result).Errors.First().Message);
        }
        
        // Testing only currency to text and currency to decimal for the time being.
        // Other coercion from currency should be added later.
        [Theory]
        [InlineData("Concatenate(First(MyTable).Currency, \"$\")", "CurrencyToText:s(FieldAccess(First:![Currency:$]", "CurrencyToText:s(FieldAccess(First:![Currency:$]", "1$")]
        [InlineData("Concatenate(\"$\", First(MyTable).Currency)", "CurrencyToText:s(FieldAccess(First:![Currency:$]", "CurrencyToText:s(FieldAccess(First:![Currency:$]", "$1")]
        [InlineData("Concatenate(First(MyTable).Currency * 100, \"%\")", "Decimal:w(FieldAccess(First:![Currency:$]", "Value:n(FieldAccess(First:![Currency:$]", "100%")]
        [InlineData("Concatenate(Float(First(MyTable).Currency) * 100, \"%\")", "Float:n(FieldAccess(First:![Currency:$]", "Float:n(FieldAccess(First:![Currency:$]", "100%")]        
        [InlineData("Collect(MyTable, {Currency:99});Last(MyTable).Currency & \"$\"", null, null, "99$")]
        public void CurrencyToTextCoercionTest(string expr, string ir, string ir_float, string expected)
        {
            // Building table with Currency column type.
            var recordType = RecordType.Empty().Add(new NamedFormulaType(new TypedName(DType.Currency, new DName("Currency"))));
            var recordValue = FormulaValue.NewRecordFromFields(recordType, new List<NamedValue>() { new NamedValue("Currency", FormulaValue.New(1)) });
            var table = FormulaValue.NewTable(recordType, recordValue);

            var engine = new RecalcEngine();

            engine.Config.SymbolTable.EnableMutationFunctions();

            engine.UpdateVariable("MyTable", table);

            foreach (var useFloat in new[] { false, true })
            {
                var opt = new ParserOptions() { NumberIsFloat = useFloat, AllowsSideEffects = true };
                var check = engine.Check(expr, options: opt);
                Assert.True(check.IsSuccess);

                var translated = IRTranslator.Translate(check.Binding).ToString();

                if (ir != null && !useFloat)
                {
                    Assert.Contains(ir, translated);
                }

                if (ir_float != null && useFloat)
                {
                    Assert.Contains(ir_float, translated);
                }

                var result = check.GetEvaluator().Eval();
                Assert.IsType<StringValue>(result);
                Assert.Equal(expected, ((StringValue)result).Value);
            }
        }
    }
}
