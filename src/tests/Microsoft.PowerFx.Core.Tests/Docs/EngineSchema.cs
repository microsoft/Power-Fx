// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Microsoft.PowerFx.Core.Tests
{
    /// <summary>
    /// Describe a SKU's capabilities.
    /// Used for documentation purposes.
    /// </summary>
    public class EngineSchema
    {
        /// <summary>
        /// Power fx version that this file was generated against. 
        /// </summary>
        public string MinVersion { get; set; }

        /// <summary>
        /// List of host objects (like User, App, Environment, etc) that this engine supports.
        /// </summary>
        public string[] HostObjects { get; set; }

        /// <summary>
        /// list of function names this engine supports. 
        /// </summary>
        public string[] FunctionNames { get; set; }

        /// <summary>
        /// Normalize this structure.
        /// </summary>
        /// <returns>this.</returns>
        public EngineSchema Normalize()
        {
            if (this.MinVersion == null)
            {
                this.MinVersion = Engine.AssemblyVersion;
            }

            if (this.HostObjects == null) 
            {
                this.HostObjects = new string[0];
            }

            if (this.FunctionNames == null)
            {
                this.FunctionNames = new string[0];
            }

            Array.Sort(this.FunctionNames);
            Array.Sort(this.HostObjects);

            return this;
        }

        // Get a canonical string representation to use for comparing.
        internal string GetCompareString()
        {
            // MinVersion is not part of the comparison. 

            return string.Join(",", HostObjects) + ";" + string.Join(",", FunctionNames);
        }
    }
}
