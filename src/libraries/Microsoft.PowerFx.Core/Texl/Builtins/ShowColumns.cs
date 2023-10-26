// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities.QueryOptions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // ShowColumns(source:*[...], name:s, name:s, ...)
    // ShowColumns(source:![...], name:s, name:s, ...)
    internal sealed class ShowColumnsFunction : ShowDropColumnsFunctionBase
    {
        public override bool AffectsDataSourceQueryOptions => true;

        public ShowColumnsFunction()
            : base(true)
        {
        }

        public override bool UpdateDataQuerySelects(CallNode callNode, TexlBinding binding, DataSourceToQueryOptionsMap dataSourceToQueryOptionsMap)
        {
            Contracts.AssertValue(callNode);
            Contracts.AssertValue(binding);

            if (!CheckArgsCount(callNode, binding))
            {
                return false;
            }

            var args = callNode.Args.Children.VerifyValue();

            var dsType = binding.GetType(args[0]);
            if (dsType.AssociatedDataSources == null)
            {
                return false;
            }

            var resultType = binding.GetType(callNode).VerifyValue();

            var retval = false;
            foreach (var typedName in resultType.GetNames(DPath.Root))
            {
                var columnType = typedName.Type;
                var columnName = typedName.Name.Value;

                Contracts.Assert(dsType.Contains(new DName(columnName)));

                retval |= dsType.AssociateDataSourcesToSelect(dataSourceToQueryOptionsMap, columnName, columnType, true);
            }

            return retval;
        }
    }
}
