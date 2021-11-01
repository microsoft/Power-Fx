using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Entities.QueryOptions
{
    /// <summary>
    /// Store information about data call queryOptions for a particular Navigation data source.
    /// </summary>
    internal sealed class ExpandQueryOptions
    {
        public IExpandInfo ExpandInfo { get; }

        public bool IsRoot { get; } // True if this is a root node.
        public readonly ExpandQueryOptions Parent;

        private readonly HashSet<string> _selects;  // List of selected fields if any on a particular entity.
        private readonly HashSet<string> _keyColumns;  // List of key columns on a particular entity.
        private HashSet<ExpandQueryOptions> _expands;

        internal ExpandQueryOptions Clone()
        {
            var clone = new ExpandQueryOptions(ExpandInfo?.Clone(), _selects, IsRoot, Parent);
            foreach (var expand in Expands)
            {
                clone.AddExpand(expand.Clone());
            }

            return clone;
        }

        private ExpandQueryOptions(IExpandInfo expandInfo, IEnumerable<string> selects, bool isRoot, ExpandQueryOptions parent)
        {
            Contracts.AssertValue(expandInfo);
            Contracts.AssertValue(selects);
            Contracts.AssertValueOrNull(parent);

            ExpandInfo = expandInfo;

            _selects = new HashSet<string>();
            _keyColumns = new HashSet<string>();

            foreach (var select in selects)
                _selects.Add(select);

            var parentDataSource = ExpandInfo.ParentDataSource as IExternalTabularDataSource;
            var keyColumns = parentDataSource?.GetKeyColumns(ExpandInfo);
            foreach (var keyColumn in keyColumns)
                _selects.Add(keyColumn);

            _expands = new HashSet<ExpandQueryOptions>();
            IsRoot = isRoot;
            Parent = parent;
        }

        public bool SelectsEqualKeyColumns()
        {
            return _keyColumns.Count == _selects.Count;
        }

        public IEnumerable<string> Selects
        {
            get
            {
                return _selects;
            }
        }

        // List of entities reachable from this node.
        public IReadOnlyCollection<ExpandQueryOptions> Expands => _expands;

        public bool AddSelect(string selectColumnName)
        {
            if (string.IsNullOrEmpty(selectColumnName))
                return false;

            var parentDataSource = ExpandInfo.ParentDataSource as IExternalTabularDataSource;
            if (!parentDataSource.CanIncludeSelect(ExpandInfo, selectColumnName))
                return false;

            return _selects.Add(selectColumnName);
        }

        public void AddRelatedColumns()
        {
            if (!(ExpandInfo.ParentDataSource is IExternalCdsDataSource))
            {
                return;
            }
            var CdsDataSourceInfo = ExpandInfo.ParentDataSource as IExternalCdsDataSource;
            var selectColumnNames = new HashSet<string>(_selects);
            foreach (var select in selectColumnNames)
            {
                if (CdsDataSourceInfo.TryGetRelatedColumn(select, out string additionalColumnName) && !_selects.Contains(additionalColumnName))
                {
                    // Add the Annotated value in case a navigation field is referred in selects. (ex: if the Datasource is Accounts and primarycontactid is in selects also append _primarycontactid_value)
                    _selects.Add(additionalColumnName);
                }
            }
        }

        /// <summary>
        /// Remove expands and replace it with annotated select column
        /// </summary>
        internal bool ReplaceExpandsWithAnnotation(ExpandQueryOptions expand)
        {
            RemoveExpand(expand);
            var selectColumnName = expand.ExpandInfo.ExpandPath.EntityName;
            if (string.IsNullOrEmpty(selectColumnName))
                return false;

            if (ExpandInfo.ParentDataSource == null || !(ExpandInfo.ParentDataSource is IExternalCdsDataSource))
            {
                return false;
            }
            var parentDataSource = ExpandInfo.ParentDataSource as IExternalCdsDataSource;
            if (!parentDataSource.Document.GlobalScope.TryGetCdsDataSourceWithLogicalName(parentDataSource.DatasetName, ExpandInfo.Identity, out var expandDataSourceInfo) || expandDataSourceInfo == null)
            {
                return false;
            }
            parentDataSource.TryGetRelatedColumn(selectColumnName, out string additionalColumnName, expandDataSourceInfo.TableDefinition);
            if (additionalColumnName == null || _selects.Contains(additionalColumnName))
            {
                return false;
            }
            return _selects.Add(additionalColumnName);
        }

        internal bool AddExpand(IExpandInfo expandInfoToAdd, out ExpandQueryOptions expand)
        {
            if (expandInfoToAdd == null)
            {
                expand = null;
                return false;
            }

            var parentDataSource = ExpandInfo.ParentDataSource as IExternalTabularDataSource;
            if (!parentDataSource.CanIncludeExpand(ExpandInfo, expandInfoToAdd))
            {
                expand = null;
                return false;
            }

            expand = CreateExpandQueryOptions(expandInfoToAdd);

            return AddExpand(expand);
        }

        private bool AddExpand(ExpandQueryOptions expand)
        {
            _expands.Add(expand);
            return true;
        }

        internal bool RemoveExpand(ExpandQueryOptions expand)
        {
            _expands.Remove(expand);
            return true;
        }

        internal void SetExpands(IReadOnlyCollection<ExpandQueryOptions> expands)
        {
            _expands = new HashSet<ExpandQueryOptions>(expands);
        }

        public static ExpandQueryOptions CreateExpandQueryOptions(IExpandInfo entityInfo)
        {
            Contracts.AssertValue(entityInfo);

            return new ExpandQueryOptions(entityInfo, selects: new HashSet<string>(), isRoot: true, parent: null);
        }

        public static Dictionary<ExpandPath, ExpandQueryOptions> MergeQueryOptions(Dictionary<ExpandPath, ExpandQueryOptions> left, Dictionary<ExpandPath, ExpandQueryOptions> right)
        {
            var merged = new Dictionary<ExpandPath, ExpandQueryOptions>(left);
            foreach (var entry in right)
            {
                if (!merged.TryGetValue(entry.Value.ExpandInfo.ExpandPath, out var selectedProjection))
                {
                    merged[entry.Value.ExpandInfo.ExpandPath] = entry.Value?.Clone();
                }
                else
                {
                    MergeQueryOptions(selectedProjection, entry.Value);
                }
            }

            return merged;
        }

        /// <summary>
        /// Helper method used to merge two different entity query options.
        /// </summary>
        public static bool MergeQueryOptions(ExpandQueryOptions original, ExpandQueryOptions added)
        {
            Contracts.AssertValue(original);
            Contracts.AssertValue(added);

            if (original == added)
                return false;

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
                    entityPathToQueryOptionsMap[expand.ExpandInfo.ExpandPath] = expand?.Clone();
            }

            foreach (var expand in added.Expands)
            {
                if (!entityPathToQueryOptionsMap.ContainsKey(expand.ExpandInfo.ExpandPath))
                {
                    original.AddExpand(expand);
                    isOriginalModified = true;
                }
                else
                {
                    isOriginalModified = isOriginalModified || MergeQueryOptions(entityPathToQueryOptionsMap[expand.ExpandInfo.ExpandPath], expand);
                }
            }

            return isOriginalModified;
        }

        public ExpandQueryOptions AppendDataEntity(IExpandInfo expandInfo)
        {
            var queryOptions = CreateExpandQueryOptions(expandInfo);
            foreach (var currentExpand in Expands)
            {
                if (!currentExpand.ExpandInfo.Equals(queryOptions.ExpandInfo)) continue;

                foreach (var qoSelect in queryOptions.Selects)
                    currentExpand.AddSelect(qoSelect);

                return currentExpand;
            }

            var expand = new ExpandQueryOptions(queryOptions.ExpandInfo, queryOptions.Selects, isRoot: false, parent: this);
            AddExpand(expand);
            return expand;
        }

        internal Dictionary<string, object> ToDebugObject()
        {
            var jsonArrSelects = new List<string>();

            foreach (var select in Selects)
            {
                jsonArrSelects.Add(select);
            }

            var expands = new List<object>();
            foreach (var expand in Expands)
            {
                expands.Add(new
                {
                    expandInfo = expand.ExpandInfo.ToDebugString(),
                    expandOptions = expand.ToDebugObject()
                });
            }

            var def = new Dictionary<string, object>()
            {
                { "propertyName", ExpandInfo.Name },
                { "selects", jsonArrSelects },
                { "expands", expands }
            };

            return def;
        }

        internal bool HasManyToOneExpand()
        {
            foreach (var expand in Expands)
            {
                if (!expand.ExpandInfo.IsTable)
                    return true;
            }

            return false;
        }
    }
}
