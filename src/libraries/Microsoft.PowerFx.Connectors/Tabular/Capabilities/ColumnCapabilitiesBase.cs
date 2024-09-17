// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.PowerFx.Connectors
{
    [JsonConverter(typeof(AbstractTypeConverter<ColumnCapabilitiesBase>))]
    public abstract class ColumnCapabilitiesBase
    {
    }
}
