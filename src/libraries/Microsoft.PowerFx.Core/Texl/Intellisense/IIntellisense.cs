// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    internal interface IIntellisense
    {
        /// <summary>
        /// Returns the result depending on the context.
        /// </summary>
        IIntellisenseResult Suggest(IntellisenseContext context, TexlBinding binding, Formula formula);
    }
}
