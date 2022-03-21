// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Marshaller for builtin primitives. 
    /// </summary>
    [DebuggerDisplay("ObjMarshal({Type})")]
    public class PrimitiveTypeMarshaler : ITypeMarshaller
    {
        /// <inheritdoc/>
        public FormulaType Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveTypeMarshaler"/> class.
        /// </summary>
        /// <param name="fxType">The power fx type this marshals to.</param>
        public PrimitiveTypeMarshaler(FormulaType fxType)
        {
            Type = fxType;
        }

        /// <inheritdoc/>
        public FormulaValue Marshal(object value)
        {
            return Marshal(value, Type);
        }

        /// <summary>
        /// Marshal from a dotnet primitive to a given Power Fx type. 
        /// </summary>
        /// <param name="value">dotnet value.</param>
        /// <param name="type">target power fx type to marshal to.</param>
        /// <returns>A power fx value.</returns>
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
