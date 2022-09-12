// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    internal static class FormulaTypeToSchemaConverter
    {
        public static FormulaTypeSchema ToSchema(this FormulaType type, DefinedTypeSymbolTable definedTypeSymbols)
        {
            return ToSchema(type, definedTypeSymbols, maxDepth: 5);
        }

        private static SchemaTypeName RecordTypeName => new () { Type = "Record", IsTable = false };

        private static SchemaTypeName TableTypeName => new () { Type = "Record", IsTable = true };

        private static FormulaTypeSchema ToSchema(FormulaType type, DefinedTypeSymbolTable definedTypeSymbols, int maxDepth)
        {
            if (maxDepth < 0)
            {
                throw new NotSupportedException("Max depth exceeded when converting type to schema definition");
            }

            if (TryLookupTypeName(type, definedTypeSymbols, out var typeName))
            {
                return new FormulaTypeSchema()
                {
                    Type = new SchemaTypeName() { Type = typeName }
                };
            }

            // Possible that the record type is defined for a TableType variant
            if (type is TableType tableType && TryLookupTypeName(tableType.ToRecord(), definedTypeSymbols, out typeName))
            {                
                return new FormulaTypeSchema()
                {
                    Type = new SchemaTypeName() { Type = typeName, IsTable = true }
                };
            }

            if (type is not AggregateType aggregateType)
            {
                throw new NotImplementedException($"Conversion to schema definition not supported for type {type}");
            }

            var children = GetChildren(aggregateType, definedTypeSymbols, maxDepth - 1);

            if (aggregateType is RecordType)
            {
                return new FormulaTypeSchema()
                {
                    Type = RecordTypeName,
                    Fields = children
                };
            }                
                
            return new FormulaTypeSchema()
            {
                Type = TableTypeName,
                Fields = children
            };
        }

        private static bool TryLookupTypeName(FormulaType type, DefinedTypeSymbolTable definedTypeSymbols, out string typeName)
        {
            var lookupOrder = new List<TypeSymbolTable>() { definedTypeSymbols, BuiltinTypesSymbolTable.Instance };
            foreach (var table in lookupOrder)
            {
                if (table.TryGetTypeName(type, out typeName))
                {
                    return true;
                }
            }

            typeName = null;
            return false;
        }

        private static Dictionary<string, FormulaTypeSchema> GetChildren(AggregateType type, DefinedTypeSymbolTable definedTypeSymbols, int maxDepth)
        {
            var fields = new Dictionary<string, FormulaTypeSchema>(StringComparer.Ordinal);
            foreach (var child in type.GetFieldTypes())
            {
                fields.Add(child.Name, ToSchema(child.Type, definedTypeSymbols, maxDepth));
            }

            return fields;
        }
    }
}
