// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Version hash. Can tell if a value has changed.  
    /// The only thing this can be used for is to tell if a snapshot has changed. 
    /// A version stamp should only be compared to a previous version of the same stamp. 
    /// </summary>
    [DebuggerDisplay("Version:{_value}")]
    internal struct VersionHash
    {
        private int _value;

        private VersionHash(int value)
        {
            _value = value;
        }

        private static int _hashStarter;

        public static VersionHash New()
        {
            // Give psuedo-random seed.
            // This can help protect against 2 objects accidentally appearing the same.    
            var x = Interlocked.Increment(ref _hashStarter);
            var value = x * 7919; // will wrap on overflow

            return new VersionHash(value);
        }

        /// <summary>
        /// Note that object has changed. 
        /// </summary>
        public void UpdateValue()
        {
            _value++;
        }

        public static VersionHash Combine(IEnumerable<VersionHash> versionHashes)
        {
            int versionHash = 0;

            foreach (VersionHash vh in versionHashes)
            {
                versionHash = HashCombine(versionHash, vh._value);
            }

            return new VersionHash(versionHash);
        }

        internal static int HashCombine(int a, int b)
        {
            return (a * -1521134295) + b;
        }

        public static bool operator ==(VersionHash a, VersionHash b) => a._value == b._value;

        public static bool operator !=(VersionHash a, VersionHash b) => a._value != b._value;

        public override bool Equals(object obj)
        {
            if (obj is VersionHash v)
            {
                return v._value == _value;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _value;
        }
    }
}
