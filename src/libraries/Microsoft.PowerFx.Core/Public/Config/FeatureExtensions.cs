// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx
{
    internal static class FeatureExtensions
    {
        public static bool HasTableSyntaxDoesntWrapRecords(this Feature feature) => feature.HasFlag(Feature.TableSyntaxDoesntWrapRecords);
    }
}
