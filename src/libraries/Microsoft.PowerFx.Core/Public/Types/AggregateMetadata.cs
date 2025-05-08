// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public class AggregateMetadata
    {
        public IEnumerable<SensitivityLabelInfo> SensitivityLabels { get; internal init; }
    }

    public class SensitivityLabelInfo
    {
        public string SensitivityLabelId { get;  }

        public string Name { get; }

        public string DisplayName { get; }

        public string Tooltip { get; }

        public int Priority { get; }

        public string Color { get; }

        public bool IsEncrypted { get; }

        public bool IsEnabled { get; }

        public bool IsParent { get; }

        public SensitivityLabelInfo(string id, string name, string displayName, string tooltip, int priority, string color, bool isEncrypted, bool isEnabled, bool isParent)
        {
            SensitivityLabelId = id;
            Name = name;
            DisplayName = displayName;
            Tooltip = tooltip;
            Priority = priority;
            Color = color;
            IsEncrypted = isEncrypted;
            IsEnabled = isEnabled;
            IsParent = isParent;
        }
    }
}
