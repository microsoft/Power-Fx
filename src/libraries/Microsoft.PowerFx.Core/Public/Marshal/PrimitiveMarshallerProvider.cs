// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Core
{
    public class PrimitiveMarshallerProvider : ITypeMashallerProvider
    {
        // Map from .net types to formulaTypes
        private static readonly Dictionary<Type, FormulaType> _map = new Dictionary<Type, FormulaType>()
        {
            // Fx needs more number typeS:
            { typeof(double), FormulaType.Number },
            { typeof(int), FormulaType.Number },
            { typeof(decimal), FormulaType.Number },
            { typeof(long), FormulaType.Number },
            { typeof(float), FormulaType.Number },
                        
            // Non-numeric types:
            { typeof(Guid), FormulaType.Guid },
            { typeof(bool), FormulaType.Boolean },
            { typeof(DateTime), FormulaType.DateTime },
            { typeof(DateTimeOffset), FormulaType.DateTime },
            { typeof(TimeSpan), FormulaType.Time },
            { typeof(string), FormulaType.String }
        };
        
        /// <inheritdoc/>
        public bool TryGetMarshaller(Type type, TypeMarshallerCache cache, int maxDepth, out ITypeMarshaller marshaler)
        {
            if (_map.TryGetValue(type, out var fxType))
            {
                marshaler = new PrimitiveTypeMarshaler(fxType);
                return true;
            }

            // Not supported
            marshaler = null;
            return false;
        }        
    }

    [DebuggerDisplay("ObjMarshal({Type})")]
    public class PrimitiveTypeMarshaler : ITypeMarshaller
    {
        public FormulaType Type { get; }

        public PrimitiveTypeMarshaler(FormulaType fxType)
        {
            Type = fxType;
        }

        public FormulaValue Marshal(object value)        
        {
            return Marshal(value, Type);
        }

        /// <summary>
        /// Marshal from a dotnet primitive to a given Power Fx type. 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FormulaValue Marshal(object value, FormulaType type)
        {         
            if (type == FormulaType.Number)
            {
                if (value is int i)
                {
                    return FormulaValue.New(i);
                }
                else if (value is double d)
                {
                    return FormulaValue.New(d);
                }
                else if (value is decimal dec)
                {
                    return FormulaValue.New(dec);
                }
                else if (value is long l)
                {
                    return FormulaValue.New(l);
                }
                else if (value is float f)
                {
                    return FormulaValue.New(f);
                }
            }
            else if (type == FormulaType.String)
            {
                return FormulaValue.New((string)value);
            }
            else if (type == FormulaType.Boolean)
            {
                return FormulaValue.New((bool)value);
            }
            else if (type == FormulaType.Date)
            {
                // DateTime is broader (includes time). If they explicitly requested a Date, use that.
                if (value is DateTime dateValue)
                {
                    return FormulaValue.NewDateOnly(dateValue);
                }
                else if (value is DateTimeOffset dateOffsetValue)
                {
                    return FormulaValue.NewDateOnly(dateOffsetValue.DateTime);
                }
            }
            else if (type == FormulaType.DateTime)
            {
                if (value is DateTime dateValue)
                {
                    return FormulaValue.New(dateValue);
                }
                else if (value is DateTimeOffset dateOffsetValue)
                {
                    return FormulaValue.New(dateOffsetValue.DateTime);
                }
            }
            else if (type == FormulaType.Time)
            {
                return FormulaValue.New((TimeSpan)value);
            }
            else if (type == FormulaType.Guid)
            {
                return FormulaValue.New((Guid)value);
            }

            throw new InvalidOperationException($"Unsupported type {value.GetType().FullName} as {type}");
        }
    }
}
