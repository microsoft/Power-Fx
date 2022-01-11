// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.App.Controls
{
    internal interface IExternalControl : IExternalEntity
    {
        bool IsDataComponentDefinition { get; }

        bool IsDataComponentInstance { get; }

        IExternalControlTemplate Template { get; }

        bool IsComponentControl { get; }

        IExternalControl TopParentOrSelf { get; }

        string DisplayName { get; }

        bool IsReplicable { get; }

        bool IsAppInfoControl { get; }

        DType ThisItemType { get; }

        bool IsAppGlobalControl { get; }

        string UniqueId { get; }

        bool IsComponentInstance { get; }

        bool IsComponentDefinition { get; }

        bool IsCommandComponentInstance { get; }

        IExternalControlType GetControlDType();

        IExternalControlType GetControlDType(bool calculateAugmentedExpandoType, bool isDataLimited);

        bool IsDescendentOf(IExternalControl controlInfo);

        IExternalRule GetRule(string propertyInvariantName);

        bool TryGetRule(string dName, out IExternalRule rule);
    }
}