// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // Wrap a .net object as an UntypedObject.
    // This will lazily marshal through the object as it's accessed.
    [DebuggerDisplay("{_source}")]
    [DebuggerDisplay("{_source}")]
    public class PrimitiveWrapperAsUnknownObject : UntypedObjectBase
    {
        public readonly object _source;

        public PrimitiveWrapperAsUnknownObject(object source)
        {
            _source = source;
        }

        public static UntypedObjectValue New(object source)
        {
            return FormulaValue.New(new PrimitiveWrapperAsUnknownObject(source));
        }

        public override FormulaType Type
        {
            get
            {
                if (_source is int || _source is double || _source is uint)
                {
                    return FormulaType.Number;
                }

                if (_source is bool)
                {
                    return FormulaType.Boolean;
                }

                if (_source is string)
                {
                    return FormulaType.String;
                }

                if (_source.GetType().IsArray)
                {
                    return ExternalType.ArrayType;
                }

                if (_source is decimal || _source is long || _source is ulong)
                {
                    return FormulaType.Decimal;
                }

                return ExternalType.ObjectType;
            }
        }

        public override IUntypedObject this[int index]
        {
            get
            {
                var a = (Array)_source;

                // Fx infastructure already did this check,
                // so we're only invoked in success case. 
                Assert.True(index >= 0 && index <= a.Length);

                var value = a.GetValue(index);
                if (value == null)
                {
                    return null;
                }

                return new PrimitiveWrapperAsUnknownObject(value);
            }
        }

        public override int GetArrayLength()
        {
            var a = (Array)_source;
            return a.Length;
        }

        public override bool GetBoolean()
        {
            Assert.True(Type == FormulaType.Boolean);

            if (_source is bool b)
            {
                return b;
            }

            throw new InvalidOperationException($"Not a boolean type");
        }

        public override double GetDouble()
        {
            // Fx will only call this helper for numbers. 
            Assert.True(Type == FormulaType.Number);

            if (_source is double valDouble)
            {
                return valDouble;
            }

            if (_source is int valInt)
            {
                return valInt;
            }

            if (_source is uint valUInt)
            {
                return valUInt;
            }

            throw new InvalidOperationException($"Not a number type");
        }

        public override decimal GetDecimal()
        {
            // Fx will only call this helper for decimals. 
            Assert.True(Type == FormulaType.Decimal);

            if (_source is decimal valDecimal)
            {
                return valDecimal;
            }

            if (_source is long valLong)
            {
                return valLong;
            }

            if (_source is ulong valULong)
            {
                return valULong;
            }

            throw new InvalidOperationException($"Not a decimal type");
        }

        public override string GetUntypedNumber()
        {
            throw new NotImplementedException();
        }

        public override string GetString()
        {
            Assert.True(Type == FormulaType.String);

            if (_source is string valString)
            {
                return valString;
            }

            throw new InvalidOperationException($"Not a string type");
        }

        public override bool TryGetProperty(string value, out IUntypedObject result)
        {
            Assert.True(Type == ExternalType.ObjectType);

            var t = _source.GetType();
            var prop = t.GetProperty(value, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
            {
                // Fx semantics are to return blank for missing properties. 
                // No way to signal error here. 
                result = null;
                return false;
            }

            var obj = prop.GetValue(_source);

            if (obj == null)
            {
                result = null;
                return false;
            }
            else
            {
                result = new PrimitiveWrapperAsUnknownObject(obj);
            }

            return true;
        }

        public override bool TryGetPropertyNames(out IEnumerable<string> result)
        {
            result = null;
            return false;
        }
    }
}
