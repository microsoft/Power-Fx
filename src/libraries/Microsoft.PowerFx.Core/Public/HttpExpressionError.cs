// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Error message. This could be a compile time error from parsing or binding, 
    /// or it could be a runtime error wrapped in a <see cref="ErrorValue"/>.
    /// </summary>
    public class HttpExpressionError : ExpressionError
    {
        public int StatusCode { get; }

        public HttpExpressionError(int statusCode)
            : base()
        {
            StatusCode = statusCode;
        }
    }
}
