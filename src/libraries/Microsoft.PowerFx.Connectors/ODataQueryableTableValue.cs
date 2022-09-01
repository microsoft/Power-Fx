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
        public readonly ODataParameters ODataParams;

        protected ODataQueryableTableValue(TableType tableType, ODataParameters odataParams = default)
            : base(IRContext.NotInSource(tableType))
        {
            ODataParams = odataParams;
        }

        protected abstract ODataQueryableTableValue WithParameters(ODataParameters odataParamsNew);

        internal sealed override TableValue Filter(LambdaFormulaValue lambda, EvalVisitor runner, EvalVisitorContext context)
        {
            ODataVisitorContext runContext = new (runner, context);
            var filterClause = lambda.Visit(ODataVisitor.I, runContext);
            return WithParameters(ODataParams.WithFilter(filterClause));
        }

        internal sealed override TableValue Sort(LambdaFormulaValue lambda, bool isDescending, EvalVisitor runner, EvalVisitorContext context)
        {
            ODataVisitorContext runContext = new (runner, context);
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

    public readonly struct ODataParameters
    {
        // Missing parameters: skip, skipToken, expand, search, select, apply
        public bool Count { get; }

        public string Filter { get; }

        public string OrderBy { get; }

        public int Top { get; }

        internal ODataParameters(bool count, string filter, string orderby, int top)
        {
            Count = count;
            Filter = filter;
            OrderBy = orderby;
            Top = top;
        }

        public void AddTo(NameValueCollection query)
        {
            if (Count)
            {
                query["$count"] = "true";
            }

            if (Filter != null)
            {
                query["$filter"] = Filter;
            }

            if (OrderBy != null)
            {
                query["$orderby"] = OrderBy;
            }

            if (Top != 0)
            {
                query["$top"] = Top.ToString(CultureInfo.InvariantCulture);
            }
        }

        public Uri GetUri(Uri uriBase)
        {
            UriBuilder uriBuilder = new (uriBase);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            AddTo(query);
            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }

        public ODataParameters WithCount() => new ODataParameters(true, Filter, OrderBy, Top);

        public ODataParameters WithFilter(string filterNew)
        {
            if (Filter != null)
            {
                return new ODataParameters(Count, $"({Filter}) and ({filterNew})", OrderBy, Top);
            }

            return new ODataParameters(Count, filterNew, OrderBy, Top);
        }

        public ODataParameters WithOrderby(string orderbyNew) => new ODataParameters(Count, Filter, orderbyNew, Top);

        public ODataParameters WithTop(int topNew) => new ODataParameters(Count, Filter, OrderBy, topNew);

        public override string ToString() => $"Count={Count}, Filter={Filter ?? "null"} OrderBy={OrderBy ?? "null"} Top={Top}";
    }
}
