// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // Wrap a .net object as an UntypedObject.
    // This will lazily marshal through the object as it's accessed.
    [DebuggerDisplay("{_source}")]
    public class PrimitiveWrapperAsUnknownObject : IUntypedObject
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

        public FormulaType Type
        {
            get
            {
                if (_source is int || _source is double)
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

                return ExternalType.ObjectType;
            }
        }

        public IUntypedObject this[int index]
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

        public int GetArrayLength()
        {
            var a = (Array)_source;
            return a.Length;
        }

        public bool GetBoolean()
        {
            Assert.True(Type == FormulaType.Boolean);

            if (_source is bool b)
            {
                return b;
            }

            throw new InvalidOperationException($"Not a boolean type");
        }

        public double GetDouble()
        {
            // Fx will only call this helper for numbers. 
            Assert.True(Type == FormulaType.Number);

            if (_source is int valInt)
            {
                return valInt;
            }

            if (_source is double valDouble)
            {
                return valDouble;
            }

            throw new InvalidOperationException($"Not a number type");
        }

        public string GetString()
        {
            Assert.True(Type == FormulaType.String);

            if (_source is string valString)
            {
                return valString;
            }

            throw new InvalidOperationException($"Not a string type");
        }

        public bool TryGetProperty(string value, out IUntypedObject result)
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
    }
}
