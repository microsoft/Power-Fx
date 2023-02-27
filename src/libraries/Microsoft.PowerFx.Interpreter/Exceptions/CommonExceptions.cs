// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Interpreter.Exceptions
{
    internal class CommonExceptions
    {
        internal static CustomFunctionErrorException RuntimeMisMatch => new (message: "Runtime type mismatch", kind: ErrorKind.InvalidArgument);
    }
}
