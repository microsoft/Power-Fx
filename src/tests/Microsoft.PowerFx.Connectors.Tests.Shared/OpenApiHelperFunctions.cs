// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Connectors.Execution;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors.Tests
{
    internal static class OpenApiHelperFunctions
    {
        internal static OpenApiSchema SchemaInteger => new () { Type = "integer" };

        internal static OpenApiSchema SchemaNumber => new () { Type = "number" };

        internal static OpenApiSchema SchemaString => new () { Type = "string" };

        internal static OpenApiSchema SchemaBoolean => new () { Type = "boolean" };

        internal static OpenApiSchema SchemaArrayInteger => new () { Type = "array", Format = "int" };

        internal static OpenApiSchema SchemaArrayString => new () { Type = "array", Format = "string" };

        internal static OpenApiSchema SchemaArrayObject => new () { Type = "array", Format = "object" };

        internal static OpenApiSchema SchemaArrayDateTime => new () { Type = "array", Format = "date-time" };

        internal static OpenApiSchema SchemaDateTime => new () { Type = "string", Format = "date-time" };

        internal static OpenApiSchema SchemaObject(params (string PropertyName, OpenApiSchema Schema, bool Required)[] properties) => 
            new () 
            { 
                Type = "object", 
                Required = new HashSet<string>(properties.Where(p => p.Required).Select(p => p.PropertyName)), 
                Properties = properties.ToDictionary(prop => prop.PropertyName, prop => prop.Schema)
            };

        internal static RecordValue GetRecord(params (string Name, FormulaValue Value)[] properties) => FormulaValue.NewRecordFromFields(properties.Select(prop => new NamedValue(prop.Name, prop.Value)).ToArray());

        internal static TableValue GetArray(params string[] values) => FormulaValue.NewSingleColumnTable(values.Select(v => FormulaValue.New(v)));

        internal static TableValue GetArray(params int[] values) => FormulaValue.NewSingleColumnTable(values.Select(v => FormulaValue.New(v)));

        internal static TableValue GetArray(params bool[] values) => FormulaValue.NewSingleColumnTable(values.Select(v => FormulaValue.New(v)));

        internal static TableValue GetArray(params DateTime[] values) => FormulaValue.NewSingleColumnTable(values.Select(v => FormulaValue.New(v)));

        internal static TableValue GetArray(params RecordValue[] values) => FormulaValue.NewTable(values.First().Type, values);

        internal static TableValue GetTable(RecordValue recordValue) => FormulaValue.NewTable(recordValue.Type, recordValue);

        internal static async Task<string> SerializeJsonAsync(Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)> parameters, IConvertToUTC utcConverter = null, CancellationToken cancellationToken = default)
            => await SerializeAsync<OpenApiJsonSerializer>(parameters, false, utcConverter, cancellationToken);

        internal static async Task<string> SerializeUrlEncoderAsync(Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)> parameters, IConvertToUTC utcConverter = null, CancellationToken cancellationToken = default)
            => await SerializeAsync<OpenApiFormUrlEncoder>(parameters, false, utcConverter, cancellationToken);

        internal static async Task<string> SerializeAsync<T>(Dictionary<string, (OpenApiSchema Schema, FormulaValue Value)> parameters, bool schemaLessBody, IConvertToUTC utcConverter = null, CancellationToken cancellationToken = default)
            where T : FormulaValueSerializer
        {
            var jsonSerializer = (FormulaValueSerializer)Activator.CreateInstance(typeof(T), new object[] { utcConverter, schemaLessBody, cancellationToken });
            jsonSerializer.StartSerialization(null);

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    await jsonSerializer.SerializeValueAsync(parameter.Key, SwaggerSchema.New(parameter.Value.Schema), parameter.Value.Value);
                }
            }

            jsonSerializer.EndSerialization();
            return jsonSerializer.GetResult();
        }
    }
}
