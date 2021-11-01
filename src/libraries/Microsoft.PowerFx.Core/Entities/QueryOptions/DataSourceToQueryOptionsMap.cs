using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities.QueryOptions
{
    internal sealed class DataSourceToQueryOptionsMap
    {
        private readonly Dictionary<DName, TabularDataQueryOptions> _tabularDataQueryOptionsSet = new Dictionary<DName, TabularDataQueryOptions>();

        public bool HasTabualarDataSource(DName tabularDataSourceName)
        {
            return _tabularDataQueryOptionsSet.ContainsKey(tabularDataSourceName);
        }

        public bool HasTabualarDataSource(IExternalTabularDataSource tabularDataSourceInfo)
        {
            return _tabularDataQueryOptionsSet.ContainsKey(tabularDataSourceInfo.EntityName);
        }

        /// <summary>
        /// Adds Associated data source entry if it is valid and not already added to rule.
        /// </summary>
        /// <param name="tabularDataSourceInfo"></param>
        /// <returns>true if this operation resulted in change.</returns>
        public bool AddDataSource(IExternalTabularDataSource tabularDataSourceInfo)
        {
            if (tabularDataSourceInfo == null
                || _tabularDataQueryOptionsSet.ContainsKey(tabularDataSourceInfo.EntityName))
                return false;

            _tabularDataQueryOptionsSet.Add(tabularDataSourceInfo.EntityName, new TabularDataQueryOptions(tabularDataSourceInfo));
            return true;
        }

        public TabularDataQueryOptions GetOrCreateQueryOptions(IExternalTabularDataSource tabularDataSourceInfo)
        {
            if (tabularDataSourceInfo == null) return null;

            if (_tabularDataQueryOptionsSet.ContainsKey(tabularDataSourceInfo.EntityName))
                return _tabularDataQueryOptionsSet[tabularDataSourceInfo.EntityName];

            var newEntry = new TabularDataQueryOptions(tabularDataSourceInfo);
            _tabularDataQueryOptionsSet.Add(tabularDataSourceInfo.EntityName, newEntry);
            return newEntry;
        }

        public TabularDataQueryOptions GetQueryOptions(IExternalTabularDataSource tabularDataSourceInfo)
        {
            if (tabularDataSourceInfo == null) return null;

            return GetQueryOptions(tabularDataSourceInfo.EntityName);
        }

        public TabularDataQueryOptions GetQueryOptions(DName tabularDataSourceInfoName)
        {
            Contracts.AssertValid(tabularDataSourceInfoName);

            if (_tabularDataQueryOptionsSet.ContainsKey(tabularDataSourceInfoName))
                return _tabularDataQueryOptionsSet[tabularDataSourceInfoName];

            return null;
        }

        internal IEnumerable<TabularDataQueryOptions> GetQueryOptions()
        {
            return _tabularDataQueryOptionsSet.Values;
        }

        internal Dictionary<string, object> ToDebugObject()
        {
            try
            {
                var debugObj = new Dictionary<string, object>();

                foreach (var kvp in _tabularDataQueryOptionsSet)
                {
                    debugObj.Add(kvp.Key, kvp.Value.ToDebugObject());
                }

                return debugObj;
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>(){
                    { "message", ex.Message },
                    { "stacktrace", ex.StackTrace}
                };
            }
        }

        /// <summary>
        /// Adds select column to tabular datasource data call.
        /// </summary>
        /// <param name="tabularDataSourceInfo"></param>
        /// <param name="selectFieldName"></param>
        /// <returns></returns>
        internal bool AddSelect(IExternalTabularDataSource tabularDataSourceInfo, DName selectFieldName)
        {
            AddDataSource(tabularDataSourceInfo);

            var returnVal = false;
            returnVal |= _tabularDataQueryOptionsSet[tabularDataSourceInfo.EntityName].AddSelect(selectFieldName);
            returnVal |= tabularDataSourceInfo.QueryOptions.AddSelect(selectFieldName);

            return returnVal;
        }

        public bool HasExpand(string logicalName)
        {
            foreach (var qOptions in _tabularDataQueryOptionsSet)
            {
                foreach (var value in qOptions.Value.ExpandDTypes.Values)
                {
                    if (value.ExpandInfo?.Identity == logicalName)
                        return true;
                }
            }

            return false;
        }

        internal Dictionary<ExpandPath, DType> GetExpandDTypes(IExternalTabularDataSource dsInfo)
        {
            var queryOptions = GetOrCreateQueryOptions(dsInfo);

            return queryOptions.ExpandDTypes;
        }


        internal IEnumerable<TabularDataQueryOptions> GetValues()
        {
            return _tabularDataQueryOptionsSet.Values;
        }

        internal bool HasAnyExpand()
        {
            foreach (var qOptions in _tabularDataQueryOptionsSet)
            {
                if (qOptions.Value.Expands.Count > 0)
                    return true;
            }

            return false;
        }

        internal bool HasNestedManyToOneExpands()
        {
            foreach (var qOptions in _tabularDataQueryOptionsSet)
            {
                if (qOptions.Value.HasNestedManyToOneExpands())
                    return true;
            }

            return false;
        }
    }
}
