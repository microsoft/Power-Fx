// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Types
{
    internal struct ExpandPath
    {
        internal const char PathSeperator = '/';

        private ExpandPath(string relatedEntityPath, string entityName)
        {
            Contracts.AssertValue(relatedEntityPath);
            Contracts.AssertNonEmpty(entityName);

            RelatedEntityPath = relatedEntityPath;
            EntityName = entityName;
        }

        public readonly string RelatedEntityPath;
        public readonly string EntityName;

        public static ExpandPath CreateExpandPath(string relatedEntityPath, string entityName)
        {
            Contracts.AssertValue(relatedEntityPath);
            Contracts.AssertNonEmpty(entityName);

            relatedEntityPath = relatedEntityPath.TrimEnd(PathSeperator);
            return new ExpandPath(relatedEntityPath, entityName);
        }

        public static bool operator ==(ExpandPath lhsPath, ExpandPath rhsPath)
        {
            return lhsPath.ToString() == rhsPath.ToString();
        }

        public static bool operator !=(ExpandPath lhsPath, ExpandPath rhsPath)
        {
            return lhsPath.ToString() != rhsPath.ToString();
        }

        public bool Equals(ExpandPath path)
        {
            return this == path;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            Contracts.AssertValueOrNull(obj);

            if (!(obj is ExpandPath))
                return false;

            return this == (ExpandPath)obj;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(RelatedEntityPath))
                return EntityName;

            return RelatedEntityPath + PathSeperator + EntityName;
        }
    }
}
