// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    [DebuggerDisplay("{Name}: {Description}")]
    public class ConnectorType
    {
        // "name"
        public string Name { get; internal set; }

        // "x-ms-summary"
        public string DisplayName { get; }

        // "description"
        public string Description { get; }

        // "required"
        public bool IsRequired { get; internal set; }

        // Only used for RecordType and TableType
        public ConnectorType[] Fields { get; }

        // FormulaType
        public FormulaType FormulaType { get; }

        // "x-ms-explicit-input"
        public bool ExplicitInput { get; }

        // "enum" 
        public bool IsEnum { get; }

        // Enumeration value, only defined if IsEnum is true
        public FormulaValue[] EnumValues { get; }

        // Enumeration display name ("x-ms-enum-display-name"), only defined if IsEnum is true
        // If not defined, this array will be empty
        public string[] EnumDisplayNames { get; }

        // Option Set, only defined when IsEnum is true and EnumValues is not empty
        public OptionSet OptionSet => GetOptionSet();

        public ConnectorType(OpenApiSchema schema, FormulaType formulaType)
        {
            FormulaType = formulaType;
            Description = schema.Description;
            DisplayName = ArgumentMapper.GetSummary(schema);
            Fields = Array.Empty<ConnectorType>();
            ExplicitInput = ArgumentMapper.GetExplicitInput(schema);
            IsEnum = schema.Enum != null && schema.Enum.Any();

            if (IsEnum)
            {
                if (schema.Type == "string" || (string.IsNullOrEmpty(schema.Type) && schema.Enum.First() is OpenApiString))
                {
                    EnumValues = schema.Enum.Cast<OpenApiString>().Select(oas => new StringValue(IRContext.NotInSource(FormulaType.String), oas.Value)).ToArray();
                }
                else if (schema.Type == "integer" || (string.IsNullOrEmpty(schema.Type) && schema.Enum.First() is OpenApiInteger))
                {
                    EnumValues = schema.Enum.Cast<OpenApiInteger>().Select<OpenApiInteger, FormulaValue>(oai => formulaType == FormulaType.Decimal ? new DecimalValue(IRContext.NotInSource(FormulaType.Decimal), oai.Value) : new NumberValue(IRContext.NotInSource(FormulaType.Number), oai.Value)).ToArray();
                }
                else if (schema.Type == "boolean" || (string.IsNullOrEmpty(schema.Type) && schema.Enum.First() is OpenApiBoolean))
                {
                    EnumValues = schema.Enum.Cast<OpenApiBoolean>().Select(oab => new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), oab.Value)).ToArray();
                }
                else
                {
                    throw new NotSupportedException($"Enum type {schema.Type} is not supported");
                }

                if (schema.Extensions != null && schema.Extensions.TryGetValue("x-ms-enum-display-name", out IOpenApiExtension enumNames) && enumNames is OpenApiArray oaa)
                {
                    EnumDisplayNames = oaa.Cast<OpenApiString>().Select(oas => oas.Value).ToArray();
                }
                else
                {
                    EnumDisplayNames = Array.Empty<string>();
                }
            }
            else
            {
                EnumValues = Array.Empty<FormulaValue>();
                EnumDisplayNames = Array.Empty<string>();
            }

            IsRequired = false;
            Name = null;
        }

        private OptionSet GetOptionSet()
        {
            if (!IsEnum || string.IsNullOrEmpty(Name) || EnumValues.Length != EnumDisplayNames.Length)
            {
                return null;
            }

            return new OptionSet(Name, EnumValues.Select(ev => ev.ToObject().ToString()).Zip(EnumDisplayNames, (ev, dn) => new KeyValuePair<string, string>(ev, dn)).ToDictionary(kvp => new DName(kvp.Key), kvp => new DName(kvp.Value)).ToImmutableDictionary());
        }

        public ConnectorType(OpenApiSchema schema, RecordType recordType, ConnectorType[] fields)
            : this(schema, recordType)
        {
            Fields = fields;
        }

        public ConnectorType(OpenApiSchema schema, TableType recordType, ConnectorType field)
            : this(schema, recordType)
        {
            Fields = new ConnectorType[] { field };
        }
    }
}
