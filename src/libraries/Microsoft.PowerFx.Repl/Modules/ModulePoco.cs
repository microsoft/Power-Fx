// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Repl
{
    /// <summary>
    /// The yaml representation of a module. 
    /// Properties with 'Src_' prefix are set by deserializer and not part of the yaml file contents.
    /// </summary>
    internal class ModulePoco
    {
        /// <summary>
        /// Full path of file this was loaded from. 
        /// This is set by the deserializer. 
        /// </summary>
        public string Src_Filename { get; set; }

        public ModuleIdentity GetIdentity()
        {
            return ModuleIdentity.FromFile(Src_Filename);
        }

        /// <summary>
        /// Contain Power Fx UDF declarations. 
        /// </summary>
        public StringWithSource Formulas { get; set; }

        /// <summary>
        /// Set of modules that this depends on. 
        /// </summary>
        public ImportPoco[] Imports { get; set; }
    }

    internal class ImportPoco
    {
        /// <summary>
        /// File path to import from. "foo.fx.yml" or ".\path\foo.fx.yml".
        /// </summary>
        public string File { get; set; }

        // identifier resolved against the host. 
        public string Host { get; set; }
    }
}
