// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx
{
    public interface IPostCheckErrorHandler
    {
        public IEnumerable<ExpressionError> Process(CheckResult check);
    }
}
