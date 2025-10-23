// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class CopilotFunctionImpl : CopilotFunction, IAsyncTexlFunction5
    {
        public async Task<FormulaValue> InvokeAsync(
            IServiceProvider runtimeServiceProvider,
            FormulaType irContext,
            FormulaValue[] args,
            CancellationToken cancellationToken)
        {
            // Resolve LLM host
            if (runtimeServiceProvider.GetService(typeof(ICopilotService)) is not ICopilotService chatService)
            {
                throw new InvalidOperationException("Copilot service was not added in service provider.");
            }

            // --- Parse arguments ---
            // Copilot(prompt:s)

            if (args[0] is BlankValue || args[0] is ErrorValue)
            {
                return args[0];
            }

            var prompt = ((StringValue)args[0]).Value;

            // Copilot(prompt:s, context:FV)
            FormulaValue context = null;
            if (args.Length >= 2)
            {
                if (args[1] is ErrorValue)
                {
                    return args[1]; // propagate error
                }
                else
                {
                    context = args[1];
                }
            }

            // Copilot(prompt:s, context:FV, schema:s)
            string schemaStr = null;
            if (args.Length >= 3)
            {
                if (args[2] is ErrorValue)
                {
                    return args[2]; // propagate error
                }
                else if (args[2] is BlankValue)
                {
                    schemaStr = string.Empty;
                }
                else
                {
                    schemaStr = ((StringValue)args[2]).Value;
                }
            }

            // Build full prompt with helper
            var fullPrompt = GeneratePrompt(runtimeServiceProvider, prompt, context, schemaStr, cancellationToken);

            if (fullPrompt is ErrorValue ev)
            {
                return ev; // propagate error from prompt generation
            }

            // Call host LLM
            string raw;
            try
            {
                raw = await chatService.AskTextAsync(((StringValue)fullPrompt).Value, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw; // honor cancellation
            }
            catch (Exception ex)
            {
                return CreateError(irContext, $"Copilot call failed: {ex.GetType().Name} {ex.Message}", ErrorKind.Internal);
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                return CreateError(irContext, "Copilot returned an empty response.", ErrorKind.InvalidArgument);
            }

            // If schema provided: parse JSON -> FormulaValue; else return string
            if (!string.IsNullOrWhiteSpace(schemaStr))
            {
                var cleaned = StripCodeFences(raw);

                // Use your helper to produce a FormulaValue; it already reports JSON issues as ErrorValue
                var fv = FormulaValueJSON.FromJson(cleaned, formulaType: irContext, numberIsFloat: false);
                return fv;
            }

            return FormulaValue.New(raw);
        }

        private static ErrorValue CreateError(FormulaType irContext, string message, ErrorKind kind)
        {
            var error = FormulaValue.NewError(new ExpressionError()
            {
                Message = message,
                Kind = kind
            });

            return error;
        }

        private static string StripCodeFences(string s)
        {
            if (s == null)
            {
                return string.Empty;
            }

            string trimmed = s.Trim();
            if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                return trimmed;
            }

            // Remove leading fence (``` or ```json)
            int firstNewline = trimmed.IndexOf('\n');
            if (firstNewline < 0)
            {
                return trimmed; // nothing to strip safely
            }

            trimmed = trimmed.Substring(firstNewline + 1);

            // Remove trailing ```
            int lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0)
            {
                trimmed = trimmed.Substring(0, lastFence).Trim();
            }

            return trimmed;
        }

        /// <summary>
        /// Builds the full prompt:
        ///   prompt
        ///   + " using the following context: {contextJson}" (if context provided)
        ///   + JSON-only instruction with schema (if schema provided)
        /// 
        /// Context is converted via the same visitor used by the JSON() function,
        /// honoring timezone, cancellation, and size/depth limits.
        /// </summary>
        private static FormulaValue GeneratePrompt(
            IServiceProvider sp,
            string prompt,
            FormulaValue context,
            string schemaStr,
            CancellationToken ct)
        {
            string finalPrompt = prompt;

            // 1) If we have context, stringify it using the existing JSON writer pipeline
            if (context != null && context is not BlankValue)
            {
                var ctxJson = FormulaValueToJsonString(sp, context, ct);
                if (ctxJson is StringValue ctxJsonString)
                {
                    finalPrompt = $"{prompt} using the following context: {ctxJsonString.Value}";
                }
                else if (ctxJson is ErrorValue ev)
                {
                    // Propagate JSON serialization error
                    return ev;
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected result type from context JSON serialization. {ctxJson.GetType()}");
                }
            }

            // 2) If we have a schema, add strict "pure JSON" instruction (match the JS behavior)
            if (TryParseSchemaToDType(schemaStr, out var formulaType))
            {
                var jsonstr = DTypeToJsonSchema(formulaType._type);
                var sb = new StringBuilder();
                sb.Append(finalPrompt);
                sb.AppendLine();
                sb.Append("Provide the response as a pure JSON value ");
                sb.Append("(without any introductions, prefixes, suffixes, summaries, or markings around it), ");
                sb.Append("according to the following schema:");
                sb.AppendLine();
                sb.Append(jsonstr);
                finalPrompt = sb.ToString();
            }

            return FormulaValue.New(finalPrompt);
        }

        /// <summary>
        /// Uses the same machinery as the JSON() function (JsonFunctionImpl) to
        /// serialize a FormulaValue to a JSON string with correct timezone rules.
        /// </summary>
        private static FormulaValue FormulaValueToJsonString(
            IServiceProvider sp,
            FormulaValue value,
            CancellationToken ct)
        {
            // These services are what your JsonFunctionImpl expects
            var tz = sp.GetService(typeof(TimeZoneInfo)) as TimeZoneInfo;

            var canceller = sp.GetService(typeof(Canceller)) as Canceller
                            ?? new Canceller(() => ct.ThrowIfCancellationRequested());

            // Reuse the inner processing to avoid duplicating serialization rules.
            // Arguments mirror the JSON() function: first arg = value to serialize,
            // no option flags. We pass `supportsLazyTypes: true` to be permissive.
            var proc = new JsonFunctionImpl.JsonProcessing(tz, FormulaType.String, new[] { value }, supportsLazyTypes: true);
            var fv = proc.Process(canceller);
            return fv;
        }

        private static bool TryParseSchemaToDType(
            string schemaJson, 
            out FormulaType formulaType)
        {
            formulaType = null;

            if (string.IsNullOrWhiteSpace(schemaJson))
            {
                return false;
            }

            // Use the internal DType.TryParse method
            if (DType.TryParse(schemaJson, out var dType) && dType.IsValid)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts a DType to a JSON Schema representation.
        /// </summary>
        /// <param name="dType">The DType to convert.</param>
        /// <returns>A JSON Schema string representation of the DType.</returns>
        private static string DTypeToJsonSchema(DType dType)
        {
            if (!dType.IsValid)
            {
                throw new ArgumentException("Invalid DType", nameof(dType));
            }

            var schemaNode = BuildJsonSchemaNode(dType);
            return schemaNode.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }

        /// <summary>
        /// Builds a JsonNode representing the JSON Schema for a DType.
        /// </summary>
        private static JsonNode BuildJsonSchemaNode(DType dType)
        {
            var schema = new JsonObject();

            switch (dType.Kind)
            {
                case DKind.Boolean:
                    schema["type"] = "boolean";
                    break;

                case DKind.Number:
                case DKind.Currency:
                    schema["type"] = "number";
                    break;

                case DKind.Decimal:
                    schema["type"] = "number";
                    schema["format"] = "decimal";
                    break;

                case DKind.String:
                case DKind.Hyperlink:
                case DKind.Guid:
                    schema["type"] = "string";
                    break;

                case DKind.Date:
                    schema["type"] = "string";
                    schema["format"] = "date";
                    break;

                case DKind.DateTime:
                    schema["type"] = "string";
                    schema["format"] = "date-time";
                    break;

                case DKind.DateTimeNoTimeZone:
                    schema["type"] = "string";
                    schema["format"] = "date-time";
                    schema["x-ms-timezone"] = "none";
                    break;

                case DKind.Time:
                    schema["type"] = "string";
                    schema["format"] = "time";
                    break;

                case DKind.Color:
                    schema["type"] = "string";
                    schema["format"] = "color";
                    break;

                case DKind.Image:
                case DKind.Media:
                case DKind.Blob:
                case DKind.PenImage:
                    schema["type"] = "string";
                    schema["format"] = "binary";
                    break;

                case DKind.Record:
                case DKind.LazyRecord:
                    BuildRecordSchemaNode(schema, dType);
                    break;

                case DKind.Table:
                case DKind.LazyTable:
                    BuildTableSchemaNode(schema, dType);
                    break;

                case DKind.Enum:
                    BuildEnumSchemaNode(schema, dType);
                    break;

                case DKind.OptionSetValue:
                    schema["type"] = "string";
                    schema["x-ms-type"] = "optionset";
                    break;

                case DKind.UntypedObject:
                    schema["type"] = "object";
                    schema["x-ms-type"] = "dynamic";
                    break;

                case DKind.Unknown:
                case DKind.Deferred:
                    schema["description"] = "Unknown or deferred type";
                    break;

                case DKind.Error:
                    schema["type"] = "object";
                    schema["x-ms-type"] = "error";
                    break;

                case DKind.ObjNull:
                    schema["type"] = "null";
                    break;

                case DKind.Void:
                    schema["description"] = "Void type (no value)";
                    break;

                default:
                    schema["type"] = "object";
                    schema["x-ms-type"] = dType.Kind.ToString().ToLowerInvariant();
                    break;
            }

            return schema;
        }

        /// <summary>
        /// Builds a record JSON Schema node.
        /// </summary>
        private static void BuildRecordSchemaNode(JsonObject schema, DType recordType)
        {
            schema["type"] = "object";

            var fields = recordType.GetNames(DPath.Root).ToArray();
            
            if (fields.Length > 0)
            {
                var properties = new JsonObject();

                foreach (var field in fields)
                {
                    properties[field.Name.Value] = BuildJsonSchemaNode(field.Type);
                }

                schema["properties"] = properties;

                // Add required fields
                var requiredFields = fields.Where(f => !f.Type.IsUnknown && !f.Type.IsError)
                                           .Select(f => f.Name.Value)
                                           .ToArray();

                if (requiredFields.Length > 0)
                {
                    var requiredArray = new JsonArray();
                    foreach (var fieldName in requiredFields)
                    {
                        requiredArray.Add(JsonValue.Create(fieldName));
                    }

                    schema["required"] = requiredArray;
                }
            }
        }

        /// <summary>
        /// Builds a table JSON Schema node (array of records).
        /// </summary>
        private static void BuildTableSchemaNode(JsonObject schema, DType tableType)
        {
            schema["type"] = "array";
            
            // Tables are arrays of records
            var recordType = tableType.ToRecord();
            schema["items"] = BuildJsonSchemaNode(recordType);
        }

        /// <summary>
        /// Builds an enum JSON Schema node.
        /// </summary>
        private static void BuildEnumSchemaNode(JsonObject schema, DType enumType)
        {
            var superType = enumType.GetEnumSupertype();
            
            // Set the base type
            switch (superType.Kind)
            {
                case DKind.Number:
                case DKind.Decimal:
                    schema["type"] = "number";
                    break;
                case DKind.String:
                    schema["type"] = "string";
                    break;
                case DKind.Boolean:
                    schema["type"] = "boolean";
                    break;
                default:
                    schema["type"] = "string";
                    break;
            }

            // Add enum values
            var enumValues = enumType.ValueTree.GetPairs().ToArray();
            if (enumValues.Length > 0)
            {
                var valuesArray = new JsonArray();
                
                foreach (var enumValue in enumValues)
                {
                    var value = enumValue.Value.Object;
                    
                    if (value is string strVal)
                    {
                        valuesArray.Add(JsonValue.Create(strVal));
                    }
                    else if (value is double dblVal)
                    {
                        valuesArray.Add(JsonValue.Create(dblVal));
                    }
                    else if (value is int intVal)
                    {
                        valuesArray.Add(JsonValue.Create(intVal));
                    }
                    else if (value is bool boolVal)
                    {
                        valuesArray.Add(JsonValue.Create(boolVal));
                    }
                    else if (value is decimal decVal)
                    {
                        valuesArray.Add(JsonValue.Create(decVal));
                    } 
                    else
                    {
                        // Fallback to string representation
                        valuesArray.Add(JsonValue.Create(value?.ToString() ?? string.Empty));
                    }
                }

                schema["enum"] = valuesArray;
            }
        }
    }
}
