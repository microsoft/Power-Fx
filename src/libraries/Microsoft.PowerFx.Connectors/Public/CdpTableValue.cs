// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Connectors.Tabular;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Represents a table value created by TabularService.GetTableValue. Does not contain any ServiceProvider which is runtime only.
    /// </summary>
    public class CdpTableValue : TableValue, IRefreshable, IDelegatableTableValue
    {
        /// <summary>
        /// Gets a value indicating whether the table is delegable.
        /// </summary>
        public bool IsDelegable => _tabularService.IsDelegable;

        protected internal readonly CdpService _tabularService;        

        internal readonly IReadOnlyDictionary<string, Relationship> Relationships;

        /// <summary>
        /// caches result of <see cref="Rows"/>.
        /// </summary>
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

        /// <summary>
        /// Gets the rows of the table, fetching and caching them if necessary.
        /// </summary>
        public override IEnumerable<DValue<RecordValue>> Rows
        {
            get
            {
                if (_cachedRows == null)
                {
                    // first time through, fetch and cache
                    _cachedRows = GetRowsAsync(
                        services: null,
                        parameters: new DefaultCDPDelegationParameter(
                            RecordType.ToTable(),
                            _tabularService.ConnectorSettings.MaxRows),
                        cancel: CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                }

                return _cachedRows;
            }
        }

        /// <summary>
        /// Gets the supported delegation features for this table.
        /// </summary>
        public DelegationParameterFeatures SupportedFeatures => DelegationParameterFeatures.Filter |
                DelegationParameterFeatures.Top |
                DelegationParameterFeatures.Columns | // $select
                DelegationParameterFeatures.Sort | // $orderby
                DelegationParameterFeatures.ApplyGroupBy |
                DelegationParameterFeatures.ApplyTopLevelAggregation |
                DelegationParameterFeatures.Count;

        /// <summary>
        /// Asynchronously gets the rows of the table.
        /// </summary>
        public async Task<IReadOnlyCollection<DValue<RecordValue>>> GetRowsAsync(IServiceProvider services, DelegationParameters parameters, CancellationToken cancel)
        {
            var rows = await _tabularService.GetItemsAsync(services, parameters, cancel).ConfigureAwait(false);
            return rows;
        }

        /// <summary>
        /// Refreshes the cached rows.
        /// </summary>
        public void Refresh()
        {
            _cachedRows = null;
        }

        /// <summary>
        /// Asynchronously executes a query and returns the result as a FormulaValue.
        /// </summary>
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
                return FormulaValue.NewBlank(expectedType);
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
