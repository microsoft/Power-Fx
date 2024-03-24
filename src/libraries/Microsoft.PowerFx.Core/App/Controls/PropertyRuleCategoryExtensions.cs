// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.App.Controls
{
    public static class PropertyRuleCategoryExtensions
    {
        public static bool IsValid(this PropertyRuleCategory category) =>
            category >= PropertyRuleCategory.Data && category <= PropertyRuleCategory.Functions;

        public static bool IsBehavioral(this PropertyRuleCategory category) =>
            category == PropertyRuleCategory.Behavior || category == PropertyRuleCategory.OnDemandData;
    }
}
