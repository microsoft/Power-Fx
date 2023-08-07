// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// Aggregate Type Definition derived from a .fx.yaml file
    /// This may recursively refer to itself or other types, and so we resolve
    /// the field types lazily using the DefinedTypeSymbolTable 
    /// passed to <see cref="FormulaTypeSchema.ToFormulaType(DefinedTypeSymbolTable, SerializerSerttings)"/>"/>.
    /// </summary>
    internal class UserDefinedRecordType : RecordType
    {
        private readonly FormulaTypeSchema _backingSchema;
        private readonly DefinedTypeSymbolTable _symbolTable;
        private readonly SerializerSerttings _settings;

        public override IEnumerable<string> FieldNames => _backingSchema.Fields?.Keys ?? Enumerable.Empty<string>();

        public UserDefinedRecordType(FormulaTypeSchema backingSchema, DefinedTypeSymbolTable definedTypes)
        {
            _backingSchema = backingSchema;
            _symbolTable = definedTypes;
        }

        public UserDefinedRecordType(FormulaTypeSchema backingSchema, DefinedTypeSymbolTable definedTypes, SerializerSerttings settings)
        {
            _backingSchema = backingSchema;
            _symbolTable = definedTypes;
            _settings = settings;
        }

        public override bool TryGetFieldType(string name, out FormulaType type)
        {
            if (_backingSchema.Fields.TryGetValue(name, out var fieldType))
            {
                type = fieldType.ToFormulaType(_symbolTable, _settings);
                return true;
            }

            type = FormulaType.Blank;
            return false;
        }

        public override bool Equals(object o)
        {
            return o is UserDefinedRecordType other && 
                _backingSchema == other._backingSchema &&
                _symbolTable == other._symbolTable;
        }

        public override int GetHashCode()
        {
            return Hashing.CombineHash(_backingSchema.GetHashCode(), _symbolTable.GetHashCode());
        }
    }
}
