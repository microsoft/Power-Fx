// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx
{
    internal static class FeatureExtensions
    {
        internal static bool HasTableSyntaxDoesntWrapRecords(this Features features) => features.HasFlag(Features.TableSyntaxDoesntWrapRecords);

        internal static bool UsesPowerFxV1CompatibilityRules(this Features features) => features.HasFlag(Features.PowerFxV1Compatibility);
    }
}
