// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerFx.TexlFunctionExporter
{
    // Internal as YamlTexlFunction's constructor uses TexlFunction which is internal
    internal class TexlYamlComparer : YamlComparer<YamlTexlFunction>
    {
        internal override string FilePattern => "Texl_*.yaml";

        internal override string CategorySuffix => "Texl";

        internal TexlYamlComparer(string category, string referenceRoot, string currentRoot, List<ConnectorStat> connectorStats, List<FunctionStat> functionStats, Action<string> log)
            : base(category, referenceRoot, currentRoot, connectorStats, functionStats, log)
        {
        }
    }
}
