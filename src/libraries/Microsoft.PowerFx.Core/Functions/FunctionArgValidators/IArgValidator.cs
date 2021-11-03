// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Syntax.Nodes;

namespace Microsoft.PowerFx.Core.Functions.FunctionArgValidators
{
    internal interface IArgValidator<T>
    {
        bool TryGetValidValue(TexlNode argNode, TexlBinding binding, out T value);
    }
}
