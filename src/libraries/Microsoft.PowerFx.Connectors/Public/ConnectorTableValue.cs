// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        private readonly IReadOnlyList<ConnectorFunction> _tabularFunctions;
        private readonly ConnectorFunction _getItems;
        private IEnumerable<DValue<RecordValue>> _cachedRows;

        public ConnectorTableValue(string tableName, IReadOnlyList<ConnectorFunction> tabularFunctions, RecordType recordType)
            : this(IRContext.NotInSource(recordType.ToTable()))
        {
            Name = tableName;

            _tabularFunctions = tabularFunctions;            
            _cachedRows = null;
            _getItems = _tabularFunctions.First(f => f.Name.Contains("GetItems"));
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

        public override IEnumerable<DValue<RecordValue>> Rows => GetRows().ConfigureAwait(false).GetAwaiter().GetResult();

        private async Task<IEnumerable<DValue<RecordValue>>> GetRows()
        {
            if (_cachedRows != null)
            {
                return _cachedRows;
            }

            FormulaValue rows = await _getItems.InvokeAsync(Array.Empty<FormulaValue>(), null, CancellationToken.None).ConfigureAwait(false);

            return null;
        }

        public void Refresh()
        {            
            _cachedRows = null;
        }
    }
}
