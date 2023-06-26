// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class DatabaseTests
    {
        [Fact]
        public void PatchFirst()
        {
            string expr = @"Patch(t, First(t), { Name: ""John""})";
            (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult, ISymbolSlot dbSlot) = CheckExpression(expr);
            (SymbolValues values, TestDatabaseTableValue database) = GetData(config, dbSlot);

            FormulaValue result = checkResult.GetEvaluator().Eval(values);
            Assert.False(result is ErrorValue);

            ConcurrentDictionary<int, TestDatabaseRecordValue> records = database.DbContent;
            Assert.Equal(17, records.Count);
            Assert.Equal("John", records[0].Name);
            Assert.Equal(1, records[0].TryGetPrimaryKey_Called);
            Assert.Equal(0, records[1].TryGetPrimaryKey_Called);
        }

        [Fact]
        public void PatchById()
        {
            // The key difference here with previous test is that the record we want to delete will have a RecordValue type
            // As a result, TryGetPrimaryKey will fail and all fields will be compared to each rows of the DB
            string expr = @"Patch(t, {Id: 2, Name: ""Mike"", Val: ""Val2""}, { Name: ""John""})";
            (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult, ISymbolSlot dbSlot) = CheckExpression(expr);
            (SymbolValues values, TestDatabaseTableValue database) = GetData(config, dbSlot);

            FormulaValue result = checkResult.GetEvaluator().Eval(values);
            Assert.False(result is ErrorValue);

            ConcurrentDictionary<int, TestDatabaseRecordValue> records = database.DbContent;
            Assert.Equal(17, records.Count);
            Assert.Equal("John", records[1].Name);
            Assert.Equal(0, records[0].TryGetPrimaryKey_Called);
            Assert.Equal(0, records[1].TryGetPrimaryKey_Called);
        }

        [Fact]
        public void RemoveFirst()
        {
            string expr = @"Remove(t, First(t))";
            (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult, ISymbolSlot dbSlot) = CheckExpression(expr);
            (SymbolValues values, TestDatabaseTableValue database) = GetData(config, dbSlot);

            FormulaValue result = checkResult.GetEvaluator().Eval(values);
            Assert.False(result is ErrorValue);

            ConcurrentDictionary<int, TestDatabaseRecordValue> records = database.DbContent;
            Assert.Single(records);
            Assert.Equal("Mike", records[0].Name);
        }

        private (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult, ISymbolSlot dbSlot) CheckExpression(string expr)
        {
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            RecalcEngine engine = new RecalcEngine(config);

            config.EnableSetFunction();
            config.SymbolTable.EnableMutationFunctions();

            RecordType recordType = TestDatabaseRecordValue.CustomRecordType;
            ISymbolSlot dbSlot = config.SymbolTable.AddVariable("t", recordType.ToTable(), new SymbolProperties() { CanSet = true, CanMutate = true });
            ReadOnlySymbolTable symbols = ReadOnlySymbolTable.NewFromRecord(recordType, allowThisRecord: true, allowMutable: true);

            CheckResult checkResult = engine.Check(expr, options: new ParserOptions(CultureInfo.InvariantCulture, true), symbolTable: symbols);
            Assert.True(checkResult.IsSuccess, $"CheckResult failed: {string.Join(", ", checkResult.Errors.Select(er => er.Message))}");

            return (config, engine, checkResult, dbSlot);
        }

        private (SymbolValues values, TestDatabaseTableValue database) GetData(PowerFxConfig config, ISymbolSlot dbSlot)
        {
            TestDatabaseRecordValue[] records = new TestDatabaseRecordValue[]
            {
                new TestDatabaseRecordValue(0, "Stanley", "Val0"),
                new TestDatabaseRecordValue(1, "Abby", "Val1"),
                new TestDatabaseRecordValue(2, "Barber", "Val2"),
                new TestDatabaseRecordValue(3, "Solomon", "Val3"),
                new TestDatabaseRecordValue(4, "Jones", "Val4"),
                new TestDatabaseRecordValue(5, "Sophia", "Val5"),
                new TestDatabaseRecordValue(6, "Horton", "Val6"),
                new TestDatabaseRecordValue(7, "Garrett", "Val7"),
                new TestDatabaseRecordValue(8, "Pace", "Val8"),
                new TestDatabaseRecordValue(9, "Giana", "Val9"),
                new TestDatabaseRecordValue(10, "Hudson", "Val10"),
                new TestDatabaseRecordValue(11, "Peter", "Val11"),
                new TestDatabaseRecordValue(12, "Aguirre", "Val12"),
                new TestDatabaseRecordValue(13, "Ariah", "Val13"),
                new TestDatabaseRecordValue(14, "Todd", "Val14"),
                new TestDatabaseRecordValue(15, "Baylor", "Val15"),
                new TestDatabaseRecordValue(16, "Shah", "Val16")
            };

            TestDatabaseTableValue database = new TestDatabaseTableValue(records);  // new List<DValue<RecordValue>>(records.Select(r => DValue<RecordValue>.Of(r))));

            SymbolValues values = new SymbolValues(config.SymbolTable);
            values.Set(dbSlot, database);

            return (values, database);
        }
    }

    internal class TestDatabaseRecordValue : RecordValue
    {
        public static readonly RecordType CustomRecordType = RecordType.Empty().Add("Id", FormulaType.Number).Add("Name", FormulaType.String).Add("Val", FormulaType.String);

        public int Id;
        public string Name;
        public string Val;

        public int TryGetPrimaryKey_Called = 0;
        public int Fields_Called = 0;
        
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

        public override IEnumerable<NamedValue> Fields
        {
            get
            {
                Fields_Called++;
                yield return new NamedValue("id", FormulaValue.New(Id));
                yield return new NamedValue("name", FormulaValue.New(Name));
                yield return new NamedValue("val", FormulaValue.New(Val));
            }
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
            TryGetPrimaryKey_Called++;
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

    internal class TestDatabaseTableValue : TableValue, IRefreshable
    {
        private readonly ConcurrentDictionary<int, TestDatabaseRecordValue> _rows = new ();

        public ConcurrentDictionary<int, TestDatabaseRecordValue> DbContent => _rows;

        public TestDatabaseTableValue(RecordType recordType)
            : base(recordType)
        {
            throw new NotImplementedException();
        }

        public TestDatabaseTableValue(TableType type)
            : base(type)
        {
            throw new NotImplementedException();
        }

        public TestDatabaseTableValue(IRContext irContext)
            : base(irContext)
        {
            throw new NotImplementedException();
        }

        public TestDatabaseTableValue(IEnumerable<TestDatabaseRecordValue> rows)
            : base(TestDatabaseRecordValue.CustomRecordType)
        {
            rows.AsParallel().ForAll((TestDatabaseRecordValue dtrv) => _rows.AddOrUpdate(dtrv.Id, dtrv, (i, dtrv2) => throw new ArgumentException($"Two elements with same ID {i}")));
        }

        public int Count_Called = 0;
        public int Rows_Called = 0;
        public int AppendAsync_Called = 0;
        public int CastRecord_Called = 0;
        public int ClearAsync_Called = 0;
        public int PatchCoreAsync_Called = 0;
        public int RemoveAsync_Called = 0;        
        public int Refresh_Called = 0;

        public override int Count()
        {
            Count_Called++;
            return _rows.Count();
        }

        public override IEnumerable<DValue<RecordValue>> Rows
        {
            get
            {
                Rows_Called++;
                return _rows.Values.Select(v => DValue<RecordValue>.Of(v));
            }
        }        

        public override Task<DValue<RecordValue>> AppendAsync(RecordValue record, CancellationToken cancellationToken)
        {
            AppendAsync_Called++;
            cancellationToken.ThrowIfCancellationRequested();

            if (record is not TestDatabaseRecordValue dtrv)
            {
                throw new ArgumentException($"Record isn't TestDatabaseRecordValue", nameof(record));
            }

            _rows.AddOrUpdate(dtrv.Id, dtrv, (i, dtrv2) => throw new ArgumentException($"Two elements with same ID {i}"));
            return Task.FromResult(DValue<RecordValue>.Of(dtrv));
        }

        public override DValue<RecordValue> CastRecord(RecordValue record, CancellationToken cancellationToken)
        {
            CastRecord_Called++;
            cancellationToken.ThrowIfCancellationRequested();

            if (record is not TestDatabaseRecordValue dtrv)
            {
                throw new ArgumentException($"Record isn't TestDatabaseRecordValue", nameof(record));
            }

            return DValue<RecordValue>.Of(dtrv);
        }

        public override Task<DValue<BooleanValue>> ClearAsync(CancellationToken cancellationToken)
        {
            ClearAsync_Called++;
            cancellationToken.ThrowIfCancellationRequested();

            _rows.Clear();
            return Task.FromResult(DValue<BooleanValue>.Of(FormulaValue.New(true)));
        }

        protected override async Task<DValue<RecordValue>> PatchCoreAsync(RecordValue baseRecord, RecordValue changeRecord, CancellationToken cancellationToken)
        {
            PatchCoreAsync_Called++;
            cancellationToken.ThrowIfCancellationRequested();

            if (!baseRecord.TryGetPrimaryKey(out string id))
            {
                throw new ArgumentException("Base record doesn't have a primary key", nameof(baseRecord));
            }

            if (!TryGetFromDB(id, out TestDatabaseRecordValue dbRecord))
            {
                throw new ArgumentException($"Record with ID {id} doesn't exist", nameof(baseRecord));
            }

            DValue<RecordValue> ret = await dbRecord.UpdateFieldsAsync(changeRecord, cancellationToken).ConfigureAwait(false);
            Refresh();

            return ret;
        }

        private bool TryGetFromDB(string id, out TestDatabaseRecordValue record)
        {
            if (int.TryParse(id, out int idInt))
            {
                return _rows.TryGetValue(idInt, out record);
            }

            record = null;
            return false;
        }

        public override Task<DValue<BooleanValue>> RemoveAsync(IEnumerable<FormulaValue> recordsToRemove, bool all, CancellationToken cancellationToken)
        {
            RemoveAsync_Called++;

            foreach (FormulaValue record in recordsToRemove)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (record is not TestDatabaseRecordValue dtrv)
                {
                    throw new ArgumentException($"Record isn't TestDatabaseRecordValue", nameof(record));
                }

                if (!dtrv.TryGetPrimaryKey(out string id))
                {
                    throw new ArgumentException("Record doesn't have a primary key");
                }

                if (int.TryParse(id, out int idInt))
                {
                    _ = _rows.TryRemove(idInt, out _);
                }
                else
                {
                    throw new ArgumentException("Cannot delete record with non-integer ID");
                }
            }

            Refresh();
            return Task.FromResult(DValue<BooleanValue>.Of(BooleanValue.New(true)));
        }

        public override void ToExpression(StringBuilder sb, FormulaValueSerializerSettings settings)
        {
            throw new NotImplementedException();
        }

        public override object ToObject()
        {
            throw new NotImplementedException();
        }

        protected override bool TryGetIndex(int index1, out DValue<RecordValue> record)
        {
            throw new NotImplementedException();
        }

        public void Refresh()
        {
            Refresh_Called++;
        }
    }

    //internal class TestDatabaseTableValue : CollectionTableValue<DValue<RecordValue>>
    //{
    //    public TestDatabaseTableValue(CollectionTableValue<DValue<RecordValue>> orig)
    //        : base(orig)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public TestDatabaseTableValue(RecordType recordType, IEnumerable<DValue<RecordValue>> source)
    //        : base(recordType, source)
    //    {
    //    }

    //    public TestDatabaseTableValue(IRContext irContext, IEnumerable<DValue<RecordValue>> source)
    //        : base(irContext, source)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    protected override DValue<RecordValue> Marshal(DValue<RecordValue> item)
    //    {
    //        return item;
    //    }

    //    internal TestDatabaseRecordValue[] Records => (typeof(CollectionTableValue<DValue<RecordValue>>).GetField("_enumerator", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this) as IEnumerable<DValue<RecordValue>>).Select(drv => drv.Value as TestDatabaseRecordValue).ToArray();
    //}
}
