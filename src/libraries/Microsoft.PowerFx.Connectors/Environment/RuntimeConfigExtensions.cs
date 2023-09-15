using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Connectors
{
    public static class RuntimeConfigExtensions
    {
        public static RuntimeConfig AddRuntimeContext(this RuntimeConfig runtimeConfig, BaseRuntimeConnectorContext context)
        {
            runtimeConfig.ServiceProvider.AddService(typeof(BaseRuntimeConnectorContext), context);
            return runtimeConfig;
        }
    }
}
