// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    internal static class FormulaTypeToSchemaHelper
    {
        public static FormulaTypeSchema ToSchema(this FormulaType type, DefinedTypeSymbolTable definedTypeSymbols, FormulaTypeSerializerSettings settings)
        {
            // Converting a formulaType to a FormulaTypeSchema requires cutting off at a max depth
            // FormulaType may contain recurisve definitions that are not supported by FormulaTypeSchema
            // As such, capping the depth ensures that we don't stack overflow when converting those types. 
            return ToSchema(type, definedTypeSymbols, settings, maxDepth: 5);
        }

        private static FormulaTypeSchema ToSchema(FormulaType type, DefinedTypeSymbolTable definedTypeSymbols, FormulaTypeSerializerSettings settings, int maxDepth)
        {
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
                // Unsupported type, return None
                return new FormulaTypeSchema()
                {
                    Type = new SchemaTypeName() { Name = "None" }
                };
            }

            var logicalName = aggregateType.TableSymbolName;
            if (type._type.AggregateHasExpandedType() && logicalName != null)
            {
                var schemaTypeName = type._type.IsTable ? SchemaTypeName.CustomTableTypeName : SchemaTypeName.CustomRecordTypeName;
                return new FormulaTypeSchema()
                {
                    Type = schemaTypeName,
                    CustomTypeName = logicalName,
                };
            }
            
            if (maxDepth < 0)
            {
                // Capped depth, return None for aggregate types
                return new FormulaTypeSchema()
                {
                    Type = new SchemaTypeName() { Name = "None" }
                };
            }

            var children = GetChildren(aggregateType, definedTypeSymbols, settings, maxDepth - 1);

            if (aggregateType is RecordType)
            {
                return new FormulaTypeSchema()
                {
                    Type = SchemaTypeName.RecordTypeName,
                    Fields = children
                };
            }                
                
            return new FormulaTypeSchema()
            {
                Type = SchemaTypeName.TableTypeName,
                Fields = children
            };
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

        private static Dictionary<string, FormulaTypeSchema> GetChildren(AggregateType type, DefinedTypeSymbolTable definedTypeSymbols, FormulaTypeSerializerSettings settings, int maxDepth)
        {
            var fields = new Dictionary<string, FormulaTypeSchema>(StringComparer.Ordinal);
            foreach (var child in type.GetFieldTypes())
            {
                fields.Add(child.Name, ToSchema(child.Type, definedTypeSymbols, settings, maxDepth));
            }

            return fields;
        }
    }
}
