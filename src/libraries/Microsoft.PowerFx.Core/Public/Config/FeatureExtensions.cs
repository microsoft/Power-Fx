// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx
{
    internal static class FeatureExtensions
    {
        internal static bool HasTableSyntaxDoesntWrapRecords(this Features feature) => feature.HasFlag(Features.TableSyntaxDoesntWrapRecords);
    }
}
