// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Connectors.Tabular;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Created by TabularService.GetTableValue
    // Doesn't contain any ServiceProvider which is runtime only
    public class ConnectorTableValue : TableValue, IRefreshable, IDelegatableTableValue
    {
        public bool IsDelegable => _tabularService.IsDelegable;

        protected internal readonly TabularService _tabularService;

        protected internal readonly ConnectorType _connectorType;

        public ConnectorRelationships Relationships { get; }

        public ConnectorTableValue(TabularService tabularService, ConnectorType connectorType)
            : base(IRContext.NotInSource(new ConnectorTableType(tabularService.TableType)))
        {
            _tabularService = tabularService;
            _connectorType = connectorType;

            // $$$ Should probably be in ConnectorTableType.AssociatedDataSources
            Relationships = connectorType != null ? new ConnectorRelationships(connectorType) : null;
        }

        internal ConnectorTableValue(IRContext irContext)
            : base(irContext)
        {
        }

        public override IEnumerable<DValue<RecordValue>> Rows => throw new InvalidOperationException("No service context. Make sure to call engine.EnableTabularConnectors().");

        public virtual void Refresh()
        {
        }

        public async Task<IReadOnlyCollection<DValue<RecordValue>>> GetRowsAsync(IServiceProvider services, DelegationParameters parameters, CancellationToken cancel)
        {
            var op = parameters.ToOdataParameters();
            var rows = await _tabularService.GetItemsAsync(services, op, cancel).ConfigureAwait(false);

            return rows;
        }
    }   

    internal static class ODataParametersExtensions
    {
        public static ODataParameters ToOdataParameters(this DelegationParameters parameters)
        {
            DelegationParameterFeatures allowedFeatures = 
                DelegationParameterFeatures.Filter | 
                DelegationParameterFeatures.Top | 
                DelegationParameterFeatures.Columns;
            parameters.EnsureOnlyFeatures(allowedFeatures);

            ODataParameters op = new ODataParameters()
            {
                Filter = parameters.GetOdataFilter(),
                Top = parameters.Top.GetValueOrDefault(),
                Select = parameters.GetColumns()
            };

            return op;
        }
    }
}
