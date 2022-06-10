// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.OpenApi.Models;

namespace Microsoft.PowerFx.Connectors
{
    internal enum FxParameterLocation
    {
        Query,
        Header,
        Path,
        Cookie,
        Body
    }

    internal static partial class FxExtentions
    {
        public static FxParameterLocation? ToFxParameterLocation(this ParameterLocation? location)
        {
            return location switch
            {
                null => null,
                ParameterLocation.Header => FxParameterLocation.Header,
                ParameterLocation.Query => FxParameterLocation.Query,
                ParameterLocation.Path => FxParameterLocation.Path,
                ParameterLocation.Cookie => FxParameterLocation.Cookie,
                _ => throw new NotImplementedException("Unknown location")
            };
        }
    }
}
