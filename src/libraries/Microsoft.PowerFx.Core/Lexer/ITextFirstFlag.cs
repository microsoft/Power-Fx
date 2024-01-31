// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// Indicates that the object is in active TextFirst context.
    /// </summary>
    public interface ITextFirstFlag
    {
        bool IsTextFirst { get; }
    }
}
