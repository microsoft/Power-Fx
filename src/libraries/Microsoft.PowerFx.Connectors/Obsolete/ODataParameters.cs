// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web; 
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part2-url-conventions.pdf
    // These values are not escaped / url encoded. 
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
        // Select is encoded as a comma separate list of fields.
        public IReadOnlyCollection<string> Select { get; init; } = null;

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

        /// <summary>
        /// Convert these OData Paramters to a URL query string.
        /// This will only inlucde non-default values, and merged them into a single 
        /// query string separated with '&'. Like:
        ///    $filter=..&$top=1
        /// </summary>
        /// <returns></returns>
        public string ToQueryString()
        {
            var query = GetNamedValues();

            // NameValueCollection.ToString does not return a query string. 

            StringBuilder sb = new StringBuilder();
            string dil = string.Empty;
            foreach (var kv in query)
            {
                string key = kv.Key;
                object value = kv.Value;
                
                sb.Append(dil);
                sb.Append($"{key}=");

                string encoded;
                if (value is IEnumerable<string> strList)
                {
                    string d2 = string.Empty;
                    foreach (var item in strList)
                    {
                        string encodedItem = HttpUtility.UrlEncode(item);

                        sb.Append(d2);
                        sb.Append(encodedItem);

                        d2 = ",";
                    }
                } 
                else
                {
                    encoded = HttpUtility.UrlEncode(value.ToString());
                    sb.Append(encoded);
                }

                dil = "&";
            }

            return sb.ToString();
        }

        public IReadOnlyDictionary<string, object> GetNamedValues()
        {
            var query = new Dictionary<string, object>();
            AddTo(query);
            return query;
        }

        public void AddTo(IDictionary<string, object> query)
        {
            if (!string.IsNullOrEmpty(Compute))
            {
                query["$compute"] = Compute;
            }

            if (Count)
            {
                query["$count"] = true;
            }

            if (!string.IsNullOrEmpty(Expand))
            {
                query["$expand"] = Expand;
            }

            if (!string.IsNullOrEmpty(Filter))
            {
                query["$filter"] = Filter;
            }

            if (!string.IsNullOrEmpty(Format))
            {
                query["$format"] = Format;
            }

            if (Index != 0)            
            {
                query["$index"] = Index;
            }

            if (Levels != 0)
            {
                query["$levels"] = Levels;
            }

            if (!string.IsNullOrEmpty(OrderBy))
            {
                query["$orderby"] = OrderBy;
            }

            if (!string.IsNullOrEmpty(SchemaVersion))
            {
                query["$schemaversion"] = SchemaVersion;
            }

            if (!string.IsNullOrEmpty(Search))
            {
                query["$search"] = Search;
            }

            if (Select != null && Select.Count > 0)
            {
                query["$select"] = Select;
            }

            if (Skip != 0)
            {
                query["$skip"] = Skip;
            }

            if (Top != 0)
            {
                query["$top"] = Top;
            }
        }        
    }
}
