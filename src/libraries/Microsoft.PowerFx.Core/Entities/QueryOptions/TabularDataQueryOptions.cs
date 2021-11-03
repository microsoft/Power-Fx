using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities.QueryOptions
{
    /// <summary>
    /// Store information about data call queryOptions for a particular TabularDataSource and its navigations.
    /// This should be serializable to "TableQueryOptions" in \src\AppMagic\js\AppMagic.Runtime.App\_AppMagic\Data\Query\QueryOptions.ts
    /// </summary>
    internal sealed class TabularDataQueryOptions
    {
        public IExternalTabularDataSource TabularDataSourceInfo { get; }
        
        public IEnumerable<string> Selects { get { return _selects; } }

        private HashSet<string> _selects { get; }

        /// <summary>
        /// List of navigation datasources and their query options
        /// </summary>
        public IReadOnlyDictionary<ExpandPath, ExpandQueryOptions> Expands { get { return _expandQueryOptions; } }

        private Dictionary<ExpandPath, ExpandQueryOptions> _expandQueryOptions { get; }

        public Dictionary<ExpandPath, DType> ExpandDTypes { get; }

        public TabularDataQueryOptions(IExternalTabularDataSource tabularDataSourceInfo)
        {
            TabularDataSourceInfo = tabularDataSourceInfo;
            _selects = new HashSet<string>();
            var keyColumns = tabularDataSourceInfo.GetKeyColumns();
            foreach (var keyColumn in keyColumns)
                _selects.Add(keyColumn);

            _expandQueryOptions = new Dictionary<ExpandPath, ExpandQueryOptions>();
            ExpandDTypes = new Dictionary<ExpandPath, DType>();
        }

        public bool AddSelect(string selectColumnName)
        {
            if (string.IsNullOrEmpty(selectColumnName))
                return false;

            if (_selects.Contains(selectColumnName)
                || !TabularDataSourceInfo.CanIncludeSelect(selectColumnName))
            {
                return false;
            }

            return _selects.Add(selectColumnName);
        }

        public bool AddSelectMultiple(IEnumerable<string> _selects)
        {
            if (_selects == null)
            {
                return false;
            }

            var retVal = false;

            foreach (var select in _selects)
            {
                retVal |= AddSelect(select);
            }

            return retVal;
        }
        /// <summary>
        /// Helper method used to add related columns like annotated columns for cds navigation fields. ex: _primarycontactid_value
        /// </summary>
        public void AddRelatedColumns()
        {
            if (!(TabularDataSourceInfo is IExternalCdsDataSource))
            {
                return;
            }
            var cdsDataSourceInfo = TabularDataSourceInfo as IExternalCdsDataSource;
            var selectColumnNames = new HashSet<string>(_selects);
            foreach (var select in selectColumnNames)
            {
                if (cdsDataSourceInfo.TryGetRelatedColumn(select, out string additionalColumnName) && !_selects.Contains(additionalColumnName))
                {
                    // Add the Annotated value in case a navigation field is referred in selects. (ex: if the Datasource is Accounts and primarycontactid is in selects also append _primarycontactid_value)
                    _selects.Add(additionalColumnName);
                }
            }
        }

        public bool HasNonKeySelects()
        {
            if (!TabularDataSourceInfo.IsSelectable)
                return false;

            Contracts.Assert(TabularDataSourceInfo.GetKeyColumns().All(x => _selects.Contains(x)));

            return TabularDataSourceInfo.GetKeyColumns().Count() < _selects.Count
                 && _selects.Count < TexlBinding.MaxSelectsToInclude;
        }

        internal bool ReplaceExpandsWithAnnotation(ExpandQueryOptions expand)
        {
            Contracts.AssertValue(expand);
            RemoveExpand(expand.ExpandInfo);
            var selectColumnName = expand.ExpandInfo.ExpandPath.EntityName;
            if (string.IsNullOrEmpty(selectColumnName) || !(TabularDataSourceInfo is IExternalCdsDataSource))
                return false;
            var CdsDataSourceInfo = TabularDataSourceInfo as IExternalCdsDataSource;
            if (_selects.Contains(selectColumnName)
                || !CdsDataSourceInfo.TryGetRelatedColumn(selectColumnName, out string additionalColumnName) || additionalColumnName == null
                || _selects.Contains(additionalColumnName))
            {
                return false;
            }

            return _selects.Add(additionalColumnName);
        }

        internal bool AddExpand(IExpandInfo expandInfo, out ExpandQueryOptions expandQueryOptions)
        {
            if (expandInfo == null || !TabularDataSourceInfo.CanIncludeExpand(expandInfo))
            {
                expandQueryOptions = null;
                return false;
            }

            if (_expandQueryOptions.ContainsKey(expandInfo.ExpandPath))
            {
                expandQueryOptions = (ExpandQueryOptions)_expandQueryOptions[expandInfo.ExpandPath];
                return false;
            }

            expandQueryOptions = ExpandQueryOptions.CreateExpandQueryOptions(expandInfo);
            return AddExpand(expandInfo.ExpandPath, expandQueryOptions);
        }

        private bool AddExpand(ExpandPath expandPath, ExpandQueryOptions expandQueryOptions)
        {
            this._expandQueryOptions.Add(expandPath, expandQueryOptions);
            return true;
        }

        internal bool RemoveExpand(IExpandInfo expandInfo)
        {
            Contracts.AssertValue(expandInfo);
            return _expandQueryOptions.Remove(expandInfo.ExpandPath);
        }

        internal bool TryGetExpandQueryOptions(IExpandInfo expandInfo, out ExpandQueryOptions expandQueryOptions)
        {
            foreach (var expandQueryOptionsKVP in Expands)
            {
                if (expandQueryOptionsKVP.Value.ExpandInfo.ExpandPath == expandInfo.ExpandPath)
                {
                    expandQueryOptions = (ExpandQueryOptions)expandQueryOptionsKVP.Value;
                    return true;
                }
            }

            expandQueryOptions = null;
            return false;
        }

        internal void Merge(TabularDataQueryOptions qo)
        {
            foreach (var select in qo.Selects)
            {
                AddSelect(select);
            }

            foreach (var entry in qo.Expands)
            {
                if (Expands.ContainsKey(entry.Key))
                    MergeQueryOptions((ExpandQueryOptions)Expands[entry.Key], (ExpandQueryOptions)entry.Value);
                else
                    AddExpand(entry.Key, (ExpandQueryOptions)entry.Value);
            }
        }

        /// <summary>
        /// Helper method used to merge two different entity query options.
        /// </summary>
        internal static bool MergeQueryOptions(ExpandQueryOptions original, ExpandQueryOptions added)
        {
            Contracts.AssertValue(original);
            Contracts.AssertValue(added);

            // Skip merge when both instances are same or any of them is null.
            if (original == added
                || original == null
                || added == null)
            {
                return false;
            }

            bool isOriginalModified = false;
            // Update selectedfields first.
            foreach (var selectedFieldToAdd in added.Selects)
            {
                if (original.Selects.Contains(selectedFieldToAdd))
                    continue;

                original.AddSelect(selectedFieldToAdd);
                isOriginalModified = true;
            }

            if (original.Expands.Count == 0 && added.Expands.Count > 0)
            {
                original.SetExpands(added.Expands);
                return true;
            }

            // Go through reachable entity list and update each of it same way.
            var entityPathToQueryOptionsMap = new Dictionary<ExpandPath, ExpandQueryOptions>();
            foreach (var expand in original.Expands)
            {
                if (!entityPathToQueryOptionsMap.ContainsKey(expand.ExpandInfo.ExpandPath))
                    entityPathToQueryOptionsMap[expand.ExpandInfo.ExpandPath] = expand;
            }

            foreach (var expand in added.Expands)
            {
                if (!entityPathToQueryOptionsMap.ContainsKey(expand.ExpandInfo.ExpandPath))
                {
                    isOriginalModified = original.AddExpand(expand.ExpandInfo, out _);
                }
                else
                {
                    isOriginalModified = isOriginalModified || MergeQueryOptions(entityPathToQueryOptionsMap[expand.ExpandInfo.ExpandPath], expand);
                }
            }

            return isOriginalModified;
        }

        internal bool AppendExpandQueryOptions(ExpandQueryOptions mergeExpandValue)
        {
            foreach (var expand in Expands)
            {
                var srcExpandInfo = expand.Value.ExpandInfo;
                var mergedExpandInfo = mergeExpandValue.ExpandInfo;
                if (srcExpandInfo.Identity == mergedExpandInfo.Identity
                    && srcExpandInfo.Name == mergedExpandInfo.Name
                    && srcExpandInfo.IsTable == mergedExpandInfo.IsTable)
                {
                    return MergeQueryOptions((ExpandQueryOptions)expand.Value, mergeExpandValue);
                }

                if (!string.IsNullOrEmpty(mergeExpandValue.ExpandInfo.ExpandPath.RelatedEntityPath)
                    && mergeExpandValue.ExpandInfo.ExpandPath.RelatedEntityPath.Contains(expand.Value.ExpandInfo.ExpandPath.EntityName))
                {
                    return AppendExpandQueryOptions((ExpandQueryOptions)expand.Value, mergeExpandValue);
                }
            }

            _expandQueryOptions[mergeExpandValue.ExpandInfo.ExpandPath] = mergeExpandValue?.Clone();
            return true;
        }

        private bool AppendExpandQueryOptions(ExpandQueryOptions options, ExpandQueryOptions mergeExpandValue)
        {
            foreach (var expand in options.Expands)
            {
                if (expand.ExpandInfo.ExpandPath == mergeExpandValue.ExpandInfo.ExpandPath)
                {
                    return MergeQueryOptions(expand, mergeExpandValue);
                }

                if (mergeExpandValue.ExpandInfo.ExpandPath.RelatedEntityPath.Contains(expand.ExpandInfo.ExpandPath.EntityName))
                {
                    foreach (var childExpand in expand.Expands)
                    {
                        if (AppendExpandQueryOptions(childExpand, mergeExpandValue))
                            return true;
                    }
                    return false;
                }
            }

            return false;
        }

        internal Dictionary<string, object> ToDebugObject()
        {
            var selects = new List<string>();

            foreach (var select in Selects)
            {
                selects.Add(select);
            }

            var expands = new List<object>();
            foreach (var expand in Expands)
            {
                expands.Add(new
                {
                    expandInfo = expand.Value.ExpandInfo.ToDebugString(),
                    expandOptions = expand.Value.ToDebugObject()
                });
            }

            var def = new Dictionary<string, object>()
            {
                { "selects", selects },
                { "expands", expands }
            };

            return def;
        }

        internal bool SelectsSetEquals(IEnumerable<string> enumerable)
        {
            return _selects.SetEquals(enumerable);
        }

        internal bool HasNestedManyToOneExpands()
        {
            foreach (var expandKvp in Expands)
            {
                if (!expandKvp.Value.ExpandInfo.IsTable
                    && ((ExpandQueryOptions)expandKvp.Value).HasManyToOneExpand())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
