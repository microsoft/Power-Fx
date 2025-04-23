// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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

        internal readonly IReadOnlyDictionary<string, Relationship> Relationships;

        private IReadOnlyCollection<DValue<RecordValue>> _cachedRows;

        internal readonly HttpClient HttpClient;

        public RecordType RecordType => _tabularService?.RecordType;
        
        internal CdpTableValue(CdpService tabularService, IReadOnlyDictionary<string, Relationship> relationships)
            : base(IRContext.NotInSource(tabularService.TableType))
        {
            _tabularService = tabularService;
            Relationships = relationships;                        
            HttpClient = tabularService.HttpClient;
        }

        internal CdpTableValue(IRContext irContext)
            : base(irContext)
        {
            _cachedRows = null;
        }

        public override IEnumerable<DValue<RecordValue>> Rows => GetRowsAsync(null, null, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

        public DelegationParameterFeatures SupportedFeatures => DelegationParameterFeatures.Filter |
                DelegationParameterFeatures.Top |
                DelegationParameterFeatures.Columns | // $select
                DelegationParameterFeatures.Sort | // $orderby
                DelegationParameterFeatures.ApplyGroupBy |
                DelegationParameterFeatures.ApplyTopLevelAggregation |
                DelegationParameterFeatures.Count;

        public async Task<IReadOnlyCollection<DValue<RecordValue>>> GetRowsAsync(IServiceProvider services, DelegationParameters parameters, CancellationToken cancel)
        {
            if (parameters == null && _cachedRows != null)
            {
                return _cachedRows;
            }

            var rows = await _tabularService.GetItemsAsync(services, parameters, cancel).ConfigureAwait(false);

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

        public async Task<FormulaValue> ExecuteQueryAsync(IServiceProvider services, DelegationParameters parameters, CancellationToken cancel)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var value = await _tabularService.ExecuteQueryAsync(services, parameters, cancel).ConfigureAwait(false);

            var expectedRT = parameters.ExpectedReturnType;
            if (expectedRT is not AggregateType) 
            {
                if (value.Type is not AggregateType)
                {
                    var expectedValue = ConvertToExpectedType(expectedRT, value);
                    return expectedValue;
                }
                else if (value.Type is RecordType resultRT)
                {
                    var expectedValue = await ExtractResultAsync((RecordValue)value, parameters, cancel).ConfigureAwait(false);
                    return expectedValue;
                }
                else
                {
                    throw new InvalidOperationException($"Invalid result type {value.Type}, expected type was {expectedRT}");
                }
            }
            else
            {
                throw new InvalidOperationException($"Invalid expected type {expectedRT} for {nameof(ExecuteQueryAsync)}");
            }
        }

        private static async Task<FormulaValue> ExtractResultAsync(RecordValue value, DelegationParameters parameters, CancellationToken cancellationToken)
        {
            var valueTable = (TableValue)(await value.GetFieldAsync(DelegationParameters.ODataResultFieldName, cancellationToken).ConfigureAwait(false));
            if (valueTable.Rows.Count() != 1)
            {
                throw new InvalidOperationException("value Table should always have 1 rows for aggregation result");
            }

            var row = valueTable.Rows.First();

            if (row.IsError)
            {
                return row.Error;
            }

            var valueRecord = row.Value;
            var result = await valueRecord.GetFieldAsync(DelegationParameters.ODataAggregationResultFieldName, cancellationToken).ConfigureAwait(false);
            result = ConvertToExpectedType(parameters.ExpectedReturnType, result);
            return result;
        }

        private static FormulaValue ConvertToExpectedType(FormulaType expectedType, FormulaValue value)
        {
            var valueType = value.Type;

            if (value is BlankValue)
            {
                return value;
            }

            if (expectedType == valueType)
            {
                return value;
            }
            else if (expectedType == FormulaType.Number && valueType == FormulaType.Decimal)
            {
                return FormulaValue.New(Convert.ToDouble(((DecimalValue)value).Value));
            }
            else if (expectedType == FormulaType.Decimal && valueType == FormulaType.Number)
            {
                return FormulaValue.New(Convert.ToDecimal(((NumberValue)value).Value));
            }
            else
            {
                throw new InvalidOperationException($"Expected type {expectedType} can not be converted to {valueType}");
            }
        }
    }
}
