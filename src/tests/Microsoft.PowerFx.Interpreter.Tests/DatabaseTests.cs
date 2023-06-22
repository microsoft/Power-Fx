// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DatabaseTests
    {
        [Fact]
        public void PatchFirst()
        {
            string expr = @"Patch(t, First(t), { Name: ""John""})";
            (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult) = CheckExpression(expr);
            (SymbolValues values, TestDatabaseTableValue database) = GetData(config);

            FormulaValue result = checkResult.GetEvaluator().Eval(values);
            Assert.False(result is ErrorValue);

            TestDatabaseRecordValue[] records = database.Records;
            Assert.Equal(2, records.Length);
            Assert.Equal("John", records[0].Name);
            Assert.True(records[0].TryGetPrimaryKey_Called);
            Assert.False(records[1].TryGetPrimaryKey_Called);
        }

        [Fact]
        public void PatchById()
        {
            // The key difference here with previous test is that the record we want to delete will have a RecordValue type
            // As a result, TryGetPrimaryKey will fail and all fields will be compared to each rows of the DB
            string expr = @"Patch(t, {Id: 2, Name: ""Mike"", Val: ""Val2""}, { Name: ""John""})";
            (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult) = CheckExpression(expr);
            (SymbolValues values, TestDatabaseTableValue database) = GetData(config);

            FormulaValue result = checkResult.GetEvaluator().Eval(values);
            Assert.False(result is ErrorValue);

            TestDatabaseRecordValue[] records = database.Records;
            Assert.Equal(2, records.Length);
            Assert.Equal("John", records[1].Name);
            Assert.False(records[0].TryGetPrimaryKey_Called);
            Assert.False(records[1].TryGetPrimaryKey_Called);
        }

        [Fact]
        public void RemoveFirst()
        {
            string expr = @"Remove(t, First(t))";
            (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult) = CheckExpression(expr);
            (SymbolValues values, TestDatabaseTableValue database) = GetData(config);

            FormulaValue result = checkResult.GetEvaluator().Eval(values);
            Assert.False(result is ErrorValue);

            TestDatabaseRecordValue[] records = database.Records;
            Assert.Single(records);
            Assert.Equal("Mike", records[0].Name);
        }

        private (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult) CheckExpression(string expr)
        {
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            RecalcEngine engine = new RecalcEngine(config);

            config.EnableSetFunction();
            config.SymbolTable.EnableMutationFunctions();

            RecordType recordType = TestDatabaseRecordValue.CustomRecordType;
            config.SymbolTable.AddVariable("t", recordType.ToTable(), new SymbolProperties() { CanSet = true, CanMutate = true });
            ReadOnlySymbolTable symbols = ReadOnlySymbolTable.NewFromRecord(recordType, allowThisRecord: true, allowMutable: true);

            CheckResult checkResult = engine.Check(expr, options: new ParserOptions(CultureInfo.InvariantCulture, true), symbolTable: symbols);
            Assert.True(checkResult.IsSuccess, $"CheckResult failed: {string.Join(", ", checkResult.Errors.Select(er => er.Message))}");

            return (config, engine, checkResult);
        }

        private (SymbolValues values, TestDatabaseTableValue database) GetData(PowerFxConfig config)
        {
            TestDatabaseRecordValue[] records = new TestDatabaseRecordValue[]
            {
                new TestDatabaseRecordValue(1, "Luc", "Val1"),
                new TestDatabaseRecordValue(2, "Mike", "Val2")
            };
            TestDatabaseTableValue database = new TestDatabaseTableValue(TestDatabaseRecordValue.CustomRecordType, new List<DValue<RecordValue>>(records.Select(r => DValue<RecordValue>.Of(r))));

            SymbolValues values = new SymbolValues(config.SymbolTable);
            values.SymbolTable.TryLookupSlot("t", out ISymbolSlot slot);
            values.Set(slot, database);

            return (values, database);
        }
    }

    internal class TestDatabaseRecordValue : RecordValue
    {
        public static readonly RecordType CustomRecordType = RecordType.Empty().Add("Id", FormulaType.Number).Add("Name", FormulaType.String).Add("Val", FormulaType.String);

        public int Id;
        public string Name;
        public string Val;

        public bool TryGetPrimaryKey_Called = false;

        public TestDatabaseRecordValue(RecordType type)
            : base(type)
        {
            throw new NotImplementedException();
        }

        public TestDatabaseRecordValue(IRContext irContext)
            : base(irContext)
        {
            throw new NotImplementedException();
        }

        public TestDatabaseRecordValue(int id, string name, string val)
            : base(CustomRecordType)
        {
            Id = id;
            Name = name;
            Val = val;
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            if (fieldName.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                result = FormulaValue.New(Id);
                return true;
            }
            else if (fieldName.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                result = FormulaValue.New(Name);
                return true;
            }
            else if (fieldName.Equals("val", StringComparison.OrdinalIgnoreCase))
            {
                result = FormulaValue.New(Val);
                return true;
            }

            result = null;
            return false;
        }

        public override bool TryGetPrimaryKey(out string key)
        {
            TryGetPrimaryKey_Called = true;
            key = Id.ToString();
            return true;
        }

        public override Task<DValue<RecordValue>> UpdateFieldsAsync(RecordValue changeRecord, CancellationToken cancellationToken)
        {
            foreach (NamedValue field in changeRecord.Fields)
            {
                if (field.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Cannot change the Id");
                }
                else if (field.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
                {
                    Name = field.Value.ToObject().ToString();
                }
                else if (field.Name.Equals("val", StringComparison.OrdinalIgnoreCase))
                {
                    Val = field.Value.ToObject().ToString();
                }
            }

            return Task.FromResult(DValue<RecordValue>.Of(this));
        }
    }

    internal class TestDatabaseTableValue : CollectionTableValue<DValue<RecordValue>>
    {
        public TestDatabaseTableValue(CollectionTableValue<DValue<RecordValue>> orig)
            : base(orig)
        {
            throw new NotImplementedException();
        }

        public TestDatabaseTableValue(RecordType recordType, IEnumerable<DValue<RecordValue>> source)
            : base(recordType, source)
        {
        }

        public TestDatabaseTableValue(IRContext irContext, IEnumerable<DValue<RecordValue>> source)
            : base(irContext, source)
        {
            throw new NotImplementedException();
        }

        protected override DValue<RecordValue> Marshal(DValue<RecordValue> item)
        {
            return item;
        }

        internal TestDatabaseRecordValue[] Records => (typeof(CollectionTableValue<DValue<RecordValue>>).GetField("_sourceList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this) as IEnumerable<DValue<RecordValue>>).Select(drv => drv.Value as TestDatabaseRecordValue).ToArray();
    }
}
