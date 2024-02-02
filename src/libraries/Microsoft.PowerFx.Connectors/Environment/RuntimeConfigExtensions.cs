// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    public static class RuntimeConfigExtensions
    {
        public static RuntimeConfig AddRuntimeContext(this RuntimeConfig runtimeConfig, BaseRuntimeConnectorContext context)
        {
            runtimeConfig.ServiceProvider.AddService(typeof(BaseRuntimeConnectorContext), context);

            // Needed for TextToBlob which cannot get BaseRuntimeConnectorContext
            runtimeConfig.ServiceProvider.AddService(context.ResourceManager);
            return runtimeConfig;
        }
    }
}
