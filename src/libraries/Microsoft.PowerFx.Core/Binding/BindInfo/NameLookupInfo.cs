﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Entities;
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
        public readonly bool IsAsync;

        // Optional data associated with a name. May be null.
        public readonly object Data;

        public NameLookupInfo(BindKind kind, DType type, DPath path, int upCount, object data = null, DName displayName = default, bool isAsync = false)
        {
            Contracts.Assert(kind >= BindKind.Min && kind < BindKind.Lim);
            Contracts.Assert(upCount >= 0);
            Contracts.AssertValueOrNull(data);

            Kind = kind;
            Type = type;
            Path = path;
            UpCount = upCount;
            Data = data;
            DisplayName = displayName;

            // Any connectedDataSourceInfo or option set or view needs to be accessed asynchronously to allow data to be loaded.
            IsAsync = Data is IExternalTabularDataSource || Kind == BindKind.OptionSet || Kind == BindKind.View || isAsync;
        }
    }
}
