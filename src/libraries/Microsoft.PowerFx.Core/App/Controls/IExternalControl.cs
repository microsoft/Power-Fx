// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.App.Controls
{
    internal interface IExternalControl : IExternalEntity
    {
        IExternalControlTemplate Template { get; }

        bool IsComponentControl { get; }

        IExternalControl TopParentOrSelf { get; }

        string DisplayName { get; }

        bool IsReplicable { get; }

        bool IsAppInfoControl { get; }

        DType ThisItemType { get; }

        bool IsAppGlobalControl { get; }

        bool IsCommandComponentInstance { get; }

        RecordType GetControlType(bool calculateAugmentedExpandoType, bool isDataLimited);

        bool IsDescendentOf(IExternalControl controlInfo);

        IExternalRule GetRule(string propertyInvariantName);

        bool TryGetRule(string dName, out IExternalRule rule);
    }
}
