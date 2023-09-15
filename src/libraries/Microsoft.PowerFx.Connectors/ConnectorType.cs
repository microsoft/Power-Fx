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
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Wrapper class around FormulaType and ConnectorType
    // FormulaType is used to represent the type of the parameter in the Power Fx expression as used in Power Apps
    // ConnectorType contains more details information coming from the swagger file and extensions
    [DebuggerDisplay("{Type._type}")]
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

        public ConnectorType[] HiddenFields { get; }

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

        public Visibility Visibility { get; internal set; }

        internal RecordType HiddenRecordType { get; }

        public bool SupportsSuggestions => DynamicReturnSchema != null || DynamicReturnProperty != null;

        internal ConnectorDynamicSchema DynamicReturnSchema { get; private set; }

        internal ConnectorDynamicProperty DynamicReturnProperty { get; private set; }

        internal ConnectorType(OpenApiSchema schema, string name, bool required, string visibility, FormulaType formulaType, RecordType hiddenRecordType)
        {

            Name = name;
            IsRequired = required;
            Visibility = visibility.ToVisibility();
            FormulaType = formulaType;
            HiddenRecordType = hiddenRecordType;

            if (schema != null)
            {
                Description = schema.Description;
                DisplayName = schema.GetSummary();
                ExplicitInput = schema.GetExplicitInput();

                Fields = Array.Empty<ConnectorType>();
                IsEnum = schema.Enum != null && schema.Enum.Any();

                if (IsEnum)
                {
                    EnumValues = schema.Enum.Select(oaa => OpenApiExtensions.TryGetOpenApiValue(oaa, out FormulaValue fv) ? fv : throw new NotSupportedException($"Invalid conversion for type {oaa.GetType().Name} in enum")).ToArray();
                    EnumDisplayNames = schema.Extensions != null && schema.Extensions.TryGetValue("x-ms-enum-display-name", out IOpenApiExtension enumNames) && enumNames is OpenApiArray oaa
                                        ? oaa.Cast<OpenApiString>().Select(oas => oas.Value).ToArray()
                                        : Array.Empty<string>();
                }
                else
                {
                    EnumValues = Array.Empty<FormulaValue>();
                    EnumDisplayNames = Array.Empty<string>();
                }
            }
        }

        internal ConnectorType(OpenApiSchema schema, string name, bool required, string visibility, FormulaType formulaType)
            : this(schema, name, required, visibility, formulaType, null)
        {
        }

        internal ConnectorType()
        {
            FormulaType = new BlankType();
        }

        internal ConnectorType(OpenApiSchema schema, string name, bool required, string visibility, TableType tableType, ConnectorType tableConnectorType)
            : this(schema, name, required, visibility, tableType)
        {
            HiddenRecordType = null;            
            Fields = new ConnectorType[] { tableConnectorType };            
        }

        internal ConnectorType(OpenApiSchema schema, string name, bool required, string visibility, RecordType recordType, RecordType hiddenRecordType, ConnectorType[] fields, ConnectorType[] hiddenFields)
            : this(schema, name, required, visibility, recordType)
        {
            HiddenRecordType = hiddenRecordType;

            Fields = fields;
            HiddenFields = hiddenFields;
        }

        internal void SetDynamicReturnSchemaAndProperty(ConnectorDynamicSchema dynamicSchema, ConnectorDynamicProperty dynamicProperty)
        {
            DynamicReturnSchema = dynamicSchema;
            DynamicReturnProperty = dynamicProperty;
        }

        private OptionSet GetOptionSet()
        {
            if (!IsEnum || string.IsNullOrEmpty(Name) || EnumValues.Length != EnumDisplayNames.Length)
            {
                return null;
            }

            return new OptionSet(Name, EnumValues.Select(ev => ev.ToObject().ToString()).Zip(EnumDisplayNames, (ev, dn) => new KeyValuePair<string, string>(ev, dn)).ToDictionary(kvp => new DName(kvp.Key), kvp => new DName(kvp.Value)).ToImmutableDictionary());
        }
    }
}
