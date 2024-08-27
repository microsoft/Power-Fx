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
    }
}
