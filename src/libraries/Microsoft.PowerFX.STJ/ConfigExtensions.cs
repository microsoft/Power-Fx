// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PowerFx.Functions;

namespace Microsoft.PowerFx
{
    public static class ConfigExtensions
    {
        /// <summary>
        /// Enables ParseJSON function for eval.
        /// </summary>
        /// <param name="config"></param>
        public static void EnableParseJSONFunction(this PowerFxConfig config)
        {
            PowerFxConfig.ParseJSONImpl = Library.ParseJSON;
        }
    }
}
