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
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Connectors.Constants;

namespace Microsoft.PowerFx.Connectors
{
    // Wrapper class around FormulaType and ConnectorType
    // FormulaType is used to represent the type of the parameter in the Power Fx expression as used in Power Apps
    // ConnectorType contains more details information coming from the swagger file and extensions
    [DebuggerDisplay("{FormulaType._type}")]
    public class ConnectorType : SupportsConnectorErrors
    {
        // "name"
        public string Name { get; internal set; }
        
        // "title"
        public string DisplayName { get; }

        // "description"
        public string Description { get; }

        // "x-ms-summary"
        public string Summary { get; }

        // "required"
        public bool IsRequired { get; internal set; }

        // Only used for RecordType and TableType
        public ConnectorType[] Fields { get; }

        internal ConnectorType[] HiddenFields { get; }

        // FormulaType
        public FormulaType FormulaType { get; }

        // "x-ms-explicit-input"
        public bool ExplicitInput { get; }

        public bool IsEnum { get; }

        // Enumeration value, only defined if IsEnum is true
        public FormulaValue[] EnumValues { get; }

        // Enumeration display name ("x-ms-enum-display-name"), only defined if IsEnum is true
        // If not defined, this array will be empty 
        public string[] EnumDisplayNames { get; }

        public Dictionary<string, FormulaValue> Enum => GetEnum();

        public Visibility Visibility { get; internal set; }

        internal RecordType HiddenRecordType { get; }

        // Supports x-ms-dynamic-values or -list locally
        public bool SupportsDynamicValuesOrList => DynamicValues != null || DynamicList != null;

        // Supports x-ms-dynamic-values or -list locally or anywhere in the tree
        public bool ContainsDynamicValuesOrList => SupportsDynamicValuesOrList || (Fields != null && Fields.Any(f => f.ContainsDynamicValuesOrList));

        // Supports x-ms-dynamic-schema or -property locally
        public bool SupportsDynamicSchemaOrProperty => DynamicSchema != null || DynamicProperty != null;

        // Supports x-ms-dynamic-schema or -property locally or anywhere in the tree
        public bool ContainsDynamicSchemaOrProperty => SupportsDynamicSchemaOrProperty || (Fields != null && Fields.Any(f => f.ContainsDynamicSchemaOrProperty));

        // Supports x-ms-dynamic-values, -list, -schema, or -property locally
        public bool SupportsDynamicIntellisense => SupportsDynamicValuesOrList || SupportsDynamicSchemaOrProperty;

        // Supports x-ms-dynamic-values, -list, -schema, or -property locally or anywhere in the tree
        public bool ContainsDynamicIntellisense => ContainsDynamicValuesOrList || ContainsDynamicSchemaOrProperty;

        internal ConnectorDynamicSchema DynamicSchema { get; private set; }

        internal ConnectorDynamicProperty DynamicProperty { get; private set; }

        internal ConnectorDynamicValue DynamicValues { get; private set; }

        internal ConnectorDynamicList DynamicList { get; private set; }

        internal bool Binary { get; private set; }

        internal MediaKind MediaKind { get; private set; }

        internal OpenApiSchema Schema { get; private set; } = null;

        internal ConnectorType(OpenApiSchema schema, OpenApiParameter openApiParameter, FormulaType formulaType, ErrorResourceKey warning = default)
        {
            Name = openApiParameter?.Name;
            IsRequired = openApiParameter?.Required == true;
            Visibility = openApiParameter?.GetVisibility().ToVisibility() ?? Visibility.Unknown;
            FormulaType = formulaType;
            Schema = schema;
            Binary = schema.Format == "binary" || schema.Format == "no_format";
            MediaKind = openApiParameter?.GetMediaKind().ToMediaKind() ?? (Binary ? MediaKind.File : MediaKind.NotBinary);

            if (schema != null)
            {
                Description = schema.Description;
                DisplayName = schema.Title;
                Summary = schema.GetSummary();
                ExplicitInput = schema.GetExplicitInput();

                Fields = Array.Empty<ConnectorType>();
                IsEnum = schema.Enum != null && schema.Enum.Any();

                if (IsEnum)
                {
                    EnumValues = schema.Enum.Select(oaa =>
                    {
                        if (OpenApiExtensions.TryGetOpenApiValue(oaa, null, out FormulaValue fv, this))
                        {
                            return fv;
                        }

                        AddError($"Invalid conversion for type {oaa.GetType().Name} in enum");
                        return FormulaValue.NewBlank();
                    }).ToArray();

                    // x-ms-enum-display-name
                    EnumDisplayNames = schema.Extensions != null && schema.Extensions.TryGetValue(XMsEnumDisplayName, out IOpenApiExtension enumNames) && enumNames is OpenApiArray oaa
                                        ? oaa.Cast<OpenApiString>().Select(oas => oas.Value).ToArray()
                                        : Array.Empty<string>();                                        
                }
                else
                {
                    // those values are null/empty even if x-ms-dynamic-* could be present and would define possible values
                    EnumValues = Array.Empty<FormulaValue>();
                    EnumDisplayNames = Array.Empty<string>();
                }
            }

            AddWarning(warning);
            DynamicSchema = AggregateErrorsAndWarnings(openApiParameter.GetDynamicSchema());
            DynamicProperty = AggregateErrorsAndWarnings(openApiParameter.GetDynamicProperty());
            DynamicValues = AggregateErrorsAndWarnings(openApiParameter.GetDynamicValue());
            DynamicList = AggregateErrorsAndWarnings(openApiParameter.GetDynamicList());
        }

        internal static readonly FormulaType DefaultType = FormulaType.UntypedObject;

        internal ConnectorType(string error, ErrorResourceKey warning = default)
            : base(error, warning)
        {
            FormulaType = DefaultType;
        }

        internal ConnectorType(OpenApiSchema schema, ConnectorCompatibility compatibility)
            : this(schema, null, new OpenApiParameter() { Schema = schema }.GetConnectorType(compatibility))
        {
        }

        internal ConnectorType(OpenApiSchema schema, OpenApiParameter openApiParameter, ConnectorType connectorType)
            : this(schema, openApiParameter, connectorType.FormulaType)
        {
            Fields = connectorType.Fields;
            AggregateErrorsAndWarnings(connectorType);
        }

        internal ConnectorType(OpenApiSchema schema, OpenApiParameter openApiParameter, FormulaType formulaType, RecordType hiddenRecordType)
            : this(schema, openApiParameter, formulaType)
        {
            HiddenRecordType = hiddenRecordType;
        }

        internal ConnectorType(OpenApiSchema schema, OpenApiParameter openApiParameter, TableType tableType, ConnectorType tableConnectorType)
            : this(schema, openApiParameter, tableType)
        {
            Fields = new ConnectorType[] { tableConnectorType };
            HiddenRecordType = null;
            AggregateErrorsAndWarnings(tableConnectorType);
        }

        internal ConnectorType(OpenApiSchema schema, OpenApiParameter openApiParameter, RecordType recordType, RecordType hiddenRecordType, ConnectorType[] fields, ConnectorType[] hiddenFields)
            : this(schema, openApiParameter, recordType)
        {
            Fields = fields;
            HiddenFields = hiddenFields;
            HiddenRecordType = hiddenRecordType;
            AggregateErrors(fields);
            AggregateErrors(hiddenFields);
        }

        internal ConnectorType(ConnectorType connectorType, ConnectorType[] fields, FormulaType formulaType)
        {
            Binary = connectorType.Binary;
            Description = connectorType.Description;
            DisplayName = connectorType.DisplayName;
            EnumDisplayNames = connectorType.EnumDisplayNames;
            EnumValues = connectorType.EnumValues;
            ExplicitInput = connectorType.ExplicitInput;            
            IsEnum = true;
            IsRequired = connectorType.IsRequired;
            MediaKind = connectorType.MediaKind;
            Name = connectorType.Name;
            Schema = connectorType.Schema;
            Visibility = connectorType.Visibility;

            Fields = fields;
            FormulaType = formulaType;

            DynamicList = null;
            DynamicProperty = null;
            DynamicSchema = null;
            DynamicValues = null;

            _errors = connectorType._errors;
            _warnings = connectorType._warnings;
        }

        private void AggregateErrors(ConnectorType[] types)
        {
            if (types != null)
            {
                foreach (ConnectorType type in types)
                {
                    AggregateErrorsAndWarnings(type);
                }
            }
        }

        // Keeping code for creating OptionSet if we need it later

        //private OptionSet GetOptionSet()
        //{
        //    if (!IsOptionSet || string.IsNullOrEmpty(Name))
        //    {
        //        return null;
        //    }

        //    string[] enumValues = EnumValues.Select(ev => ev.ToObject().ToString()).ToArray();
        //    string[] enumDisplayNames = EnumDisplayNames ?? enumValues;

        //    return new OptionSet(Name, enumValues.Zip(enumDisplayNames, (ev, dn) => new KeyValuePair<string, string>(ev, dn)).ToDictionary(kvp => new DName(kvp.Key), kvp => new DName(kvp.Value)).ToImmutableDictionary());
        //}

        private Dictionary<string, FormulaValue> GetEnum()
        {
            if (!IsEnum || string.IsNullOrEmpty(Name))
            {
                return null;
            }

            FormulaValue[] enumValues = EnumValues;
            string[] enumDisplayNames = EnumDisplayNames ?? enumValues.Select(ev => ev.ToObject().ToString()).ToArray();

            return enumDisplayNames.Zip(enumValues, (dn, ev) => new KeyValuePair<string, FormulaValue>(dn, ev)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}
