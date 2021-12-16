// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Logging.Trackers
{
    internal enum DelegationStatus
    {
        DelegationSuccessful,
        BinaryOpNoSupported,
        DataSourceNotDelegatable,
        UndelegatableFunction,
        AsyncPredicate,
        BinaryOpNotSupportedByTable,
        UnaryOpNotSupportedByTable,
        ImpureNode,
        NoDelSupportByColumn,
        UnSupportedSortArg,
        AsyncSortOrder,
        SortOrderNotSupportedByColumn,
        NotANumberArgType,
        InvalidArgType,
        UnSupportedRowScopedDottedNameNode,
        UnSupportedDistinctArg
    }
}
