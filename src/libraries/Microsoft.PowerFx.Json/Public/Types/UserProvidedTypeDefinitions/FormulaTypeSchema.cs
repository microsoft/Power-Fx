﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    internal sealed class FormulaTypeSchema
    {
        /// <summary>
        /// Represents the type of this item.
        /// </summary>
        public SchemaTypeName Type { get; set; }

        /// <summary>
        /// Name for the custom type.<see cref="SchemaTypeName.CustomRecordTypeName"/> or <see cref="SchemaTypeName.CustomTableTypeName"/>.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string CustomTypeName { get; set; }

        // Optional, description for the type
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; }

        // Optional, help link for the type
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Help { get; set; }

        /// <summary>
        /// Optional. For Records and Tables, contains the list of fields.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, FormulaTypeSchema> Fields { get; set; }

        public FormulaType ToFormulaType(INameResolver definedTypeSymbols, FormulaTypeSerializerSettings settings)
        {
            var typeName = Type.Name;

            if (TryLookupType(typeName, definedTypeSymbols, out var actualType))
            {
                if (Type.IsTable)
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

            var logicalNameToRecordType = settings.LogicalNameToRecordType;
            if ((typeName == SchemaTypeName.CustomTableTypeName.Name || typeName == SchemaTypeName.CustomRecordTypeName.Name) && logicalNameToRecordType != null)
            {
                if (Type.IsTable)
                {
                    return logicalNameToRecordType.Invoke(this.CustomTypeName).ToTable();
                }

                return logicalNameToRecordType.Invoke(this.CustomTypeName);
            }
            else if (typeName == SchemaTypeName.RecordTypeName.Name || typeName == SchemaTypeName.TableTypeName.Name)
            {
                if (Fields == null || !Fields.Any())
                {
                    FormulaType emptyAggregateType = Type.IsTable ? TableType.Empty() : RecordType.Empty();
                    return emptyAggregateType;
                }

                var result = new UserDefinedRecordType(this, definedTypeSymbols, settings);
                return Type.IsTable ? result.ToTable() : result;
            }

            return FormulaType.BindingError;
        }

        private static bool TryLookupType(string typeName, INameResolver definedTypeSymbols, out FormulaType type)
        {
            var lookupOrder = new List<INameResolver>() { definedTypeSymbols, ReadOnlySymbolTable.PrimitiveTypesTableInstance };
            foreach (var table in lookupOrder)
            {
                if (table.LookupType(new DName(typeName), out var ft))
                {
                    type = ft;
                    return true;
                }
            }

            type = null;
            return false;
        }

        public override bool Equals(object o)
        {
            return o is FormulaTypeSchema other && 
                Type.Equals(other.Type) &&
                Description == other.Description &&
                Help == other.Help && 
                Fields?.Count() == other.Fields?.Count() &&
                (Fields?.All(
                     (thisKvp) => other.Fields.TryGetValue(thisKvp.Key, out var otherValue) &&
                     (thisKvp.Value?.Equals(otherValue) ?? otherValue == null)) ?? true);
        }

        public override int GetHashCode()
        {
            return Hashing.CombineHash(
                Type.GetHashCode(),
                Description?.GetHashCode() ?? 1,
                Help?.GetHashCode() ?? 1,
                Fields?.GetHashCode() ?? 1);
        }
    }

    internal readonly struct SchemaTypeName
    {
        public string Name { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsTable { get; init; }

        public static SchemaTypeName RecordTypeName => new () { Name = "Record", IsTable = false };

        public static SchemaTypeName CustomRecordTypeName => new () { Name = "CustomType", IsTable = false };

        public static SchemaTypeName CustomTableTypeName => new () { Name = "CustomType", IsTable = true };

        public static SchemaTypeName TableTypeName => new () { Name = "Record", IsTable = true };
    }
}
