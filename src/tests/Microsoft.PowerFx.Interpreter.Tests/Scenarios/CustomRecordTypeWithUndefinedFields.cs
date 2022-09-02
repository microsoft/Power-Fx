// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    // Demonstrate the ability to override the behavior when accessing undefined fields.
    public class CustomRecordTypeWithUndefinedFields : PowerFxTest
    {
        private readonly RecordType _originalRecordType;
        private readonly RecordType _customRecordType;
        private readonly TestObj _testObj;

        public CustomRecordTypeWithUndefinedFields()
        {
            _originalRecordType = RecordType
                .Empty()
                .Add(new NamedFormulaType("prop1", FormulaType.Number))
                .Add(new NamedFormulaType("prop2", FormulaType.Number))
                .Add(new NamedFormulaType("prop3", FormulaType.Number));

            _customRecordType = new TreatMissingFieldAsUntypedRecordType(_originalRecordType);
            _testObj = new TestObj
            {
                Properties = new Dictionary<string, object>
                {
                    { "prop1",  1 },
                    { "prop2",  "hello" },
                    { "prop_not_defined_in_type",  3 },
                }
            };
        }

        [Theory]
        [InlineData("obj.prop1", 1.0, false)]
        [InlineData("IsError(obj.prop2)", true, false)] // type mismatch should fail at runtime
        [InlineData("IsBlank(obj.missing)", null, true)] // type checking fails when referencing undefined type.
        [InlineData("IsBlank(obj.missing.missing)", null, true)]
        [InlineData("IsBlank(obj.prop_not_defined_in_type)", null, true)]
        [InlineData("IsError(obj.missing + 1)", null, true)]
        public async Task TestOriginalRecordType(string expr, object expected, bool hasException)
        {
            var engine = new RecalcEngine();
            var symbolTable = new SymbolTable();
            var symbolValues = new SymbolValues();

            symbolTable.AddVariable("obj", _originalRecordType);
            symbolValues.Add("obj", new TestCustomRecordValue(_testObj, _originalRecordType));

            try
            {
                var result = await engine.EvalAsync(expr, default, null, symbolTable, symbolValues).ConfigureAwait(false);
                Assert.False(hasException);
                Assert.Equal(expected, result.ToObject());
            }
            catch (InvalidOperationException)
            {
                Assert.True(hasException);
            }
        }

        [Theory]
        [InlineData("obj.prop1", 1.0, false)]
        [InlineData("IsError(obj.prop2)", true, false)] // type mismatch should fail at runtime.
        [InlineData("IsBlank(obj.missing)", true, false)] // fields not defined in RecordType is treated as Blank.
        [InlineData("IsBlank(obj.missing.missing)", true, false)]
        [InlineData("IsBlank(obj.prop_not_defined_in_type)", true, false)] // fields not defined in RecordType is always Blank, regardless of whether it is defined in actual data.
        [InlineData("IsError(obj.missing + 1)", null, true)] // type checking fails when operating on undefined field.
        public async Task TestCustomRecordType(string expr, object expected, bool hasException)
        {
            var engine = new RecalcEngine();
            var symbolTable = new SymbolTable();
            var symbolValues = new SymbolValues();

            symbolTable.AddVariable("obj", _customRecordType);
            symbolValues.Add("obj", new TestCustomRecordValue(_testObj, _customRecordType));

            try
            {
                var result = await engine.EvalAsync(expr, default, null, symbolTable, symbolValues).ConfigureAwait(false);
                Assert.False(hasException);
                Assert.Equal(expected, result.ToObject());
            }
            catch (InvalidOperationException)
            {
                Assert.True(hasException);
            }
        }

        private class TestObj
        {
            public Dictionary<string, object> Properties { get; set; }
        }

        private class TestCustomRecordValue : RecordValue
        {
            private readonly TestObj _testObj;

            public TestCustomRecordValue(TestObj testObj, RecordType recordType)
                : base(recordType)
            {
                _testObj = testObj;
            }

            protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
            {
                result = FormulaValue.NewBlank(fieldType);
                if (fieldType == FormulaType.UntypedObject)
                {
                    return true;
                }

                if (_testObj.Properties.TryGetValue(fieldName, out var value))
                {
                    if (value is string str)
                    {
                        result = FormulaValue.New(str);
                    }
                    else if (value is int i)
                    {
                        result = FormulaValue.New(i);
                    }

                    if (!fieldType.Equals(result.Type))
                    {
                        result = FormulaValue.NewError(new ExpressionError());
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// A wrapper Record Type that forces accesses to missing fields to return FormulaType.UntypedObject.
        /// </summary>
        private class TreatMissingFieldAsUntypedRecordType : RecordType
        {
            public RecordType RealRecordType { get; set; }

            public TreatMissingFieldAsUntypedRecordType(RecordType realRecordType)
            {
                RealRecordType = realRecordType;
            }

            public override IEnumerable<string> FieldNames => RealRecordType.FieldNames;

            public override RecordType Add(NamedFormulaType field)
            {
                var real = RealRecordType.Add(field);
                return new TreatMissingFieldAsUntypedRecordType(real);
            }

            public override bool Equals(object other)
            {
                if (other is not TreatMissingFieldAsUntypedRecordType otherRecordType)
                {
                    return false;
                }

                return RealRecordType.Equals(otherRecordType);
            }

            public override int GetHashCode()
            {
                return RealRecordType.GetHashCode();
            }

            public override string ToString()
            {
                return RealRecordType.ToString();
            }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                if (RealRecordType.TryGetFieldType(name, out type))
                {
                    return true;
                }

                type = FormulaType.UntypedObject;
                return true;
            }

            public override void Visit(ITypeVisitor vistor)
            {
                RealRecordType.Visit(vistor);
            }
        }
    }
}
