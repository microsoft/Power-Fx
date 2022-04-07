// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Functions.FunctionArgValidators
{
    internal static class ArgValidators
    {
        public static readonly SortOrderValidator SortOrderValidator = new SortOrderValidator();
        public static readonly DelegatableDataSourceInfoValidator DelegatableDataSourceInfoValidator = new DelegatableDataSourceInfoValidator();
        public static readonly DataSourceArgNodeValidator DataSourceArgNodeValidator = new DataSourceArgNodeValidator();
        public static readonly EntityArgNodeValidator EntityArgNodeValidator = new EntityArgNodeValidator();
    }
}
