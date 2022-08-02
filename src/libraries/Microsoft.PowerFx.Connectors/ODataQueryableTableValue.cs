// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public abstract class ODataQueryableTableValue : QueryableTableValue
    {
        private readonly Uri _uriBase;

        private readonly ODataParams _odataParams;

        // internal ODataQueryableTableValue(IRContext irContext, NameValueCollection odataParams)
        //     : base(irContext)
        // {
        //     ODataParams = odataParams;
        // }

        protected ODataQueryableTableValue(TableType tableType, Uri uriBase, ODataParams odataParams = default)
            : base(IRContext.NotInSource(tableType))
        {
            _uriBase = uriBase;
            _odataParams = odataParams;
        }

        protected abstract ODataQueryableTableValue WithParameters(ODataParams odataParamsNew);

        public Uri GetUri()
        {
            UriBuilder uriBuilder = new UriBuilder(_uriBase);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            _odataParams.AddTo(query);
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }

        internal sealed override TableValue Filter(LambdaFormulaValue lambda, EvalVisitor runner, EvalVisitorContext context)
        {
            DelegationRunContext runContext = new DelegationRunContext(runner, context);
            var filterClause = lambda.Visit(ODataVisitor.I, runContext);
            return WithParameters(_odataParams.WithFilter(filterClause));
        }

        internal sealed override TableValue Sort(LambdaFormulaValue lambda, bool isDescending, EvalVisitor runner, EvalVisitorContext context)
        {
            DelegationRunContext runContext = new DelegationRunContext(runner, context);
            var orderby = lambda.Visit(ODataVisitor.I, runContext);
            if (isDescending)
            {
                orderby += " desc";
            }

            return WithParameters(_odataParams.WithOrderby(orderby));
        }

        internal override TableValue FirstN(int n)
        {
            return WithParameters(_odataParams.WithTop(n));
        }
    }

    public readonly struct ODataParams
    {
        // Missing parameters: skip, skipToken, expand, search, select, apply
        private readonly bool _count;
        private readonly string _filter;
        private readonly string _orderby;
        private readonly int _top;

        internal ODataParams(bool count, string filter, string orderby, int top)
        {
            _count = count;
            _filter = filter;
            _orderby = orderby;
            _top = top;
        }

        internal void AddTo(NameValueCollection query)
        {
            if (_count)
            {
                query["$count"] = "true";
            }

            if (_filter != null)
            {
                query["$filter"] = _filter;
            }

            if (_orderby != null)
            {
                query["$orderby"] = _orderby;
            }

            if (_top != 0)
            {
                query["$top"] = _top.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal ODataParams WithCount() => new ODataParams(true, _filter, _orderby, _top);

        internal ODataParams WithFilter(string filterNew)
        {
            if (_filter != null)
            {
                return new ODataParams(_count, $"{_filter} and {filterNew}", _orderby, _top);
            }

            return new ODataParams(_count, filterNew, _orderby, _top);
        }

        internal ODataParams WithOrderby(string orderbyNew) => new ODataParams(_count, _filter, orderbyNew, _top);

        internal ODataParams WithTop(int topNew) => new ODataParams(_count, _filter, _orderby, topNew);
    }
}
