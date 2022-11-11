// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// <see cref="TexlFunction"/> can override <see cref="TexlFunction.GetIRPreProcessors"/> to return one of these preprocessor 
    /// that IR can leverage to add preprocessing for arguments of function call.
    /// </summary>
    internal enum IRPreProcessor
    {
        None,

        BlankToZero,
        BlankToEmptyString,
        NumberTruncate,
    }
}
