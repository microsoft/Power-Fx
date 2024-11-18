// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types
{
    /// <summary>
    /// Provides information about data entity.
    /// </summary>
    internal sealed class ExpandInfo : IExpandInfo, IEquatable<ExpandInfo>
    {
        // identity = foreign table name
        // name = property name
        public ExpandInfo(string identity, string name, bool isTable, string polymorphicParent = null)
        {
            Contracts.AssertNonEmpty(identity);
            Contracts.AssertNonEmpty(name);

            Identity = identity;
            Name = name;
            IsTable = isTable;
            PolymorphicParent = polymorphicParent;
        }

        public IExpandInfo Clone()
        {
            var info = new ExpandInfo(Identity, Name, IsTable, PolymorphicParent)
            {
                ExpandPath = ExpandPath,
                ParentDataSource = ParentDataSource
            };
            return info;
        }

        public bool IsTable { get; }

        public string Name { get; }

        public string Identity { get; }

        public string PolymorphicParent { get; }

        public ExpandPath ExpandPath { get; internal set; }

        // Display name (user friendly name) of the entity.
        public IExternalDataSource ParentDataSource { get; internal set; }

        public void UpdateEntityInfo(IExternalDataSource dataSource, string relatedEntityPath)
        {
            Contracts.AssertValue(dataSource);
            Contracts.AssertValue(relatedEntityPath);

            ExpandPath = ExpandPath.CreateExpandPath(relatedEntityPath, Name);
            ParentDataSource = dataSource;
        }

        public bool Equals(ExpandInfo other) =>
            other != null &&
            Name == other.Name &&
            IsTable == other.IsTable &&
            ParentDataSource.Equals(other.ParentDataSource) &&
            ExpandPath.Equals(other.ExpandPath);

        public override bool Equals(object obj) => obj is ExpandInfo info && Equals(info);

        public override int GetHashCode() => Hashing.CombineHash(Identity.GetHashCode(), Name.GetHashCode(), IsTable.GetHashCode(), PolymorphicParent.GetHashCode(), ExpandPath.GetHashCode(), ParentDataSource.GetHashCode());

        public string ToDebugString() => $"Name={Name}, Identity={Identity}, IsTable={IsTable}";
    }
}
