// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Tabular
{
    internal class DefaultCDPDelegationParameter : DelegationParameters
    {
        private readonly int _maxRows;

        public override DelegationParameterFeatures Features => throw new NotImplementedException();

        private readonly FormulaType _expectedReturnType;

        public override FormulaType ExpectedReturnType => throw new NotImplementedException();

        public DefaultCDPDelegationParameter(FormulaType expectedReturnType, int maxRows)
        {
            _expectedReturnType = expectedReturnType;
            _maxRows = maxRows;
        }

        public override string GetODataApply()
        {
            return string.Empty;
        }

        public override string GetOdataFilter()
        {
            return string.Empty;
        }

        public override string GetODataQueryString()
        {
            var sb = new StringBuilder();
            sb.Append($"$top={_maxRows}");
            return sb.ToString();
        }

        public override IReadOnlyCollection<(string, bool)> GetOrderBy()
        {
            return Array.Empty<(string, bool)>();
        }

        public override bool ReturnTotalCount()
        {
            return false;
        }
    }
}
