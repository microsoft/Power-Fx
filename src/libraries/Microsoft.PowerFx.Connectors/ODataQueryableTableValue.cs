// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public abstract class ODataQueryableTableValue : QueryableTableValue
    {
        public readonly ODataParams ODataParams;

        protected ODataQueryableTableValue(TableType tableType, ODataParams odataParams = default)
            : base(IRContext.NotInSource(tableType))
        {
            ODataParams = odataParams;
        }

        protected abstract ODataQueryableTableValue WithParameters(ODataParams odataParamsNew);

        internal sealed override TableValue Filter(LambdaFormulaValue lambda, EvalVisitor runner, EvalVisitorContext context)
        {
            ODataVisitorContext runContext = new ODataVisitorContext(runner, context);
            var filterClause = lambda.Visit(ODataVisitor.I, runContext);
            return WithParameters(ODataParams.WithFilter(filterClause));
        }

        internal sealed override TableValue Sort(LambdaFormulaValue lambda, bool isDescending, EvalVisitor runner, EvalVisitorContext context)
        {
            ODataVisitorContext runContext = new ODataVisitorContext(runner, context);
            var orderby = lambda.Visit(ODataVisitor.I, runContext);
            if (isDescending)
            {
                orderby += " desc";
            }

            return WithParameters(ODataParams.WithOrderby(orderby));
        }

        internal override TableValue FirstN(int n)
        {
            return WithParameters(ODataParams.WithTop(n));
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

        public void AddTo(NameValueCollection query)
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

        public Uri GetUri(Uri uriBase)
        {
            UriBuilder uriBuilder = new UriBuilder(uriBase);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            AddTo(query);
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }

        public ODataParams WithCount() => new ODataParams(true, _filter, _orderby, _top);

        public ODataParams WithFilter(string filterNew)
        {
            if (_filter != null)
            {
                return new ODataParams(_count, $"({_filter}) and ({filterNew})", _orderby, _top);
            }

            return new ODataParams(_count, filterNew, _orderby, _top);
        }

        public ODataParams WithOrderby(string orderbyNew) => new ODataParams(_count, _filter, orderbyNew, _top);

        public ODataParams WithTop(int topNew) => new ODataParams(_count, _filter, _orderby, topNew);
    }
}
