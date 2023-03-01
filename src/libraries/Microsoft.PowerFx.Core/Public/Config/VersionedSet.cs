// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx
{
    public class VersionedSet
    {
        // Changed on each update. 
        // Host can use to ensure that a symbol table wasn't mutated on us.                 
        private protected VersionHash _version = VersionHash.New();

        /// <summary>
        /// This can be compared to determine if the symbol table was mutated during an operation. 
        /// </summary>
        internal virtual VersionHash VersionHash => _version;

        /// <summary>
        /// Notify the symbol table has changed. 
        /// </summary>
        public void NotifyChange()
        {
            _version.UpdateValue();
        }
    }
}
