// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Entities;

namespace Microsoft.PowerFx.Core.Types
{
    /// <summary>
    /// Information about polymorphic entity type, generates/stores ExpandInfo for each of its target casts.
    /// </summary>
    internal interface IPolymorphicInfo
    {
        string[] TargetTables { get; }

        string[] TargetFields { get; }

        bool IsTable { get; }

        string Name { get; }

        IExternalDataSource ParentDataSource { get; }

        public IEnumerable<IExpandInfo> Expands { get; }

        public IExpandInfo TryGetExpandInfo(string targetTable);

        IPolymorphicInfo Clone();

        string ToDebugString();
    }
}