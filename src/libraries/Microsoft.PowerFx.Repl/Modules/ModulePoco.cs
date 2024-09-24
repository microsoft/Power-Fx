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

        /// <summary>
        /// Contain Power Fx UDF declarations. 
        /// </summary>
        public string Formulas { get; set; }

        /// <summary>
        /// Source location of the "Formulas" property's content within the overall yaml file. 
        /// This is set by the deserializer and not part of the yaml file contents. 
        /// This is used for error reporting.
        /// </summary>
        public FileLocation Src_Formulas { get; set; }

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
