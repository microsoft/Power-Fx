// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
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
            (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult, ISymbolSlot dbSlot) = CheckExpression(expr);
            (SymbolValues values, TestDatabaseTableValue database) = GetData(config, dbSlot);

            FormulaValue result = checkResult.GetEvaluator().Eval(values);
            Validate(result, database);

            ConcurrentDictionary<int, TestDatabaseRecordValue> records = database.DbContent;
            Assert.Equal(17, records.Count);
            Assert.Equal("John", records[0].Name);
        }

        [Fact]
        public void PatchFirstInMemory()
        {
            string expr = @"Set(x, FirstN(t, 5)); Patch(x, First(t), { Name: ""John""}); First(x).Name";
            (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult, ISymbolSlot dbSlot) = CheckExpression(expr);
            (SymbolValues values, TestDatabaseTableValue database) = GetData(config, dbSlot);

            FormulaValue result = checkResult.GetEvaluator().Eval(values);
            Validate(result, database);

            ConcurrentDictionary<int, TestDatabaseRecordValue> records = database.DbContent;
            Assert.Equal(17, records.Count);
            Assert.Equal("Stanley", records[0].Name); // DB is unchanged
            Assert.Equal("John", ((StringValue)result).Value); // InMemory table is updated as expected
        }

        [Fact]
        public void PatchById()
        {
            string expr = @"Patch(t, {Id: 2}, { Name: ""John""})";
            (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult, ISymbolSlot dbSlot) = CheckExpression(expr);
            (SymbolValues values, TestDatabaseTableValue database) = GetData(config, dbSlot);

            FormulaValue result = checkResult.GetEvaluator().Eval(values);
            Validate(result, database);

            ConcurrentDictionary<int, TestDatabaseRecordValue> records = database.DbContent;
            Assert.Equal(17, records.Count);
            Assert.Equal("John", records[2].Name);
        }

        [Fact]
        public void PatchByIdInMemory()
        {
            string expr = @"Set(x, FirstN(t, 5)); Patch(x, {Id: 2}, { Name: ""John""}); First(Filter(x, Id=2)).Name";
            (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult, ISymbolSlot dbSlot) = CheckExpression(expr);
            (SymbolValues values, TestDatabaseTableValue database) = GetData(config, dbSlot);

            FormulaValue result = checkResult.GetEvaluator().Eval(values);
            Validate(result, database);

            ConcurrentDictionary<int, TestDatabaseRecordValue> records = database.DbContent;
            Assert.Equal(17, records.Count);
            Assert.Equal("Barber", records[2].Name); // DB is unchanged
            Assert.Equal("John", ((StringValue)result).Value); // InMemory table is updated as expected
        }

        [Fact]
        public void RemoveFirst()
        {
            string expr = @"Remove(t, First(t))";
            (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult, ISymbolSlot dbSlot) = CheckExpression(expr);
            (SymbolValues values, TestDatabaseTableValue database) = GetData(config, dbSlot);

            FormulaValue result = checkResult.GetEvaluator().Eval(values);
            Validate(result, database);

            ConcurrentDictionary<int, TestDatabaseRecordValue> records = database.DbContent;
            Assert.Equal(16, records.Count);
        }

        [Fact]
        public void RemoveFirstInMemory()
        {
            string expr = @"Set(x, FirstN(t, 5)); Remove(x, First(t)); CountRows(x)";
            (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult, ISymbolSlot dbSlot) = CheckExpression(expr);
            (SymbolValues values, TestDatabaseTableValue database) = GetData(config, dbSlot);

            FormulaValue result = checkResult.GetEvaluator().Eval(values);
            Validate(result, database);

            ConcurrentDictionary<int, TestDatabaseRecordValue> records = database.DbContent;
            Assert.Equal(17, records.Count);
            Assert.Equal(4, ((DecimalValue)result).Value); // InMemory table is updated as expected
        }

        private void Validate(FormulaValue result, TestDatabaseTableValue database)
        {
            Assert.False(result is ErrorValue, (result as ErrorValue).GetMessage());            
        }

        private (PowerFxConfig config, RecalcEngine engine, CheckResult checkResult, ISymbolSlot dbSlot) CheckExpression(string expr)
        {
            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            RecalcEngine engine = new RecalcEngine(config);

            config.EnableSetFunction();
            config.SymbolTable.EnableMutationFunctions();
            engine.UpdateVariable("x", FormulaValue.NewTable(TestDatabaseRecordValue.CustomRecordType));

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

            TestDatabaseTableValue database = new TestDatabaseTableValue(records);

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

        //public override IEnumerable<NamedValue> Fields
        //{
        //    get
        //    {
        //        Fields_Called++;
        //        yield return new NamedValue("Id", FormulaValue.New(Id));                
        //        yield return new NamedValue("Name", FormulaValue.New(Name));
        //        yield return new NamedValue("Val", FormulaValue.New(Val));
        //    }
        //}

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            if (fieldName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                result = FormulaValue.New(Id);
                return true;
            }
            else if (fieldName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                result = FormulaValue.New(Name);
                return true;
            }
            else if (fieldName.Equals("Val", StringComparison.OrdinalIgnoreCase))
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
                if (field.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Cannot change the Id");
                }
                else if (field.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    Name = field.Value.ToObject().ToString();
                }
                else if (field.Name.Equals("Val", StringComparison.OrdinalIgnoreCase))
                {
                    Val = field.Value.ToObject().ToString();
                }
            }

            return Task.FromResult(DValue<RecordValue>.Of(this));
        }

        public override string GetPrimaryKeyName()
        {
            return "Id";
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

            AddToDB(dtrv);
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

            ClearDB();
            return Task.FromResult(DValue<BooleanValue>.Of(FormulaValue.New(true)));
        }

        protected override async Task<DValue<RecordValue>> PatchCoreAsync(RecordValue baseRecord, RecordValue changeRecord, CancellationToken cancellationToken)
        {
            PatchCoreAsync_Called++;
            cancellationToken.ThrowIfCancellationRequested();

            // We know the primary key name is "Id" and can use it here
            FormulaValue idValue = await baseRecord.GetFieldAsync("Id", cancellationToken).ConfigureAwait(false);

            if (idValue is not NumberValue idNumValue)
            {
                throw new ArgumentException($"Record doesn't have a number Id", nameof(baseRecord));
            }

            if (!TryGetFromDB((int)idNumValue.Value, out TestDatabaseRecordValue dbRecord))
            {
                throw new ArgumentException($"Record with ID {idNumValue.Value} doesn't exist", nameof(baseRecord));
            }

            DValue<RecordValue> ret = await dbRecord.UpdateFieldsAsync(changeRecord, cancellationToken).ConfigureAwait(false);
            Refresh();

            return ret;
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
                    _ = TryRemoveFromDB(idInt);
                }
                else
                {
                    throw new ArgumentException("Cannot delete record with non-integer ID");
                }
            }

            Refresh();
            return Task.FromResult(DValue<BooleanValue>.Of(BooleanValue.New(true)));
        }

        private void AddToDB(TestDatabaseRecordValue tdrv)
        {
            _ = _rows.AddOrUpdate(tdrv.Id, tdrv, (i, tdrv2) => throw new ArgumentException($"Two elements with same ID {i}"));
        }

        private bool TryGetFromDB(int id, out TestDatabaseRecordValue record)
        {
            return _rows.TryGetValue(id, out record);
        }

        private void ClearDB()
        {
            _rows.Clear();
        }

        private bool TryRemoveFromDB(int id)
        {
            return _rows.TryRemove(id, out _);
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

    public static class ErrorValueExtensions
    {
        public static string GetMessage(this ErrorValue ev) => ev == null ? "No Error" : string.Join("\r\n", ev.Errors.Select((er, i) => $"ERROR #{i}: {er.Message}"));
    }
}
