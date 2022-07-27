// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Runtime values corresponding to static values described in a <see cref="SymbolTable"/>.
    /// See <see cref="SymbolValues"/> for a mutable derived class. 
    /// </summary>
    [DebuggerDisplay("{DebugName}")]
    public abstract class ReadOnlySymbolValues : IServiceProvider
    {
        internal VersionHash _version = VersionHash.New();

        // Helper in debugging. Useful when we have multiple symbol tables chained. 
        public string DebugName { get; init; } = "(RuntimeValues)";

        // This will also mark dirty any symbol table that we handed out. 
        public void Inc()
        {
            if (_lastSnapshot != null)
            {
                _lastSnapshot.Inc();
            }

            _version.Inc();
        }

        /// <summary>
        /// Expose services to functions. For example, this can provide TimeZone information to the 
        /// Now()/TimeZoneInfo()/etc. Or current culture to the various text parsing functions. 
        /// </summary>
        /// <param name="serviceType">type of service requested.</param>
        /// <returns>Return null if service is not provided.</returns>
        public virtual object GetService(Type serviceType)
        {
            return null;
        }

        /// <summary>
        /// Get a snapshot of the current set of symbols. 
        /// </summary>
        /// <returns></returns>
        public ReadOnlySymbolTable GetSymbolTableSnapshot()
        {
            if (_lastSnapshot != null)
            {
                _lastSnapshot.Inc();
            }

            _lastSnapshot = GetSymbolTableSnapshotWorker();
            return _lastSnapshot;
        }

        private ReadOnlySymbolTable _lastSnapshot;

        protected abstract ReadOnlySymbolTable GetSymbolTableSnapshotWorker();

        public virtual bool TryGetValue(string name, out FormulaValue value)
        {
            value = null;
            return false;
        }

        public static ReadOnlySymbolValues NewFromRecord(RecordValue parameters)
        {
            var runtimeConfig = new SymbolValues();
            foreach (var kv in parameters.Fields)
            {
                runtimeConfig.Add(kv.Name, kv.Value);
            }

            return runtimeConfig;
        }
    }
}
