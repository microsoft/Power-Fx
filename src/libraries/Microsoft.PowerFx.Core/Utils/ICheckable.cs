// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core.Utils
{
    /// <summary>
    /// Implement this interface to add AssertValid/CheckValid validation capabilities to your class.
    /// </summary>
    public interface ICheckable
    {
        bool IsValid { get; }
    }
}
