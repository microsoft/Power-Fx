// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Texl.Builtins;

namespace Microsoft.PowerFx
{
    public static class ConfigExtensions
    {
        /// <summary>
        /// Enables ParseJSON function for eval.
        /// </summary>
        /// <param name="config"></param>
        [Obsolete("Use EnableJsonFunctions instead.")]
        public static void EnableParseJSONFunction(this PowerFxConfig config)
        {
            config.AddFunction(new ParseJSONFunctionImpl());
        }
    }
}
