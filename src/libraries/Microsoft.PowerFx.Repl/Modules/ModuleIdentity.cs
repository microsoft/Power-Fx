// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.PowerFx.Repl
{
    /// <summary>
    /// A comparable handle representing module identity. 
    /// </summary>
    [DebuggerDisplay("{_value}")]
    internal struct ModuleIdentity
    {        
        private readonly string _value;

        private ModuleIdentity(string value)
        {
            _value = value;
        }

        /// <summary>
        /// Get an identify for a file. 
        /// </summary>
        /// <param name="fullPath">full path to file. File does not need to actually exist.</param>
        /// <returns>An identity.</returns>
        public static ModuleIdentity FromFile(string fullPath)
        {
            if (fullPath == null)
            {
                throw new ArgumentNullException(nameof(fullPath));
            }

            if (!Path.IsPathRooted(fullPath))
            {
                throw new ArgumentException("Path must be rooted", nameof(fullPath));
            }

            // GetFullPath will normalize ".." to a canonical representation. 
            // Path does not need to actually exist. 
            string canonicalPath = Path.GetFullPath(fullPath);

            // Lower to avoid case sensitivity.
            var value = canonicalPath.ToLowerInvariant();

            return new ModuleIdentity(value);
        }

        public override bool Equals(object obj)
        {
            if (obj is ModuleIdentity other)
            {
                return _value == other._value;
            }

            return false; 
        }
        
        public override int GetHashCode()
        {
            return _value == null ? 0 : _value.GetHashCode();
        }

        // Do not implement ToString - identity should be treated opaque. 
    }
}
