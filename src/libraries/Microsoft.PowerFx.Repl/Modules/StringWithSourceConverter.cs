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
    /// <summary>
    /// Yaml converter to parse a <see cref="StringWithSource"/> and tag 
    /// it with the source location. 
    /// </summary>
    internal class StringWithSourceConverter : IYamlTypeConverter
    {
        private readonly string _filename;
        private readonly string[] _lines; // to infer indent level.

        /// <summary>
        /// Initializes a new instance of the <see cref="StringWithSourceConverter"/> class.
        /// </summary>
        /// <param name="filename">filename to tag with.</param>
        /// <param name="contents">contents of the file - this is used to infer indenting depth that can't be gotten from th yaml parser.</param>
        public StringWithSourceConverter(string filename, string contents)
        {
            _filename = filename;

            _lines = contents.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        }

        public bool Accepts(Type type)
        {
            return type == typeof(StringWithSource);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var val = parser.Consume<Scalar>();

            // Empty case 
            if (val.Style == ScalarStyle.Plain && 
                val.Value.Length == 0)
            {
                return new StringWithSource
                {
                    Value = null, // Preserve same semantics as String deserialization.
                    Location = new FileLocation
                    {
                        Filename = _filename,
                        LineStart = val.Start.Line,
                        ColStart = val.Start.Column
                    }
                };
            }

            // https://stackoverflow.com/questions/3790454/how-do-i-break-a-string-in-yaml-over-multiple-lines
            // Yaml has many multi-line encodings. Fortunately, we can determine this based on ScalarStyle 
            // We want to force | , which is Literal. This means the fx is copy & paste into yaml, with an indent level
            // and no other mutation.  
            //
            // This will prevent all other forms, including:
            // > is "fold" which will remove newlines. 
            // "" is double quotes. 
            // plain where the string is single-line embedded.

            if (val.Style != ScalarStyle.Literal)
            {
                // This is important if we want to preserve character index mapping. 
                throw new InvalidOperationException($"must be literal string encoding at {val.Start.Line}");
            }

            int line1PropertyName = val.Start.Line; // 1-based Line that the "Formulas" property is on. Content starts on next line. 

            // YamlParser does not tell us indent level. Must discern it. 
            var line = _lines[line1PropertyName];
            var colStart0 = line.FindIndex(x => !char.IsWhiteSpace(x));

            return new StringWithSource
            {
                Value = val.Value,
                Location = new FileLocation
                {
                    Filename = _filename,
                    LineStart = line1PropertyName + 1,
                    ColStart = colStart0 + 1
                }
            };
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            // Only reads. 
            throw new NotImplementedException();
        }
    } // end class 
}
