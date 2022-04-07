// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Types
{
    internal sealed class DTypeInfo
    {
        public DType DType { get; }

        /// <summary>
        /// Indicates if the DType represents a truncated schema.
        /// This would indicate a loop in a type tree or that the originating schema has a depth larger
        /// than the 'max schema depth' supported by the schema computation function.
        /// </summary>
        public bool IsTruncated { get; }

        public DTypeInfo(DType dtype, bool isTruncated)
        {
            DType = dtype;
            IsTruncated = isTruncated;
        }
    }
}
