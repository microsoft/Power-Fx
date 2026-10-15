// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Microsoft.PowerFx.Repl
{
    internal class FileLoader : IFileLoader
    {
        private static readonly StringComparison PathComparison =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        // Root directory that we load from.
        private readonly string _root;

        // Normalized root directory, ending with a directory separator.
        private readonly string _rootPrefix;

        public FileLoader(string root)
        {
            if (!Path.IsPathRooted(root))
            {
                throw new ArgumentException($"Path must be rooted: {root}", nameof(root));
            }

            _root = root;

            var fullRoot = Path.GetFullPath(root);
            var last = fullRoot[fullRoot.Length - 1];
            _rootPrefix = (last == Path.DirectorySeparatorChar || last == Path.AltDirectorySeparatorChar) ?
                fullRoot :
                fullRoot + Path.DirectorySeparatorChar;
        }

        public async Task<(ModulePoco, IFileLoader)> LoadAsync(string name)
        {
            name = name.Trim();
            var fullPath = ResolvePath(name);
            
            var txt = File.ReadAllText(fullPath);

            // Deserialize.
            var deserializer = new DeserializerBuilder()                
                .WithTypeConverter(new StringWithSourceConverter(fullPath, txt))
                .Build();
            var modulePoco = deserializer.Deserialize<ModulePoco>(txt);

            modulePoco.Src_Filename = fullPath;

            return (modulePoco, this);
        }

        // Resolve a module name to a full path. Module files must be within the root directory.
        internal string ResolvePath(string name)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_root, name));

            if (!fullPath.StartsWith(_rootPrefix, PathComparison))
            {
                throw new InvalidOperationException($"Module '{name}' is outside the module directory.");
            }

            return fullPath;
        }
    }
}
