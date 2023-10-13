// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
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
                var pathTemp = Path.GetTempFileName();
                var jsonActual = JsonSerializer.Serialize(schemaActual, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(pathTemp, jsonActual);

                throw new InvalidOperationException(
                    $"Schema is different. Expected: {pathInput}. Actual: {pathTemp}");
            }
        }
    }
}
