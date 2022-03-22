// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Binding.BindInfo
{
    /// <summary>
    /// Temporary name information used by the Binder---Document lookup handshake/mechanism.
    /// </summary>
    internal struct NameLookupInfo
    {
        public readonly BindKind Kind;
        public readonly DPath Path;
        public readonly int UpCount;
        public readonly DType Type;

        /// <summary>
        /// Some resolved objects may have a display name associated with them. If this is non-default,
        /// it has the display name of the object in Data. 
        /// </summary>
        public readonly DName DisplayName;
        public readonly DName LogicalName;
        public readonly bool IsAsync;

        // Optional data associated with a name. May be null.
        public readonly object Data;

        public NameLookupInfo(BindKind kind, DType type, DPath path, int upCount, object data = null, DName logicalName = default, bool isAsync = default)
        public NameLookupInfo(BindKind kind, DType type, DPath path, int upCount, object data = null, DName displayName = default)
        {
            Contracts.Assert(kind >= BindKind.Min && kind < BindKind.Lim);
            Contracts.Assert(upCount >= 0);
            Contracts.AssertValueOrNull(data);

            Kind = kind;
            Type = type;
            Path = path;
            UpCount = upCount;
            Data = data;
            LogicalName = logicalName;
            IsAsync = isAsync;
            DisplayName = displayName;
        }
    }
}
