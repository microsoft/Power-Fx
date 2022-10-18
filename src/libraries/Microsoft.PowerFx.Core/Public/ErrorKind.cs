// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Indicates the error on a record in a connected data source. 
    /// This must be kept in sync with the document enum "ErrorKind".
    /// </summary>
    public enum ErrorKind
    {
        None = 0,
        Sync = 1,
        MissingRequired = 2,
        CreatePermission = 3,
        EditPermission = 4,
        DeletePermission = 5,
        Conflict = 6,
        NotFound = 7,
        ConstraintViolated = 8,
        GeneratedValue = 9,
        ReadOnlyValue = 10,
        Validation = 11,
        Unknown = 12,
        Div0 = 13,
        BadLanguageCode = 14,
        BadRegex = 15,
        InvalidFunctionUsage = 16,
        FileNotFound = 17,
        AnalysisError = 18,
        ReadPermission = 19,
        NotSupported = 20,
        InsufficientMemory = 21,
        QuotaExceeded = 22,
        Network = 23,
        Numeric = 24,
        InvalidArgument = 25,
        Internal = 26,
        Custom = 1000
    }
}
