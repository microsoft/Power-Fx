// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.App.Controls
{
    internal static class PropertyRuleCategoryHelper
    {
        public static bool IsValidPropertyRuleCategory(string category)
        {
            return Enum.TryParse<PropertyRuleCategory>(category, ignoreCase: true, result: out _);
        }

        public static bool TryParsePropertyCategory(string category, out PropertyRuleCategory categoryEnum)
        {
            Contracts.CheckNonEmpty(category, "category");

            // Enum.TryParse uses a bunch of reflection and boxing. If this becomes an issue, we can
            // use plain-old switch statement.
            return Enum.TryParse(category, ignoreCase: true, result: out categoryEnum);
        }
    }
}
