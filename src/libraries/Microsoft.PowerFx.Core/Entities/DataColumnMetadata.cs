// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities
{
    internal sealed class DataColumnMetadata : IDataColumnMetadata
    {
        private readonly ColumnMetadata _columnMetadata;

        public DataColumnMetadata(ColumnMetadata columnMetadata, DataTableMetadata tableMetadata)
        {
            Contracts.AssertValue(columnMetadata);
            Contracts.AssertValue(tableMetadata);

            _columnMetadata = columnMetadata;
            ParentTableMetadata = tableMetadata;
            IsSearchable = _columnMetadata.LookupMetadata.HasValue && _columnMetadata.LookupMetadata.Value.IsSearchable;
            IsSearchRequired = _columnMetadata.LookupMetadata.HasValue && _columnMetadata.LookupMetadata.Value.IsSearchRequired;
            Type = columnMetadata.Type;
            Name = columnMetadata.Name;
            IsExpandEntity = false;
        }

        public DataColumnMetadata(string name, DType type, DataTableMetadata tableMetadata)
        {
            Contracts.AssertValue(name);
            Contracts.AssertValid(type);
            Contracts.AssertValue(tableMetadata);

            Name = name;
            Type = type;
            ParentTableMetadata = tableMetadata;
            IsSearchable = false;
            IsSearchRequired = false;
            IsExpandEntity = true;
        }

        public bool IsSearchable { get; }

        public bool IsExpandEntity { get; }

        public bool IsSearchRequired { get; }

        public string Name { get; }

        public DataTableMetadata ParentTableMetadata { get; }

        public DType Type { get; }
    }
}
