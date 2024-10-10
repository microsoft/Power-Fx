// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    internal class InternalTableMetadata : IExternalTableMetadata
    {        
        private readonly RecordType _type;

        public InternalTableMetadata(RecordType recordType, string name, string displayName, bool isReadOnly, string parameterPkColumnName = "")
        {
            Contracts.AssertNonEmpty(name);
            Contracts.AssertNonEmpty(displayName);

            _type = recordType;

            Name = name;
            DisplayName = displayName;
            IsReadOnly = isReadOnly;
                        
            ParameterPkColumnName = parameterPkColumnName;
        }        

        public string Name { get; }

        public string DisplayName { get; }

        public string ParameterPkColumnName { get; }

        public bool IsReadOnly { get; }

        public IReadOnlyList<string> KeyColumns => _type._type.DisplayNameProvider.LogicalToDisplayPairs.Select(pair => pair.Key.Value).Where(col => col != "57dfb1b5-7d79-4046-a4da-fd831d5befe1-KeyId").ToList();

        public IReadOnlyList<ColumnMetadata> Columns { get; }

        public ColumnMetadata this[string columnName]
        {
            get
            {
                Contracts.AssertNonEmpty(columnName);
                return TryGetColumn(columnName, out ColumnMetadata columnMetadata) ? columnMetadata : null;
            }
        }

        public bool TryGetColumn(string columnName, out ColumnMetadata column)
        {
            string GetDisplayName(string fieldName) => _type._type.DisplayNameProvider == null || !_type._type.DisplayNameProvider.TryGetDisplayName(new DName(fieldName), out DName displayName) ? fieldName : displayName.Value;
            DataFormat? ToDataFormat(DType dType) => dType.Kind switch
            {
                DKind.Record or DKind.Table or DKind.OptionSetValue => DataFormat.Lookup,
                DKind.String or DKind.Decimal or DKind.Number or DKind.Currency => DataFormat.AllowedValues,
                _ => null
            };            

            Contracts.AssertNonEmpty(columnName);

            if (_type.TryGetUnderlyingFieldType(columnName, out FormulaType ft))
            {
                column = new ColumnMetadata(
                    columnName,
                    ft._type,
                    ToDataFormat(ft._type),
                    GetDisplayName(columnName),
                    false, // is read-only 
                    false, // primary key 
                    false, // isRequired 
                    ColumnCreationKind.UserProvided,
                    ColumnVisibility.Default,
                    columnName,
                    columnName,
                    columnName,
                    null,  // columnLookupMetadata
                    null); // attachmentMetadata

                return true;
            }

            column = null;
            return false;
        }

        /// <summary>
        /// Checks whether specified column can be included in select query option.
        /// </summary>
        /// <param name="selectColumnName"></param>
        internal bool CanIncludeSelect(string selectColumnName)
        {            
            DType colType = DType.Unknown;
            bool hasColumn = TryGetColumn(selectColumnName, out ColumnMetadata columnMetadata);

            if (hasColumn)
            {
                colType = columnMetadata.Type;
            }

            return hasColumn && !colType.IsAttachment;
        }

        /// <summary>
        /// Checks whether specified navigation column can be included in expand query option.
        /// </summary>
        /// <param name="expand"></param>
        internal bool CanIncludeExpand(IExpandInfo expand)
        {            
            string fieldName = expand.PolymorphicParent ?? expand.Name;

            DType colType = DType.Unknown;
            bool hasColumn = TryGetColumn(fieldName, out ColumnMetadata columnMetadata);

            if (hasColumn)
            {
                colType = columnMetadata.Type;
            }

            return hasColumn && (colType.Kind == DKind.Record || colType.Kind == DKind.DataEntity || (colType.Kind == DKind.Polymorphic && ValidatePolymorphicExpand(expand, colType)));
        }

        private bool ValidatePolymorphicExpand(IExpandInfo expand, DType colType)
        {
            if (!colType.HasPolymorphicInfo)
            {
                return false;
            }

            if (colType.PolymorphicInfo.TargetFields.Contains(expand.Name))
            {
                return true;
            }

            // Owner types have different metadata compared to other polymorphics,
            // they use the same field for patching but have different ones for relationships
            // which are always hard-coded to 'owning<user/team>'.
            return colType.PolymorphicInfo.TargetFields.Contains("ownerid") && (expand.Name == "owninguser" || expand.Name == "owningteam");
        }
    }
}
