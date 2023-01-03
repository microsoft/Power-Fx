// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    // This should have read-only views on all members. 
    // Assume any field is nullable. 
    public interface IRuntimeConfig
    {
        /// <summary>
        /// This should match the SymbolTable provided at bind time. 
        /// </summary>
        public ReadOnlySymbolValues Values { get; }

        /// <summary>
        /// Services. 
        /// </summary>
        public IServiceProvider Services { get; }
    }

    /// <summary>
    /// Runtime configuration for the execution of an expression.
    /// This can be reused across multiple evals - so it shouldn't have any state that expires such as a cancellation token. 
    /// </summary>
    public sealed class RuntimeConfig : IRuntimeConfig
    {
        /// <summary>
        /// This should match the SymbolTable provided at bind time. 
        /// </summary>
        public ReadOnlySymbolValues Values { get; set; }

        /// <summary>
        /// Mutable set of serivces for runtime functions and evaluation. 
        /// </summary>
        public BasicServiceProvider Services { get; set; } = new BasicServiceProvider();

        IServiceProvider IRuntimeConfig.Services => Services;

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
            AddService(runtimeCulture);
        }

        // Add services for runtime usage. This could include custom services used by a Functions,
        // or builtin services like:
        // - CultureInfo, Timezone, Clock, 
        // - Stack depth 
        // - Max memory 
        // - Logging
        public void AddService<T>(T service)
        {
            Services.AddService(typeof(T), service);
        }

        public T GetService<T>()
        {
            return (T)Services.GetService(typeof(T));
        }
    }
}
