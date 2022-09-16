// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    internal static class FormulaTypeToSchemaHelper
    {
        public static FormulaTypeSchema ToSchema(this FormulaType type, DefinedTypeSymbolTable definedTypeSymbols)
        {
            return ToSchema(type, definedTypeSymbols, maxDepth: 5);
        }
                
        public static FormulaType ToFormulaType(this FormulaTypeSchema schema, DefinedTypeSymbolTable definedTypeSymbols)
        {
            var typeName = schema.Type.Name;

            if (TryLookupType(typeName, definedTypeSymbols, out var actualType))
            {
                if (schema.Type.IsTable)
                {
                    return actualType switch
                    {
                        RecordType recordType => recordType.ToTable(),

                        // Add support for table of primitives here in the future
                        _ => FormulaType.BindingError,
                    };
                }
                
                return actualType;
            }

            if (typeName != RecordTypeName.Name)
            {
                return FormulaType.BindingError;
            }

            if (schema.Fields == null || !schema.Fields.Any())
            {
                return FormulaType.BindingError;
            }

            var result = new UserDefinedRecordType(schema, definedTypeSymbols);
            return schema.Type.IsTable ? result.ToTable() : result;
        }

        private static SchemaTypeName RecordTypeName => new () { Name = "Record", IsTable = false };

        private static SchemaTypeName TableTypeName => new () { Name = "Record", IsTable = true };

        private static FormulaTypeSchema ToSchema(FormulaType type, DefinedTypeSymbolTable definedTypeSymbols, int maxDepth)
        {
            if (maxDepth < 0)
            {
                throw new InvalidOperationException("Max depth exceeded when converting type to schema definition");
            }

            if (TryLookupTypeName(type, definedTypeSymbols, out var typeName))
            {
                return new FormulaTypeSchema()
                {
                    Type = new SchemaTypeName() { Name = typeName }
                };
            }

            // Possible that the record type is defined for a TableType variant
            if (type is TableType tableType && TryLookupTypeName(tableType.ToRecord(), definedTypeSymbols, out typeName))
            {                
                return new FormulaTypeSchema()
                {
                    Type = new SchemaTypeName() { Name = typeName, IsTable = true }
                };
            }

            if (type is not AggregateType aggregateType)
            {
                throw new InvalidOperationException($"Conversion to schema definition not supported for type {type}");
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

        private static bool TryLookupType(string typeName, DefinedTypeSymbolTable definedTypeSymbols, out FormulaType type)
        {
            var lookupOrder = new List<TypeSymbolTable>() { definedTypeSymbols, PrimitiveTypesSymbolTable.Instance };
            foreach (var table in lookupOrder)
            {
                if (table.TryLookup(new DName(typeName), out var lookupInfo))
                {
                    if (lookupInfo.Kind != BindKind.TypeName || lookupInfo.Data is not FormulaType castType)
                    {
                        throw new InvalidOperationException("Resolved non-type name when constructing FormulaType definition");
                    }

                    type = castType;
                    return true;
                }
            }

            type = null;
            return false;
        }

        private static bool TryLookupTypeName(FormulaType type, DefinedTypeSymbolTable definedTypeSymbols, out string typeName)
        {
            var lookupOrder = new List<TypeSymbolTable>() { definedTypeSymbols, PrimitiveTypesSymbolTable.Instance };
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
