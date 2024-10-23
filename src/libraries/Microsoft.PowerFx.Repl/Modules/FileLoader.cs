// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        // Root directory that we load from.
        private readonly string _root;

        public FileLoader(string root)
        {
            if (!Path.IsPathRooted(root))
            {
                throw new ArgumentException($"Path must be rooted: {root}", nameof(root));
            }

            _root = root;
        }

        public async Task<(ModulePoco, IFileLoader)> LoadAsync(string name)
        {
            name = name.Trim();
            var fullPath = Path.Combine(_root, name);
            
            var txt = File.ReadAllText(fullPath);

            // Deserialize.
            var deserializer = new DeserializerBuilder()                
                .WithTypeConverter(new StringWithSourceConverter(fullPath, txt))
                .Build();
            var modulePoco = deserializer.Deserialize<ModulePoco>(txt);

            modulePoco.Src_Filename = fullPath;

            return (modulePoco, this);
        }
    }
}
