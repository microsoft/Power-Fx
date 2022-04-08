// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.App.Controls
{
    internal static class PropertyRuleCategoryExtensions
    {
        internal static bool IsValid(this PropertyRuleCategory category) =>
            category >= PropertyRuleCategory.Data && category <= PropertyRuleCategory.Formulas;

        internal static bool IsBehavioral(this PropertyRuleCategory category) =>
            category == PropertyRuleCategory.Behavior || category == PropertyRuleCategory.OnDemandData;
    }
}
