// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Web;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public abstract class ODataQueryableTableValue : QueryableTableValue
    {
        private readonly Uri _uriBase;
        internal readonly NameValueCollection ODataParams;

        // internal ODataQueryableTableValue(IRContext irContext, NameValueCollection odataParams)
        //     : base(irContext)
        // {
        //     ODataParams = odataParams;
        // }

        protected ODataQueryableTableValue(TableType tableType, Uri uriBase, NameValueCollection odataParams = null)
            : base(IRContext.NotInSource(tableType))
        {
            _uriBase = uriBase;
            ODataParams = odataParams;
        }

        protected abstract ODataQueryableTableValue WithQuery(NameValueCollection odataParamsNew);

        public Uri GetUri()
        {
            UriBuilder uriBuilder = new UriBuilder(_uriBase);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            if (ODataParams != null)
            {
                query.Add(ODataParams);
            }

            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }

        internal sealed override TableValue Filter(LambdaFormulaValue lambda)
        {
            // TODO: Coalesce with existing filter
            var filterClause = lambda.Visit(ODataVisitor.I, null);
            NameValueCollection odataParamsNew = ODataParams == null ? new NameValueCollection() : new NameValueCollection(ODataParams);
            odataParamsNew["$filter"] = filterClause;
            return WithQuery(odataParamsNew);
        }
    }
}
