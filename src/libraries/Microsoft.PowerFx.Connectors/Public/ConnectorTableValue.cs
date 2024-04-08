// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Returned by AddTabularConnector
    // Doesn't contain any ServiceProvider which is runtime only
    public class ConnectorTableValue : TableValue, IRefreshable
    {
        public string Name => _tabularService.TableName;

        public string Namespace => _tabularService.Namespace;

        public new TableType Type => _tabularService.TableType;

        protected internal readonly TabularService _tabularService;                        
        
        public ConnectorTableValue(TabularService tabularService, RecordType recordType)
            : base(IRContext.NotInSource(new ConnectorTableType(recordType)))
        {            
            _tabularService = tabularService;            
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
