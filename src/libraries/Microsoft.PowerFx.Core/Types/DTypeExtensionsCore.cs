// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types
{
    internal static class DTypeExtensionsCore
    {
        internal static bool AssociateDataSourcesToSelect(this DType self, DataSourceToQueryOptionsMap dataSourceToQueryOptionsMap, string columnName, DType columnType, bool skipIfNotInSchema = false, bool skipExpands = false)
        {
            Contracts.AssertValue(dataSourceToQueryOptionsMap);
            Contracts.AssertNonEmpty(columnName);
            Contracts.AssertValue(columnType);

            var retval = false;
            if (self.HasExpandInfo && self.ExpandInfo != null && !skipExpands)
            {
                var qOptions = dataSourceToQueryOptionsMap.GetOrCreateQueryOptions(self.ExpandInfo.ParentDataSource as IExternalTabularDataSource);
                retval |= qOptions.AddExpand(self.ExpandInfo, out var expandQueryOptions);
                if (expandQueryOptions != null)
                {
                    retval |= expandQueryOptions.AddSelect(columnName);
                }
            }
            else
            {
                foreach (var tabularDataSource in self.AssociatedDataSources)
                {
                    // Skip if this column doesn't belong to this datasource.
                    if (skipIfNotInSchema && !tabularDataSource.Type.Contains(new DName(columnName)))
                    {
                        continue;
                    }

                    retval |= dataSourceToQueryOptionsMap.AddSelect((IExternalTabularDataSource)tabularDataSource, new DName(columnName));

                    if (columnType.IsExpandEntity && columnType.ExpandInfo != null && !skipExpands)
                    {
                        var scopedExpandInfo = columnType.ExpandInfo;
                        var qOptions = dataSourceToQueryOptionsMap.GetOrCreateQueryOptions(scopedExpandInfo.ParentDataSource as IExternalTabularDataSource);
                        retval |= qOptions.AddExpand(scopedExpandInfo, out _);
                    }
                }
            }

            return retval;
        }

        public static bool HasMetaField(this DType type)
        {
            return type.TryGetMetaField(out _);
        }

        // Fetch the meta field for this DType, if there is one.
        public static bool TryGetMetaField(this DType self, out IExternalControlType metaFieldType)
        {
            if (!self.IsAggregate ||
                !self.TryGetType(new DName(DType.MetaFieldName), out var field) ||
                !(field is IExternalControlType control) ||
                !control.ControlTemplate.IsMetaLoc)
            {
                metaFieldType = null;
                return false;
            }

            metaFieldType = control;
            return true;
        }

        public static bool ContainsDataEntityType(this DType self, DPath path, int currentDepth)
        {
            if (currentDepth < 1)
            {
                return false;
            }

            return self.GetNames(path).Any(n => n.Type.IsExpandEntity ||
                (n.Type.IsAggregate && n.Type.ContainsDataEntityType(DPath.Root, currentDepth - 1)));
        }
    }
}
