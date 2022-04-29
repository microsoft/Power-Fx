// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Functions.FunctionArgValidators
{
    internal interface IArgValidator<T>
    {
        bool TryGetValidValue(TexlNode argNode, TexlBinding binding, out T value);
    }
}
