// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;

namespace Microsoft.PowerFx.Interpreter.Localization
{
    internal static class RuntimeStringResources
    {
        internal static readonly IExternalStringResources RuntimeResources = new PowerFxStringResources("Microsoft.PowerFx.Interpreter.strings.PowerFxRuntimeResources", typeof(RuntimeStringResources).Assembly);

        internal static ErrorResourceKey New(string key) => new (key, RuntimeResources);

        internal static readonly ErrorResourceKey ErrDivByZero = RuntimeStringResources.New("ErrDivByZero");
        internal static readonly ErrorResourceKey ErrInvalidCharValue = RuntimeStringResources.New("ErrInvalidCharValue");
        internal static readonly ErrorResourceKey ErrArgumentOutOfRange = RuntimeStringResources.New("ErrArgumentOutOfRange");
        internal static readonly ErrorResourceKey ErrRuntimeTypeMismatch = RuntimeStringResources.New("ErrRuntimeTypeMismatch");
        internal static readonly ErrorResourceKey ErrBadLanguageCode = RuntimeStringResources.New("ErrBadLanguageCode");
        internal static readonly ErrorResourceKey ErrNonFiniteArgument = RuntimeStringResources.New("ErrNonFiniteArgument");
        internal static readonly ErrorResourceKey ErrNonFiniteResult = RuntimeStringResources.New("ErrNonFiniteResult");
        internal static readonly ErrorResourceKey ErrInvalidDateTimeParsingError = RuntimeStringResources.New("ErrInvalidDateTimeParsingError");
        internal static readonly ErrorResourceKey ErrInvalidDateTimeError = RuntimeStringResources.New("ErrInvalidDateTimeError");
        internal static readonly ErrorResourceKey ErrInvalidBooleanFormatError = RuntimeStringResources.New("ErrInvalidBooleanFormatError");
        internal static readonly ErrorResourceKey ErrInvalidColorFormatError = RuntimeStringResources.New("ErrInvalidColorFormatError");
        internal static readonly ErrorResourceKey ErrUnreachableCodeError = RuntimeStringResources.New("ErrUnreachableCodeError");
        internal static readonly ErrorResourceKey ErrNotYetImplementedError = RuntimeStringResources.New("ErrNotYetImplementedError");
        internal static readonly ErrorResourceKey ErrInvalidChain = RuntimeStringResources.New("ErrInvalidChain");
        internal static readonly ErrorResourceKey ErrMaxCallDepth = RuntimeStringResources.New("ErrMaxCallDepth");
        internal static readonly ErrorResourceKey ErrRecordNotFound = RuntimeStringResources.New("ErrRecordNotFound");
        internal static readonly ErrorResourceKey ErrOnlyPrimitiveValuesAllowed = RuntimeStringResources.New("ErrOnlyPrimitiveValuesAllowed");
        internal static readonly ErrorResourceKey ErrCanNotConvertToNumber = RuntimeStringResources.New("ErrCanNotConvertToNumber");
        internal static readonly ErrorResourceKey ErrStartOfWeekInvalid = RuntimeStringResources.New("ErrStartOfWeekInvalid");
        internal static readonly ErrorResourceKey ErrRuntimeExceptionError = RuntimeStringResources.New("ErrRuntimeExceptionError");
    }
}
