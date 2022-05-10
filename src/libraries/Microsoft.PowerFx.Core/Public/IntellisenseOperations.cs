// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// Various IntelliSense-like operations.
    /// </summary>
    public class IntellisenseOperations
    {
        private readonly CheckResult _checkResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntellisenseOperations"/> class.
        /// </summary>
        /// <param name="result"></param>
        public IntellisenseOperations(CheckResult result)
        {
            _checkResult = result;
        }
    }
}
