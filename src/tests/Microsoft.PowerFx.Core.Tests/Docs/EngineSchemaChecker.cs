// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Microsoft.PowerFx.Core.Tests
{
    /// <summary>
    /// Check that an <see cref="Engine"/> matches the <see cref="EngineSchema"/>.
    /// </summary>
    public class EngineSchemaChecker
    {
        /// <summary>
        /// Check that the engine matches the schema. 
        /// On failure, point to a path wit the actual schema. 
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="pathInput">File path to a JSON serialization of <see cref="EngineSchema"/>. </param>
        /// <exception cref="InvalidOperationException">on failure.</exception>
        public static void Check(Engine engine, string pathInput)
        {
            EngineSchema schema;
            if (pathInput != null)
            {
                var json = File.ReadAllText(pathInput);
                schema = JsonSerializer.Deserialize<EngineSchema>(json);
            }
            else
            {
                schema = new EngineSchema();
            }

            schema.Normalize();

            var actualNames = engine.GetAllFunctionNames().ToArray();
            Array.Sort(actualNames);

            var schemaActual = new EngineSchema
            {
                FunctionNames = actualNames,
                HostObjects = schema.HostObjects // copy over 
            }.Normalize();

            if (schema.GetCompareString() != schemaActual.GetCompareString())
            {
                // Can we get a more specific message? 
                var setExpected = new HashSet<string>(schema.FunctionNames);
                setExpected.ExceptWith(schemaActual.FunctionNames); // missing

                var setActual = new HashSet<string>(schemaActual.FunctionNames);
                setActual.ExceptWith(schema.FunctionNames); // extra

                var sb = new StringBuilder();
                sb.Append("Schema different");

                if (setExpected.Count > 0)
                {
                    sb.Append("; Extra: " + string.Join(", ", setExpected.ToArray()));
                }

                if (setActual.Count > 0)
                {
                    sb.Append("; Missing: " + string.Join(",", setActual.ToArray()));
                }

                var pathTemp = Path.Combine(Path.GetTempPath(), "actual-" + Path.GetFileName(pathInput));

                var jsonActual = JsonSerializer.Serialize(schemaActual, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(pathTemp, jsonActual);

                sb.Append($"; Expected: {pathInput}. Actual: {pathTemp}");

                throw new InvalidOperationException(sb.ToString());
            }
        }
    }
}
