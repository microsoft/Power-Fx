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
                .Build();
            var poco = deserializer.Deserialize<ModulePoco>(txt);

            poco.Src_Filename = fullPath;
                        
            poco.Src_Formulas = GetFormulaOffset(fullPath);
            return (poco, this);
        }

        // $$$ Must be a better way to do this!  
        // Can we tie into lexer somehow? 
        private static FileLocation GetFormulaOffset(string fullPath)
        {
            var lines = File.ReadAllLines(fullPath);
            var iLine = lines.FindIndex(x => x.StartsWith("Formulas: |", StringComparison.Ordinal));

            // Find column start
            var colStart = lines[iLine + 1].FindIndex(x => !char.IsWhiteSpace(x));

            var loc = new FileLocation
            {
                Filename = fullPath,
                LineStart = iLine + 2,
                ColStart = colStart + 1
            };

            return loc;
        }
    }
}
