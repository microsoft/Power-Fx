// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorTableValue : TableValue, IRefreshable
    {
        public string Name { get; }

        public string Namespace => _tabularFunctions[0].Namespace;

        protected internal readonly IReadOnlyList<ConnectorFunction> _tabularFunctions;
        protected readonly ConnectorFunction _getItems;

        public new TableType Type => _tableType;

        protected readonly TableType _tableType;

        public ConnectorTableValue(string tableName, IReadOnlyList<ConnectorFunction> tabularFunctions, RecordType recordType)
            : base(IRContext.NotInSource(new ConnectorTableType(recordType)))
        {
            Name = tableName;

            _tabularFunctions = tabularFunctions;                        
            _getItems = _tabularFunctions.First(f => f.Name.Contains("GetItemsV2"));
            _tableType = recordType.ToTable();
        }

        public ConnectorTableValue(RecordType recordType) 
            : this(recordType.ToTable())
        {
            throw new NotImplementedException("This constructor should never be called. We need to set tabular functions.");
        }

        public ConnectorTableValue(TableType type) 
            : this(IRContext.NotInSource(type))
        {
            throw new NotImplementedException("This constructor should never be called. We need to set tabular functions.");
        }

        internal ConnectorTableValue(IRContext irContext) 
            : base(irContext)
        {
        }

        public override IEnumerable<DValue<RecordValue>> Rows => GetRowsInternal().ConfigureAwait(false).GetAwaiter().GetResult();

        protected virtual Task<IEnumerable<DValue<RecordValue>>> GetRowsInternal()
        {
            throw new Exception("No HttpClient context");
        }

        public virtual void Refresh()
        {                        
        }
    }
}
