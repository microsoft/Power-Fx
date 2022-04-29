// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// Mapping between builtin dotnet Types and power fx types.
    /// </summary>
    public static class BuiltinFormulaTypeConversions
    {
        // Map from .net types to formulaTypes
        private static readonly IReadOnlyDictionary<Type, FormulaType> _map = new Dictionary<Type, FormulaType>()
        {
            // Fx needs more number types:
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
    }
}
