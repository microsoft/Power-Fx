// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx
{
    public class JsonSettings
    {
        public int MaxDepth { get; init; }

        public static JsonSettings Default => new JsonSettings() 
        { 
            MaxDepth = 40 
        };
    }
}
