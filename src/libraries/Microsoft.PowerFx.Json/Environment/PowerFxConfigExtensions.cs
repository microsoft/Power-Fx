// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Texl.Builtins;

namespace Microsoft.PowerFx
{
    [ThreadSafeImmutable]
    public static class JsonConfigExtensions
    {
        /// <summary>
        /// Adds JSON functions: ParseJSON and JSON.
        /// </summary>
        /// 
        /// <param name="config">Config to add the functions to.</param>
        public static void EnableJsonFunctions(this PowerFxConfig config)
        {
            config.AddFunction(new ParseJSONFunctionImpl());
            config.AddFunction(new JsonFunctionImpl());
            config.AddFunction(new AsTypeFunction_UOImpl());
            config.AddFunction(new IsTypeFunction_UOImpl());
            config.AddFunction(new TypedParseJSONFunctionImpl());
        }

        public static void EnableJsonFunctions(this PowerFxConfig config, JsonSettings settings)
        {
            config.AddFunction(new ParseJSONFunctionImpl());
            config.AddFunction(new JsonFunctionImpl(settings));
            config.AddFunction(new AsTypeFunction_UOImpl());
            config.AddFunction(new IsTypeFunction_UOImpl());
            config.AddFunction(new TypedParseJSONFunctionImpl());
        }
    }
}
