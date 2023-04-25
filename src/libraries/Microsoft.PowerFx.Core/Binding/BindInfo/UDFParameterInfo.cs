// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Binding.BindInfo
{
    /// <summary>
    /// Lookup info for a parameters in user defined functions.
    /// </summary>
    internal sealed class UDFParameterInfo
    {
        public readonly DType Type;
        public readonly int ArgIndex;
        public readonly DName Name;

        public UDFParameterInfo(DType type, int argIndex, DName name)
        {
            Contracts.AssertValid(type);
            Contracts.AssertValid(name);
            Contracts.Assert(argIndex >= 0);

            Type = type;
            ArgIndex = argIndex;
            Name = name;
        }
    }
}
