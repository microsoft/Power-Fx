// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;

namespace Microsoft.PowerFx.Interpreter.Localization
{
    public static class RuntimeStringResources
    {
        internal static readonly IExternalStringResources RuntimeResources = new PowerFxStringResources("Microsoft.PowerFx.Interpreter.strings.PowerFxRuntimeResources", typeof(RuntimeStringResources).Assembly);

        internal static ErrorResourceKey New(string key) => new (key, RuntimeResources);

        public static readonly ErrorResourceKey ErrDivByZero = New("ErrDivByZero");
        public static readonly ErrorResourceKey ErrInvalidCharValue = New("ErrInvalidCharValue");
        public static readonly ErrorResourceKey ErrArgumentOutOfRange = New("ErrArgumentOutOfRange");
        public static readonly ErrorResourceKey ErrRuntimeTypeMismatch = New("ErrRuntimeTypeMismatch");
        public static readonly ErrorResourceKey ErrBadLanguageCode = New("ErrBadLanguageCode");
        public static readonly ErrorResourceKey ErrNonFiniteArgument = New("ErrNonFiniteArgument");
        public static readonly ErrorResourceKey ErrNonFiniteResult = New("ErrNonFiniteResult");
        public static readonly ErrorResourceKey ErrInvalidDateTimeParsingError = New("ErrInvalidDateTimeParsingError");
        public static readonly ErrorResourceKey ErrInvalidDateTimeError = New("ErrInvalidDateTimeError");
        public static readonly ErrorResourceKey ErrInvalidBooleanFormatError = New("ErrInvalidBooleanFormatError");
        public static readonly ErrorResourceKey ErrInvalidColorFormatError = New("ErrInvalidColorFormatError");
        public static readonly ErrorResourceKey ErrUnreachableCodeError = New("ErrUnreachableCodeError");
        public static readonly ErrorResourceKey ErrNotYetImplementedFunctionError = New("ErrNotYetImplementedFunctionError");
        public static readonly ErrorResourceKey ErrNotYetImplementedUnaryOperatorError = New("ErrNotYetImplementedUnaryOperatorError");
        public static readonly ErrorResourceKey ErrInvalidChain = New("ErrInvalidChain");
        public static readonly ErrorResourceKey ErrMaxCallDepth = New("ErrMaxCallDepth");
        public static readonly ErrorResourceKey ErrRecordNotFound = New("ErrRecordNotFound");
        public static readonly ErrorResourceKey ErrOnlyPrimitiveValuesAllowed = New("ErrOnlyPrimitiveValuesAllowed");
        public static readonly ErrorResourceKey ErrCanNotConvertToNumber = New("ErrCanNotConvertToNumber");
        public static readonly ErrorResourceKey ErrStartOfWeekInvalid = New("ErrStartOfWeekInvalid");
        public static readonly ErrorResourceKey ErrRuntimeExceptionError = New("ErrRuntimeExceptionError");
        public static readonly ErrorResourceKey ErrNameIsNotValid = New("ErrNameIsNotValid");
        public static readonly ErrorResourceKey ErrMondayZeroValueNotSupported = New("ErrMondayZeroValueNotSupported");
        public static readonly ErrorResourceKey ErrAggregateArgsSameNumberOfRecords = New("ErrAggregateArgsSameNumberOfRecords");
        public static readonly ErrorResourceKey ErrInvalidArgument = New("ErrInvalidArgument");
        public static readonly ErrorResourceKey ErrUntypedObjectDoesNotImplementSetProperty = New("ErrUntypedObjectDoesNotImplementSetProperty");
        public static readonly ErrorResourceKey ErrLowerValueGreaterThanUpperValue = New("ErrLowerValueGreaterThanUpperValue");
        public static readonly ErrorResourceKey ErrUntypedObjectNotArray = New("ErrUntypedObjectNotArray");
        public static readonly ErrorResourceKey ErrNotApplicable = New("ErrNotApplicable");
        public static readonly ErrorResourceKey ErrAccessingFieldNotValidValue = New("ErrAccessingFieldNotValidValue");
        public static readonly ErrorResourceKey ErrSystemNoError = New("ErrSystemNoError");
        public static readonly ErrorResourceKey ErrSystemError = New("ErrSystemError");
        public static readonly ErrorResourceKey ErrMissingRequiredField = New("ErrMissingRequiredField");
        public static readonly ErrorResourceKey ErrCreateRecordPermissionDenied = New("ErrCreateRecordPermissionDenied");
        public static readonly ErrorResourceKey ErrUpdateRecordPermissionDenied = New("ErrUpdateRecordPermissionDenied");
        public static readonly ErrorResourceKey ErrDeleteRecordPermissionDenied = New("ErrDeleteRecordPermissionDenied");
        public static readonly ErrorResourceKey ErrColumnServerGenerated = New("ErrColumnServerGenerated");
        public static readonly ErrorResourceKey ErrRecordUpdateConflict = New("ErrRecordUpdateConflict");
        public static readonly ErrorResourceKey ErrValidationError = New("ErrValidationError");
        public static readonly ErrorResourceKey ErrColumnReadOnly = New("ErrColumnReadOnly");
        public static readonly ErrorResourceKey ErrInvalidRecord = New("ErrInvalidRecord");
        public static readonly ErrorResourceKey ErrBadLanguage = New("ErrBadLanguage");
        public static readonly ErrorResourceKey ErrRegexSyntaxError = New("ErrRegexSyntaxError");
        public static readonly ErrorResourceKey ErrInvalidFunctionUsage = New("ErrInvalidFunctionUsage");
        public static readonly ErrorResourceKey ErrFileNotFound = New("ErrFileNotFound");
        public static readonly ErrorResourceKey ErrSystemAnalysisError = New("ErrSystemAnalysisError");
        public static readonly ErrorResourceKey ErrReadRecordPermissionDenied = New("ErrReadRecordPermissionDenied");
        public static readonly ErrorResourceKey ErrOperationNotSupportedByPlayerDevice = New("ErrOperationNotSupportedByPlayerDevice");
        public static readonly ErrorResourceKey ErrInsufficientMemoryStorage = New("ErrInsufficientMemoryStorage");
        public static readonly ErrorResourceKey ErrStorageQuotaExceeded = New("ErrStorageQuotaExceeded");
        public static readonly ErrorResourceKey ErrNetworkError = New("ErrNetworkError");
        public static readonly ErrorResourceKey ErrNumericError = New("ErrNumericError");
        public static readonly ErrorResourceKey ErrTimeoutError = New("ErrTimeoutError");
        public static readonly ErrorResourceKey ErrOnlineServiceNotAvailable = New("ErrOnlineServiceNotAvailable");
        public static readonly ErrorResourceKey ErrInvalidJsonFormat = New("ErrInvalidJsonFormat");
        public static readonly ErrorResourceKey ErrCustomError = New("ErrCustomError");
        public static readonly ErrorResourceKey ErrCustomErrorArg = New("ErrCustomErrorArg");
        public static readonly ErrorResourceKey ErrReservedErrorArg = New("ErrReservedErrorArg");
        public static readonly ErrorResourceKey ErrOverflow = New("ErrOverflow");
    }
}
