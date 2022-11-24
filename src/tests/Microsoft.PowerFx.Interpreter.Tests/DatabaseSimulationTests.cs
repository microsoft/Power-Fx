// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Entities.Delegation;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DatabaseSimulationTests
    {
        [Theory]
        [InlineData("Patch(Table, First(Filter(Table, MyStr = \"Str3\")), {MyDate: \"2022-11-14 7:22:06 pm\"})", false)]
        [InlineData("Patch(Table, First(Filter(Table, MyStr = \"Str3\")), {MyDate: DateTime(2022,11,14,19,22,6) })", true)]
        public async Task DatabaseSimulation_Test(string expr, bool checkSuccess)
        {
            var databaseTable = DatabaseTable.CreateTestTable(0);
            var symbols = new SymbolTable();

            var slot = symbols.AddVariable("Table", DatabaseTable.TestTableType);
            symbols.EnableMutationFunctions();

            var engine = new RecalcEngine();
            var runtimeConfig = new SymbolValues(symbols);
            runtimeConfig.Set(slot, databaseTable);
            
            CheckResult check = engine.Check(expr, symbolTable: symbols, options: new ParserOptions() { AllowsSideEffects = true });
            Assert.Equal(checkSuccess, check.IsSuccess);

            if (!check.IsSuccess)
            {
                return;
            }

            IExpressionEvaluator run = check.GetEvaluator();

            FormulaValue result = await run.EvalAsync(CancellationToken.None, runtimeConfig);
            Assert.IsType<InMemoryRecordValue>(result);
        }

        // Verify that we catch timeouts. 
        [Theory]
        [InlineData("Patch(Table, First(Filter(Table, MyStr = \"Str3\")), {MyDate: DateTime(2022,11,14,19,22,6) })")]
        public async Task DatabaseSimulation_TestTimeout(string expr)
        {
            // Set PatchDelay to "infinite" (20 seconds). We'll timeout after 500ms. 
            // This should abort from the middle of the Patch. 
            var databaseTable = DatabaseTable.CreateTestTable(patchDelay: 20000);
            var symbols = new SymbolTable();

            var slot = symbols.AddVariable("Table", DatabaseTable.TestTableType);
            symbols.EnableMutationFunctions();

            var engine = new RecalcEngine();
            var runtimeConfig = new SymbolValues(symbols);
            runtimeConfig.Set(slot, databaseTable);

            CheckResult check = engine.Check(expr, symbolTable: symbols, options: new ParserOptions() { AllowsSideEffects = true });
            Assert.True(check.IsSuccess);

            IExpressionEvaluator run = check.GetEvaluator();
                        
            using (var cts = new CancellationTokenSource(500))
            {
                // Won't complete - should throw cancellation task 
                await Assert.ThrowsAsync<TaskCanceledException>(async () => await run.EvalAsync(cts.Token, runtimeConfig));
            }
        }

        internal class DatabaseTable : InMemoryTableValue
        {
            internal static TableType TestTableType => DatabaseRecord.TestRecordType.ToTable();

            internal readonly int PatchDelay;

            internal static DatabaseTable CreateTestTable(int patchDelay) =>
                new (
                    IRContext.NotInSource(TestTableType),
                    new List<DValue<RecordValue>>()
                    {
                        DValue<RecordValue>.Of(DatabaseRecord.CreateTestRecord("Str1", new DateTime(2022, 1, 1, 17, 33, 17), 3.14159265358979)),
                        DValue<RecordValue>.Of(DatabaseRecord.CreateTestRecord("Str2", new DateTime(2001, 7, 11, 8, 17, 52), 2.71828182845904)),
                        DValue<RecordValue>.Of(DatabaseRecord.CreateTestRecord("Str3", new DateTime(2019, 6, 28, 0, 45, 15), 1.41421356237309)),
                        DValue<RecordValue>.Of(DatabaseRecord.CreateTestRecord("Str4", new DateTime(2010, 4, 24, 16, 15, 0), 1.61803398874989)),
                        DValue<RecordValue>.Of(DatabaseRecord.CreateTestRecord("Str5", new DateTime(1954, 12, 4, 21, 5, 10), 2.15443469003188))
                    },
                    patchDelay);

            internal DatabaseTable(IRContext irContext, IEnumerable<DValue<RecordValue>> records, int patchDelay)
                : base(irContext, records)
            {
                PatchDelay = patchDelay;
            }

            protected override async Task<DValue<RecordValue>> PatchCoreAsync(RecordValue baseRecord, RecordValue changeRecord, CancellationToken cancellationToken)
            {
                if (PatchDelay > 0)
                {
                    await Task.Delay(PatchDelay, cancellationToken);
                }

                return await base.PatchCoreAsync(baseRecord, changeRecord, cancellationToken);
            }
        }

        internal class DatabaseRecord : InMemoryRecordValue
        {
            internal static FormulaType TestEntityType => new TestEntityType();

            internal static RecordType TestRecordType => RecordType.Empty()
                .Add("logicStr", FormulaType.String, "MyStr")
                .Add("logicDate", FormulaType.DateTime, "MyDate")
                .Add("logicNum", FormulaType.Number, "MyNum")
                .Add("logicEntity", TestEntityType, "MyEntity");

            internal static DatabaseRecord CreateTestRecord(string myStr, DateTime myDate, double myNum) =>
                new (
                    IRContext.NotInSource(TestRecordType),
                    new List<NamedValue>()
                    {
                        new NamedValue("logicStr", New(myStr)),
                        new NamedValue("logicDate", New(myDate)),
                        new NamedValue("logicNum", New(myNum)),
                        new NamedValue("logicEntity", new InMemoryRecordValue(
                            IRContext.NotInSource(TestDelegationMetadata.EntityRecordType),
                            new List<NamedValue>()
                            {
                                new NamedValue("logicStr2", New(myStr + "E")),
                                new NamedValue("logicDate2", New(myDate.AddYears(1)))
                            }))
                    });

            internal DatabaseRecord(IRContext irContext, IEnumerable<NamedValue> fields)
                : base(irContext, fields)
            {
            }

            internal DatabaseRecord(IRContext irContext, IReadOnlyDictionary<string, FormulaValue> fields)
                : base(irContext, fields)
            {
            }

            protected override Task<(bool Result, FormulaValue Value)> TryGetFieldAsync(FormulaType fieldType, string fieldName, CancellationToken cancellationToken)
            {
                var st = Environment.StackTrace;

                if (st.Contains("Microsoft.PowerFx.SymbolContext.GetScopeVar") ||
                    st.Contains("Microsoft.PowerFx.Types.CollectionTableValue`1.Matches"))
                {
                    return base.TryGetFieldAsync(fieldType, fieldName, cancellationToken);
                }

                throw new NotImplementedException("Cannot call TryGetField");
            }
        }

        internal class TestEntityType : FormulaType
        {
            internal TestEntityType()
                : base(DType.CreateExpandType(new TestExpandInfo()))
            {
            }

            public override void Visit(ITypeVisitor vistor)
            {
                throw new NotImplementedException("TestEntityType.Visit");
            }
        }

        internal class TestEntityValue : ValidFormulaValue
        {
            public TestEntityValue(IRContext irContext)
                : base(irContext)
            {
            }

            public override object ToObject()
            {
                throw new NotImplementedException("TestEntityValue.ToObject() isn't implemented");
            }

            public override void Visit(IValueVisitor visitor)
            {
                throw new NotImplementedException("TestEntityValue.Visit");
            }

            public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
            {
                // Internal only.
                throw new System.NotImplementedException("TestEntityValue cannot be serialized.");
            }
        }
    }
}
