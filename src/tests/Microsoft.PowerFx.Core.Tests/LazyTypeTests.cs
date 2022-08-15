// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class LazyTypeTests
    {
        public class TestLazyRecordType : RecordType
        {
            public delegate bool TryGetFieldDelegate(string name, out FormulaType type);

            private readonly TryGetFieldDelegate _tryGetField;
            
            internal string Identity;

            public override IEnumerable<string> FieldNames { get; }

            public TestLazyRecordType(string identity, IEnumerable<string> fields, TryGetFieldDelegate getter)
                : base()
            {
                Identity = identity;
                FieldNames = fields; 
                _tryGetField = getter;
            }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                return _tryGetField(name, out type);
            }

            public override bool Equals(object other)
            {
                return other is TestLazyRecordType otherTable &&
                    Identity == otherTable.Identity;
            }

            public override int GetHashCode()
            {
                return Identity.GetHashCode();
            }
        }

        public class PadTableType : TableType
        {
            private RecordType _underlyingType;

            public override IEnumerable<string> FieldNames => _underlyingType.FieldNames;

            public PadTableType(RecordType underlyingType)
            {
                _underlyingType = underlyingType;
            }

            public PadTableType()
            {
                _underlyingType = RecordType.Empty();
            }

            public override TableType Add(NamedFormulaType field)
            {
                _underlyingType = _underlyingType.Add(field);
                return this;
            }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                return _underlyingType.TryGetFieldType(name, out type);
            }

            public override bool Equals(object other)
            {
                return false;
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Fact]
        public void PadTableWithUnderlyingRecordType()
        {
            var context = RecordType.Empty();
            context = context.Add("tableVar", new PadTableType(new TestLazyRecordType("Lazy1", new List<string>() { "Foo", "Bar", "Baz" }, LazyGetField1)));
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("Index(tableVar, 1)", context);
            Assert.IsType<KnownRecordType>(result.ReturnType);
            
            // Expected result
            Assert.IsType<TestLazyRecordType>(result.ReturnType);
        }

        public class TestLazyTableType : TableType
        {
            public delegate bool TryGetFieldDelegate(string name, out FormulaType type);

            private readonly TryGetFieldDelegate _tryGetField;
            
            internal string Identity;

            public override IEnumerable<string> FieldNames { get; }

            public TestLazyTableType(string identity, IEnumerable<string> fields, TryGetFieldDelegate getter)
                : base()
            {
                Identity = identity;
                FieldNames = fields; 
                _tryGetField = getter;
            }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                return _tryGetField(name, out type);
            }

            public override bool Equals(object other)
            {
                return other is TestLazyTableType otherTable &&
                    Identity == otherTable.Identity;
            }

            public override int GetHashCode()
            {
                return Identity.GetHashCode();
            }
        }

        private int _getter1CalledCount = 0;

        private bool LazyGetField1(string name, out FormulaType type)
        {
            _getter1CalledCount++;
            type = name switch
            {
                "Foo" => FormulaType.Number,
                "Bar" => FormulaType.String,
                "Baz" => FormulaType.Boolean,
                _ => FormulaType.Blank,
            };

            return type != FormulaType.Blank;
        }

        private int _getter2CalledCount = 0;

        private bool LazyGetField2(string name, out FormulaType type)
        {
            _getter2CalledCount++;
            type = name switch
            {
                "Qux" => FormulaType.Number,
                "Nested" => _lazyRecord1,
                _ => FormulaType.Blank,
            };

            return type != FormulaType.Blank;
        }
        
        private readonly TestLazyRecordType _lazyRecord1;
        private readonly TestLazyRecordType _lazyRecord2;
        private readonly TestLazyTableType _lazyTable1;
        private readonly TestLazyTableType _lazyTable2;

        public LazyTypeTests()
        {
            _lazyRecord1 = new TestLazyRecordType("Lazy1", new List<string>() { "Foo", "Bar", "Baz" }, LazyGetField1);
            _lazyRecord2 = new TestLazyRecordType("Lazy2", new List<string>() { "Qux", "Nested" }, LazyGetField2);
            _lazyTable1 = new TestLazyTableType("Lazy3", new List<string>() { "Foo", "Bar", "Baz" }, LazyGetField1);
            _lazyTable2 = new TestLazyTableType("Lazy4", new List<string>() { "Qux", "Nested" }, LazyGetField2);
        }

        [Fact]
        public void DTypeRepresentation()
        {            
            Assert.Equal(DKind.LazyRecord, _lazyRecord1._type.Kind);
            Assert.Equal(DKind.LazyTable, _lazyTable1._type.Kind);
            Assert.Equal("r!", _lazyRecord1._type.ToString());
            Assert.Equal("r*", _lazyTable1._type.ToString());
            Assert.Equal(_lazyRecord2, _lazyRecord2._type.LazyTypeProvider.BackingFormulaType);
            Assert.Equal(_lazyTable1, _lazyTable1._type.LazyTypeProvider.BackingFormulaType);
            
            Assert.Equal(0, _getter1CalledCount);
            Assert.Equal(0, _getter2CalledCount);
        }
        
        [Fact]
        public void AcceptsSimple()
        {
            // Error accepts all
            Assert.True(DType.Error.Accepts(_lazyRecord1._type));
            Assert.True(DType.Error.Accepts(_lazyTable1._type));

            // All accept Unknown
            Assert.True(_lazyRecord1._type.Accepts(DType.Unknown));
            Assert.True(_lazyTable1._type.Accepts(DType.Unknown));
            
            // No primitive accepts
            Assert.False(DType.Number.Accepts(_lazyRecord1._type));
            Assert.False(DType.Boolean.Accepts(_lazyRecord1._type));
            Assert.False(DType.String.Accepts(_lazyRecord1._type));
            Assert.False(DType.Color.Accepts(_lazyRecord1._type));
            Assert.False(DType.Date.Accepts(_lazyRecord1._type));
            Assert.False(DType.Time.Accepts(_lazyRecord1._type));
            Assert.False(DType.Guid.Accepts(_lazyRecord1._type));

            Assert.False(DType.Number.Accepts(_lazyTable1._type));
            Assert.False(DType.Boolean.Accepts(_lazyTable1._type));
            Assert.False(DType.String.Accepts(_lazyTable1._type));
            Assert.False(DType.Color.Accepts(_lazyTable1._type));
            Assert.False(DType.Date.Accepts(_lazyTable1._type));
            Assert.False(DType.Time.Accepts(_lazyTable1._type));
            Assert.False(DType.Guid.Accepts(_lazyTable1._type));
            
            // Hasn't callled field getter
            Assert.Equal(0, _getter1CalledCount);
            Assert.Equal(0, _getter2CalledCount);
        }
        
        [Fact]
        public void AcceptsAggregate()
        {
            Assert.True(DType.EmptyRecord.Accepts(_lazyRecord1._type));
            Assert.True(DType.EmptyTable.Accepts(_lazyTable1._type));

            Assert.False(DType.EmptyRecord.Accepts(_lazyTable1._type));
            Assert.False(DType.EmptyTable.Accepts(_lazyRecord1._type));

            // Hasn't callled field getter
            Assert.Equal(0, _getter1CalledCount);
            Assert.Equal(0, _getter2CalledCount);

            Assert.False(_lazyRecord2._type.Accepts(_lazyRecord1._type));
            Assert.False(_lazyTable2._type.Accepts(_lazyTable1._type));
                        
            Assert.False(_lazyRecord1._type.Accepts(_lazyRecord2._type));
            Assert.False(_lazyTable1._type.Accepts(_lazyTable2._type));

            Assert.True(_lazyRecord1._type.Accepts(_lazyRecord1._type));
            Assert.True(_lazyTable2._type.Accepts(_lazyTable2._type));

            // Hasn't callled field getter for lazy/lazy ops
            Assert.Equal(0, _getter1CalledCount);
            Assert.Equal(0, _getter2CalledCount);            

            Assert.False(TestUtils.DT("![not:s, in: b, lazy:n, record:c]").Accepts(_lazyRecord1._type));
            Assert.False(TestUtils.DT("*[not:s, in: b, lazy:n, record:c]").Accepts(_lazyTable2._type));

            // Calls field getter only once, stops on first mismatch
            Assert.Equal(1, _getter1CalledCount);
            Assert.Equal(1, _getter2CalledCount);
        }

        [Fact]
        public void AcceptsAggregateAllFields()
        {
            Assert.True(_lazyRecord1._type.Accepts(TestUtils.DT("![Foo:n, Bar: s, Baz:b]")));

            // Must call field getter for each field
            Assert.Equal(3, _getter1CalledCount);

            // Not all fields present
            Assert.False(_lazyRecord2._type.Accepts(TestUtils.DT("![Qux: n]")));

            // Must call field getter for each field
            Assert.Equal(2, _getter2CalledCount);

            _getter1CalledCount = 0;

            Assert.True(_lazyRecord1._type.Accepts(TestUtils.DT("![Foo:n, Bar: s, Baz:b]")));
            
            // Running Accepts again doesn't re-run the getter
            Assert.Equal(0, _getter1CalledCount);
        }

        [Fact]
        public void AcceptsLazyMatch()
        {
            Assert.True(_lazyRecord1._type.Accepts(_lazyRecord2.GetFieldType("Nested")._type));

            // Getter2 only called once, Getter1 never called
            Assert.Equal(0, _getter1CalledCount);
            Assert.Equal(1, _getter2CalledCount);
        }

        [Fact]
        public void AcceptsLazyMatch_negative()
        {
            Assert.False(_lazyRecord1._type.Accepts(_lazyRecord2._type));

            Assert.Equal(0, _getter1CalledCount);
            Assert.Equal(0, _getter2CalledCount);
        }

        [Fact]
        public void DropAllOfKind()
        {
            var fError = false;

            var type1 = _lazyTable2._type.DropAllOfKind(ref fError, DPath.Root, DKind.LazyRecord);

            // Must call field getter for each field
            Assert.Equal(2, _getter2CalledCount);

            Assert.Equal("*[Qux:n]", type1.ToString());
        }        

        [Fact]
        public void AddField()
        {
            var fError = false;

            var type1 = _lazyRecord1._type.Add(ref fError, DPath.Root, new DName("Test"), DType.UntypedObject);

            // Must call field getter for each field
            Assert.Equal(3, _getter1CalledCount);

            Assert.Equal("![Bar:s, Baz:b, Foo:n, Test:O]", type1.ToString());
        }
    }
}
