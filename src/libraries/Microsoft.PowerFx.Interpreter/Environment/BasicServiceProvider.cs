// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerFx
{    
    /// <summary>
    /// Trivial <see cref="IServiceProvider"/> implementation that allows chaining and composition. 
    /// </summary>
    public sealed class BasicServiceProvider : IServiceProvider
    {
        // Chain to list of inner service providers. 
        private readonly IServiceProvider[] _inners;

        // services we implement. 
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public BasicServiceProvider()
            : this(null)
        {
        }

        // Chain to an inner service. 
        // Must always create a new copy since the caller can now AddServices to this. 
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

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (!serviceType.IsAssignableFrom(service.GetType()))
            {
                throw new InvalidOperationException($"Service instance {service.GetType()} must match service type {serviceType.GetType()}.");
            }

            _services[serviceType] = service;
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
                        service = inner?.GetService(serviceType);
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
