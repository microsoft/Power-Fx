// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities; 
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Created by TabularService.GetTableValue
    // Doesn't contain any ServiceProvider which is runtime only
    public class CdpTableValue : TableValue, IRefreshable, IDelegatableTableValue
    {
        public bool IsDelegable => _tabularService.IsDelegable;

        protected internal readonly CdpService _tabularService;

        protected internal readonly ConnectorType _connectorType;

        private IReadOnlyCollection<DValue<RecordValue>> _cachedRows;

        internal readonly HttpClient HttpClient;

        public RecordType TabularRecordType => _tabularService?.TabularRecordType;
        
        public CdpTableValue(CdpService tabularService, ConnectorType connectorType)
            : base(IRContext.NotInSource(new CdpTableType(tabularService.TableType)))
        {
            _tabularService = tabularService;
            _connectorType = connectorType;
            
            HttpClient = tabularService.HttpClient;
        }

        internal CdpTableValue(IRContext irContext)
            : base(irContext)
        {
            _cachedRows = null;
        }

        public override IEnumerable<DValue<RecordValue>> Rows => GetRowsAsync(null, null, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

        public async Task<IReadOnlyCollection<DValue<RecordValue>>> GetRowsAsync(IServiceProvider services, DelegationParameters parameters, CancellationToken cancel)
        {
            if (parameters == null && _cachedRows != null)
            {
                return _cachedRows;
            }

            var op = parameters?.ToOdataParameters();
            var rows = await _tabularService.GetItemsAsync(services, op, cancel).ConfigureAwait(false);

            if (parameters == null)
            {
                _cachedRows = rows;
            }

            return rows;
        }

        public void Refresh()
        {
            _cachedRows = null;
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
