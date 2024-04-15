// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web; 
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part2-url-conventions.pdf
    public class ODataParameters
    {
        // $compute
        public string Compute { get; init; } = null;

        // $count
        public bool Count { get; init; } = false;

        // $expand
        public string Expand { get; init; } = null;

        // $filter
        public string Filter { get; init; } = null;

        // $format
        public string Format { get; init; } = null;

        // $index
        public int Index { get; init; } = 0;

        // $levels
        public int Levels { get; init; } = 0;

        // $orderby
        public string OrderBy { get; init; } = null;

        // $schemaversion
        public string SchemaVersion { get; init; } = null;

        // $search
        public string Search { get; init; } = null;

        // $select
        public string Select { get; init; } = null;

        // $skip
        public int Skip { get; init; } = 0;

        // $top
        public int Top { get; init; } = 0;

        public ODataParameters()
        {
        }

        public ODataParameters(ODataParameters oDataParameters)
        {
            Compute = oDataParameters.Compute;
            Count = oDataParameters.Count;
            Expand = oDataParameters.Expand;
            Filter = oDataParameters.Filter;
            Format = oDataParameters.Format;
            Index = oDataParameters.Index;
            Levels = oDataParameters.Levels;
            OrderBy = oDataParameters.OrderBy;
            SchemaVersion = oDataParameters.SchemaVersion;
            Search = oDataParameters.Search;
            Select = oDataParameters.Select;
            Skip = oDataParameters.Skip;
            Top = oDataParameters.Top;
        }

        public IReadOnlyList<NamedValue> GetNamedValues()
        {
            List<NamedValue> namedValues = new ();

            if (!string.IsNullOrEmpty(Compute))
            {
                namedValues.Add(new NamedValue("$compute", FormulaValue.New(Compute)));
            }

            if (Count)
            {
                namedValues.Add(new NamedValue("$count", FormulaValue.New(true)));
            }

            if (!string.IsNullOrEmpty(Expand))
            {
                namedValues.Add(new NamedValue("$expand", FormulaValue.New(Expand)));
            }

            if (!string.IsNullOrEmpty(Filter))
            {
                namedValues.Add(new NamedValue("$filter", FormulaValue.New(Filter)));
            }

            if (!string.IsNullOrEmpty(Format))
            {
                namedValues.Add(new NamedValue("$format", FormulaValue.New(Format)));
            }

            if (Index != 0)
            {
                namedValues.Add(new NamedValue("$index", FormulaValue.New(Index)));
            }

            if (Levels != 0)
            {
                namedValues.Add(new NamedValue("$levels", FormulaValue.New(Levels)));
            }

            if (!string.IsNullOrEmpty(OrderBy))
            {
                namedValues.Add(new NamedValue("$orderby", FormulaValue.New(OrderBy)));
            }

            if (!string.IsNullOrEmpty(SchemaVersion))
            {
                namedValues.Add(new NamedValue("$schemaversion", FormulaValue.New(SchemaVersion)));
            }

            if (!string.IsNullOrEmpty(Search))
            {
                namedValues.Add(new NamedValue("$search", FormulaValue.New(Search)));
            }

            if (!string.IsNullOrEmpty(Select))
            {
                namedValues.Add(new NamedValue("$select", FormulaValue.New(Select)));
            }

            if (Skip != 0)
            {
                namedValues.Add(new NamedValue("$skip", FormulaValue.New(Skip)));
            }

            if (Top != 0)
            {
                namedValues.Add(new NamedValue("$top", FormulaValue.New(Top)));
            }

            return namedValues;
        }        

        public Uri GetUri(Uri uriBase)
        {
            UriBuilder uriBuilder = new (uriBase);
            NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
            
            foreach (NamedValue nv in GetNamedValues())
            {
                query[nv.Name] = nv.Value.ToString();
            }

            uriBuilder.Query = query.ToString();
            return uriBuilder.Uri;
        }
    }
}
