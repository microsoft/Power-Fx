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
    }
}
