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
using static Microsoft.PowerFx.Connectors.Constants;

namespace Microsoft.PowerFx.Connectors
{
    // Wrapper class around FormulaType and ConnectorType
    // FormulaType is used to represent the type of the parameter in the Power Fx expression as used in Power Apps
    // ConnectorType contains more details information coming from the swagger file and extensions
    [DebuggerDisplay("{FormulaType._type}")]
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

        internal ConnectorType[] HiddenFields { get; }

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

        public bool SupportsSuggestions => DynamicSchema != null || DynamicProperty != null;

        internal ConnectorDynamicSchema DynamicSchema { get; private set; }

        internal ConnectorDynamicProperty DynamicProperty { get; private set; }

        internal ConnectorType(OpenApiSchema schema, OpenApiParameter openApiParameter, FormulaType formulaType, bool numberIsFloat)
        {
            Name = openApiParameter?.Name;
            IsRequired = openApiParameter?.Required == true;
            Visibility = openApiParameter?.GetVisibility().ToVisibility() ?? Visibility.Unknown;

            FormulaType = formulaType;

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
                    EnumDisplayNames = schema.Extensions != null && schema.Extensions.TryGetValue(XMsEnumDisplayName, out IOpenApiExtension enumNames) && enumNames is OpenApiArray oaa
                                        ? oaa.Cast<OpenApiString>().Select(oas => oas.Value).ToArray()
                                        : Array.Empty<string>();
                }
                else
                {
                    EnumValues = Array.Empty<FormulaValue>();
                    EnumDisplayNames = Array.Empty<string>();
                }                                               
            }

            DynamicSchema = openApiParameter.GetDynamicSchema(numberIsFloat);
            DynamicProperty = openApiParameter.GetDynamicProperty(numberIsFloat);
        }

        internal ConnectorType()
        {
            FormulaType = new BlankType();
        }

        internal ConnectorType(OpenApiSchema schema, bool numberIsFloat)
            : this(schema, null, new OpenApiParameter() { Schema = schema }.ToConnectorType(), numberIsFloat)
        {
        }

        internal ConnectorType(OpenApiSchema schema, OpenApiParameter openApiParameter, ConnectorType connectorType, bool numberIsFloat)
            : this(schema, openApiParameter, connectorType.FormulaType, numberIsFloat)
        {
            Fields = connectorType.Fields;
        }

        internal ConnectorType(OpenApiSchema schema, OpenApiParameter openApiParameter, FormulaType formulaType, RecordType hiddenRecordType, bool numberIsFloat)
            : this(schema, openApiParameter, formulaType, numberIsFloat)
        {
            HiddenRecordType = hiddenRecordType;
        }

        internal ConnectorType(OpenApiSchema schema, OpenApiParameter openApiParameter, TableType tableType, ConnectorType tableConnectorType, bool numberIsFloat)
            : this(schema, openApiParameter, tableType, numberIsFloat)
        {
            Fields = new ConnectorType[] { tableConnectorType };
            HiddenRecordType = null;
        }

        internal ConnectorType(OpenApiSchema schema, OpenApiParameter openApiParameter, RecordType recordType, RecordType hiddenRecordType, ConnectorType[] fields, ConnectorType[] hiddenFields, bool numberIsFloat)
            : this(schema, openApiParameter, recordType, numberIsFloat)
        {
            Fields = fields;
            HiddenFields = hiddenFields;
            HiddenRecordType = hiddenRecordType;
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
