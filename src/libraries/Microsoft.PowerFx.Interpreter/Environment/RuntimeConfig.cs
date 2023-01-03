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

        // $$$ Can this just be a IServiceProvider? But then how do we add to it?
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
            Values = values;
        }

        public RuntimeConfig(ReadOnlySymbolValues values, CultureInfo runtimeCulture)
        {
            Values = values;
            AddService(runtimeCulture); // $$$ Nom utates the services... which could be shared. 
        }

        public void AddService<T>(T service)
        {
            Services.AddService(typeof(T), service);
        }

        public T GetService<T>()
        {
            return (T)Services.GetService(typeof(T));
        }
    }

    // $$$
    // Use existing?
    // https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.serviceprovider?view=dotnet-plat-ext-7.0 ? 
    public sealed class BasicServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider[] _inners;

        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public BasicServiceProvider() 
            : this(null)
        {
        }

        // Chain to an inner service. 
        public BasicServiceProvider(params IServiceProvider[] inners)
        {
            _inners = (inners?.Length == 0) ? null : inners;
        }

        public void AddService<T>(T service)
        {
            AddService(typeof(T), service);
        }

        public void AddService(Type serviceType, object service)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            _services[serviceType] = service ?? throw new ArgumentNullException(nameof(service));
        }

        // Null if service is missing 
        public object GetService(Type serviceType)
        {
            if (!_services.TryGetValue(serviceType, out var service))
            {
                if (_inners != null)
                {
                    foreach (var inner in _inners)
                    {
                        service = inner.GetService(serviceType);
                        if (service != null)
                        {
                            return service;
                        }
                    }
                }

                return null;
            }

            return service;
        }
    }
}
