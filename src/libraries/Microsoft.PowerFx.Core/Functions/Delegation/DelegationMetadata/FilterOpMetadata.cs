// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    // Defines filter operation metadata.
    internal sealed class FilterOpMetadata : OperationCapabilityMetadata
    {
        private readonly Dictionary<DPath, DelegationCapability> _columnCapabilities;
        private readonly Dictionary<DPath, DelegationCapability> _columnRestrictions;
        private readonly DelegationCapability? _filterFunctionsSupportedByTable;

        // Filter functions supported at the table level.
        // If no capability at column level specified then this would be the default filter functionality supported by column.
        private readonly DelegationCapability _defaultCapabilities;

        public FilterOpMetadata(DType tableSchema, Dictionary<DPath, DelegationCapability> columnRestrictions, 
            Dictionary<DPath, DelegationCapability> columnCapabilities, 
            DelegationCapability filterFunctionsSupportedByAllColumns,
            DelegationCapability? filterFunctionsSupportedByTable)
            : base(tableSchema)
        {
            Contracts.AssertValue(columnRestrictions);
            Contracts.AssertValue(columnCapabilities);

            _columnCapabilities = columnCapabilities;
            _columnRestrictions = columnRestrictions;
            _filterFunctionsSupportedByTable = filterFunctionsSupportedByTable;
            _defaultCapabilities = filterFunctionsSupportedByAllColumns;
            if (_filterFunctionsSupportedByTable != null)
                _defaultCapabilities = filterFunctionsSupportedByAllColumns | DelegationCapability.Filter;

        }

        protected override Dictionary<DPath, DelegationCapability> ColumnRestrictions { get { return _columnRestrictions; } }

        public override DelegationCapability DefaultColumnCapabilities { get { return _defaultCapabilities; } }

        public override DelegationCapability TableCapabilities
        {
            get
            {
                if (_filterFunctionsSupportedByTable.HasValue)
                    return _filterFunctionsSupportedByTable.Value;
                else
                {
                    // If there are no capabilities defined at column level then filter is not supported.
                    // Otherwise this simply means that filter operators at table level are not supported.
                    // For example, Filter(CDS, Lower(Col1) != Lower(Col2)), here != operator at table level needs to be supported as it's not operating on any column directly. 
                    if (DefaultColumnCapabilities.Capabilities == DelegationCapability.None)
                        return DelegationCapability.None;
                    else
                        return DelegationCapability.Filter;
                }
            }
        }

        public override bool TryGetColumnCapabilities(DPath columnPath, out DelegationCapability capabilities)
        {
            Contracts.AssertValid(columnPath);

            // See if there is a specific capability defined for column.
            // If not then just return default one.
            if (!_columnCapabilities.TryGetValue(columnPath, out capabilities))
                return base.TryGetColumnCapabilities(columnPath, out capabilities);

            // If metadata specified any restrictions for this column then apply those
            // before returning capabilities.
            DelegationCapability restrictions;
            if (TryGetColumnRestrictions(columnPath, out restrictions))
                capabilities &= ~restrictions;

            return true;
        }

        public override bool IsDelegationSupportedByTable(DelegationCapability delegationCapability)
        {
            if (_filterFunctionsSupportedByTable.HasValue)
                return _filterFunctionsSupportedByTable.Value.HasCapability(delegationCapability.Capabilities);
            else
                return base.IsDelegationSupportedByTable(delegationCapability); /* This is needed for compatibility with older metadata */
        }

        public bool IsColumnSearchable(DPath columnPath)
        {
            Contracts.AssertValid(columnPath);

            return IsDelegationSupportedByColumn(columnPath, DelegationCapability.Filter | DelegationCapability.Contains) ||
                IsDelegationSupportedByColumn(columnPath, DelegationCapability.Filter | DelegationCapability.IndexOf | DelegationCapability.GreaterThan) ||
                IsDelegationSupportedByColumn(columnPath, DelegationCapability.Filter | DelegationCapability.SubStringOf | DelegationCapability.Equal);
        }
    }
}
