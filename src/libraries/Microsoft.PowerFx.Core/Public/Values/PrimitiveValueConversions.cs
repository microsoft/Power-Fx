// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Mapping between builtin dotnet Types and power fx types.
    /// This works for primitives derived from <see cref="PrimitiveValue{T}"/>. 
    /// To marshal complex types, use explicit construction methods or a TypeMarshaller.
    /// </summary>
    public static class PrimitiveValueConversions
    {
        // Map from .net types to formulaTypes
        // Inverse map of PrimitiveValue<T> to T. 
        private static readonly IReadOnlyDictionary<Type, FormulaType> _map = new Dictionary<Type, FormulaType>()
        {
            // Fx needs more number types:
            { typeof(double), FormulaType.Number },
            { typeof(int), FormulaType.Decimal },
            { typeof(decimal), FormulaType.Decimal },
            { typeof(long), FormulaType.Decimal },
            { typeof(float), FormulaType.Number },
            { typeof(double?), FormulaType.Number },
            { typeof(int?), FormulaType.Decimal },
            { typeof(decimal?), FormulaType.Decimal },   
            { typeof(long?), FormulaType.Decimal },
            { typeof(float?), FormulaType.Number },
                        
            // Non-numeric types:
            { typeof(Guid), FormulaType.Guid },
            { typeof(bool), FormulaType.Boolean },
            { typeof(DateTime), FormulaType.DateTime },
            { typeof(DateTimeOffset), FormulaType.DateTime },
            { typeof(TimeSpan), FormulaType.Time },
            { typeof(string), FormulaType.String },
            { typeof(Guid?), FormulaType.Guid },
            { typeof(bool?), FormulaType.Boolean },
            { typeof(DateTime?), FormulaType.DateTime },
            { typeof(DateTimeOffset?), FormulaType.DateTime },
            { typeof(TimeSpan?), FormulaType.Time },
            { typeof(Color), FormulaType.Color }
        };

        /// <summary>
        /// Get the dotnet to powerfx type mapping. 
        /// </summary>
        /// <param name="type">dotnet type.</param>
        /// <param name="fxType">Power Fx type that corresponds to the dotnet type.</param>
        /// <returns>true if the dotnet type is a builtin primitive mapping to fx, else false.</returns>
        public static bool TryGetFormulaType(Type type, out FormulaType fxType)
        {
            return _map.TryGetValue(type, out fxType);
        }

        /// <summary>
        /// Marshal a primitive type. 
        /// For complex types, use a TypeMarshallerCache or other explicit methods. 
        /// </summary>
        /// <param name="value">value to marhal.</param>
        /// <param name="type">type of the value object, needed in case value is null.</param>
        /// <returns></returns>
        public static FormulaValue Marshal(object value, Type type)
        {
            if (value != null && value.GetType() != type)
            {
                throw new ArgumentException($"value.type (and type don't match.");
            }

            if (TryGetFormulaType(type, out var fxType))
            {
                return Marshal(value, fxType);
            }

            // For complex types, must use a TypeMarshaller cache. 
            throw new InvalidOperationException($"Unsupported primitive type {value.GetType().FullName}.");
        }

        public static FormulaValue Marshal(object value, FormulaType fxType)
        {
            if (TryMarshal(value, fxType, out var result))
            {
                return result;
            }

            throw new InvalidOperationException($"Unsupported type {value.GetType().FullName} as {fxType.GetType().Name}");
        }

        private static bool TryConvertToDecimal(object value, out decimal result)
        {
            switch (value)
            {
                case ushort x: // UInt16
                    result = x;
                    return true;
                case short x: // Int16
                    result = x;
                    return true;
                case uint x: // UInt32
                    result = x;
                    return true;
                case int x: // Int32
                    result = x;
                    return true;
                case ulong x: // UInt64
                    result = x;
                    return true;
                case long x: // Int64
                    result = x;
                    return true;
                case double x:
                    result = (decimal)x;
                    return true;
                case float x:
                    result = (decimal)x;
                    return true;
                case decimal x:
                    result = x;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }

        private static bool TryConvertToDouble(object value, out double result)
        {
            switch (value)
            {
                case ushort x: // UInt16
                    result = x;
                    return true;
                case short x: // Int16
                    result = x;
                    return true;
                case uint x: // UInt32
                    result = x;
                    return true;
                case int x: // Int32
                    result = x;
                    return true;
                case ulong x: // UInt64
                    result = x;
                    return true;
                case long x: // Int64
                    result = x;
                    return true;
                case double x:
                    result = x;
                    return true;
                case float x:
                    result = x;
                    return true;
                case decimal x:
                    result = (double)x;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }

        internal static ErrorValue OverflowError(IRContext irContext)
        {
            return new ErrorValue(irContext, new ExpressionError()
            {
                Message = "Overflow",
                Span = irContext.SourceContext,
                Kind = ErrorKind.Numeric
            });
        }

        /// <summary>
        /// Marshal from a dotnet primitive to a given Power Fx type. 
        /// Call <see cref="FormulaValue.ToObject"/> to go the other direction and get a dotnet object from a formulavalue. 
        /// </summary>
        /// <param name="value">dotnet value.</param>
        /// <param name="type">target power fx type to marshal to.</param>
        /// <param name="result"></param>
        /// <returns>True on success with result set to a non-null value.</returns>
        public static bool TryMarshal(object value, FormulaType type, out FormulaValue result)
        {
            if (value == null)
            {
                result = FormulaValue.NewBlank(type);
                return true;
            }

            result = null;

            try
            {
                if (type == FormulaType.Number)
                {
                    if (TryConvertToDouble(value, out var num))
                    {
                        result = FormulaValue.New(num);
                    }
                }
                else if (type == FormulaType.Decimal)
                {
                    if (TryConvertToDecimal(value, out var num))
                    {
                        result = FormulaValue.New(num);
                    }
                }
            }
            catch (OverflowException)
            {
                result = OverflowError(IRContext.NotInSource(type));
                return true;
            }

            if (type == FormulaType.String)
            {
                result = FormulaValue.New((string)value);
            }
            else if (type == FormulaType.Boolean)
            {
                result = FormulaValue.New((bool)value);
            }
            else if (type == FormulaType.Date)
            {
                // DateTime is broader (includes time). If they explicitly requested a Date, use that.
                if (value is DateTime dateValue)
                {
                    result = FormulaValue.NewDateOnly(dateValue);
                }
                else if (value is DateTimeOffset dateOffsetValue)
                {
                    result = FormulaValue.NewDateOnly(dateOffsetValue.DateTime);
                }
            }
            else if (type == FormulaType.DateTime)
            {
                if (value is DateTime dateValue)
                {
                    result = FormulaValue.New(dateValue);
                }
                else if (value is DateTimeOffset dateOffsetValue)
                {
                    result = FormulaValue.New(dateOffsetValue.DateTime);
                }
            }
            else if (type == FormulaType.Time)
            {
                result = FormulaValue.New((TimeSpan)value);
            }
            else if (type == FormulaType.Guid)
            {
                result = FormulaValue.New((Guid)value);
            }
            else if (type == FormulaType.Color)
            {
                result = FormulaValue.New((Color)value);
            }

            return result != null;
        }
    }
}
