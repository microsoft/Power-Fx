// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Runtime configuration for the execution of an expression.
    /// This can be reused across multiple evals - so it shouldn't have any state that expires such as a cancellation token. 
    /// </summary>
    public sealed class RuntimeConfig
    {
        /// <summary>
        /// This should match the SymbolTable provided at bind time. 
        /// </summary>
        public ReadOnlySymbolValues Values { get; set; }

        public BasicServiceProvider Services { get; set; } = new BasicServiceProvider();

        // Other services:
        // CultureInfo, Timezone, Clock, 
        // Stack depth 
        // Max memory 
        // Logging
        // 

        // $$$ Implicit conversion operator?
        public RuntimeConfig()
        {
        }

        public RuntimeConfig(ReadOnlySymbolValues values)
        {
            this.Values = values;
        }

        public RuntimeConfig(ReadOnlySymbolValues values, CultureInfo runtimeCulture)
        {
            this.Values = values;
            AddService(runtimeCulture); // $$$ Nom utates the services... which could be shared. 
        }

        public void AddService<T>(T service)
        {
            Services.AddService(typeof(T), service);
        }

        public T GetService<T>()
        {
            return (T) Services.GetService(typeof(T));
        }
    }

    // $$$
    // Use existing?
    // https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.serviceprovider?view=dotnet-plat-ext-7.0 ? 
    public sealed class BasicServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _inner;


        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public BasicServiceProvider() : this(null)
        {
        }

        // Chain to an inner service. 
        public BasicServiceProvider(IServiceProvider inner)
        {
            _inner = inner;
        }

        public void AddService(Type serviceType, object service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            _services[serviceType] = service;
        }


        // Null if service is missing 
        public object GetService(Type serviceType)
        {
            if (!_services.TryGetValue(serviceType, out var service))
            {
                return _inner?.GetService(serviceType);
            }
            return service;
        }
    }
}
