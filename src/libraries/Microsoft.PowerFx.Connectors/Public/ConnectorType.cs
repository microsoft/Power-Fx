// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Connectors.Constants;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Wrapper class around FormulaType and ConnectorType. Contains detailed information from the swagger file and extensions.
    /// </summary>
    [DebuggerDisplay("{FormulaType._type}")]
    public class ConnectorType : SupportsConnectorErrors
    {
        /// <summary>
        /// Gets or sets the name of the connector type.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the display name ("x-ms-summary" or "title").
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the field is required.
        /// </summary>
        public bool IsRequired { get; internal set; }

        /// <summary>
        /// Gets the fields for RecordType and TableType.
        /// </summary>
        public ConnectorType[] Fields { get; }

        /// <summary>
        /// Gets the formula type.
        /// </summary>
        public FormulaType FormulaType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the input is explicit ("x-ms-explicit-input").
        /// </summary>
        public bool ExplicitInput { get; }

        /// <summary>
        /// Gets a value indicating whether this type is an enum.
        /// </summary>
        public bool IsEnum { get; }

        /// <summary>
        /// Gets the enumeration values, only defined if IsEnum is true.
        /// </summary>
        public FormulaValue[] EnumValues { get; }

        /// <summary>
        /// Gets the enumeration display names ("x-ms-enum-display-name"), only defined if IsEnum is true.
        /// </summary>
        public string[] EnumDisplayNames { get; }

        /// <summary>
        /// Gets the enum dictionary.
        /// </summary>
        public Dictionary<string, FormulaValue> Enum => GetEnum();

        /// <summary>
        /// Gets the visibility (supports x-ms-visibility).
        /// </summary>
        public Visibility Visibility { get; internal set; }

        /// <summary>
        /// Gets the key type (supports x-ms-keyType).
        /// </summary>
        public ConnectorKeyType KeyType { get; }

        /// <summary>
        /// Gets the key order (supports x-ms-keyOrder, only valid if KeyType = Primary).
        /// </summary>
        public double KeyOrder { get; }

        /// <summary>
        /// Gets the permission.
        /// </summary>
        public ConnectorPermission Permission { get; }

        /// <summary>
        /// Gets a value indicating whether notification URL is supported (supports x-ms-notification-url).
        /// </summary>
        public bool? NotificationUrl { get; }

        /// <summary>
        /// Gets the AI sensitivity (supports x-ms-ai-sensitivity).
        /// </summary>
        public AiSensitivity AiSensitivity { get; }

        /// <summary>
        /// Gets the property entity type (supports x-ms-property-entity-type).
        /// </summary>
        public string PropertyEntityType { get; }

        /// <summary>
        /// Gets a value indicating whether dynamic values or list are supported locally.
        /// </summary>
        public bool SupportsDynamicValuesOrList => DynamicValues != null || DynamicList != null;

        /// <summary>
        /// Gets a value indicating whether dynamic values or list are supported locally or anywhere in the tree.
        /// </summary>
        public bool ContainsDynamicValuesOrList => SupportsDynamicValuesOrList || (Fields != null && Fields.Any(f => f.ContainsDynamicValuesOrList));

        /// <summary>
        /// Gets a value indicating whether dynamic schema or property are supported locally.
        /// </summary>
        public bool SupportsDynamicSchemaOrProperty => DynamicSchema != null || DynamicProperty != null;

        /// <summary>
        /// Gets a value indicating whether dynamic schema or property are supported locally or anywhere in the tree.
        /// </summary>
        public bool ContainsDynamicSchemaOrProperty => SupportsDynamicSchemaOrProperty || (Fields != null && Fields.Any(f => f.ContainsDynamicSchemaOrProperty));

        /// <summary>
        /// Gets a value indicating whether dynamic intellisense is supported locally.
        /// </summary>
        public bool SupportsDynamicIntellisense => SupportsDynamicValuesOrList || SupportsDynamicSchemaOrProperty;

        /// <summary>
        /// Gets a value indicating whether dynamic intellisense is supported locally or anywhere in the tree.
        /// </summary>
        public bool ContainsDynamicIntellisense => ContainsDynamicValuesOrList || ContainsDynamicSchemaOrProperty;

        internal ConnectorDynamicSchema DynamicSchema { get; private set; }

        internal ConnectorDynamicProperty DynamicProperty { get; private set; }

        internal ConnectorDynamicValue DynamicValues { get; private set; }

        internal ConnectorDynamicList DynamicList { get; private set; }

        internal bool Binary { get; private set; }

        /// <summary>
        /// Gets the media kind (supports x-ms-media-kind).
        /// </summary>
        internal MediaKind MediaKind { get; private set; }

        internal ISwaggerSchema Schema { get; private set; } = null;

        // Relationships to external tables
        internal List<string> ExternalTables { get; set; }

        internal string RelationshipName { get; set; }

        internal string ForeignKey { get; set; }

        internal CDPMetadataItem FieldMetadata { get; }

        internal ConnectorType(ISwaggerSchema schema, ISwaggerParameter openApiParameter, FormulaType formulaType, ExpressionError warning = default, IEnumerable<KeyValuePair<DName, DName>> list = null, bool isNumber = false)
        {
            Name = openApiParameter?.Name;
            IsRequired = openApiParameter?.Required == true;
            Visibility = openApiParameter?.GetVisibility().ToVisibility() ?? Visibility.Unknown;
            FormulaType = formulaType;
            Schema = schema;
            Binary = schema.Format == "binary" || schema.Format == "no_format";
            MediaKind = openApiParameter?.GetMediaKind().ToMediaKind() ?? (Binary ? MediaKind.File : MediaKind.NotBinary);
            NotificationUrl = openApiParameter?.GetNotificationUrl();
            PropertyEntityType = openApiParameter?.GetPropertyEntityType();
            AiSensitivity = openApiParameter?.GetAiSensitivity().ToAiSensitivity() ?? AiSensitivity.Unknown;
            Description = schema.Description;

            string summary = schema.GetSummary();
            string title = schema.Title;

            DisplayName = string.IsNullOrEmpty(title) ? summary : title;
            ExplicitInput = schema.GetExplicitInput();
            Capabilities = schema.GetColumnCapabilities();
            Relationships = schema.GetRelationships(); // x-ms-relationships
            KeyType = schema.GetKeyType();
            KeyOrder = schema.GetKeyOrder();
            Permission = schema.GetPermission();

            // We only support one reference for now
            // SalesForce only
            if (schema.ReferenceTo != null && schema.ReferenceTo.Count == 1)
            {
                ExternalTables = new List<string>(schema.ReferenceTo);
                RelationshipName = schema.RelationshipName;
                ForeignKey = null; // SalesForce doesn't provide it, defaults to "Id"
            }

            Fields = Array.Empty<ConnectorType>();
            IsEnum = (schema.Enum != null && schema.Enum.Any()) || (list != null && list.Any());

            if (IsEnum)
            {
                if (list != null && list.Any())
                {
                    EnumValues = list.Select<KeyValuePair<DName, DName>, FormulaValue>(kvp => isNumber ? FormulaValue.New(decimal.Parse(kvp.Key.Value, CultureInfo.InvariantCulture)) : FormulaValue.New(kvp.Key)).ToArray();
                    EnumDisplayNames = list.Select(list => list.Value.Value).ToArray();
                }
                else
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
                    EnumDisplayNames = schema.Extensions != null && schema.Extensions.TryGetValue(XMsEnumDisplayName, out IOpenApiExtension enumNames) && enumNames is IList<IOpenApiAny> oaa
                                     ? oaa.Cast<OpenApiString>().Select(oas => oas.Value).ToArray()
                                     : Array.Empty<string>();

                    // x-ms-enum-values
                    if (!EnumDisplayNames.Any() && formulaType is OptionSetValueType osvt)
                    {
                        List<string> displayNames = new List<string>();

                        // ensure we follow the EnumValues order
                        foreach (FormulaValue enumName in EnumValues)
                        {
                            string logicalName = enumName switch
                            {
                                StringValue sv => sv.Value,
                                DecimalValue dv => dv.Value.ToString(CultureInfo.InvariantCulture),
                                NumberValue nv => nv.Value.ToString(CultureInfo.InvariantCulture),
                                _ => throw new InvalidOperationException("Not supported enum type")
                            };

                            if (osvt.TryGetValue(logicalName, out OptionSetValue osValue))
                            {
                                displayNames.Add(osValue.DisplayName ?? logicalName);
                            }
                        }

                        EnumDisplayNames = displayNames.ToArray();
                    }
                }
            }
            else
            {
                // those values are null/empty even if x-ms-dynamic-* could be present and would define possible values
                EnumValues = Array.Empty<FormulaValue>();
                EnumDisplayNames = Array.Empty<string>();
            }
            
            AddWarning(warning);            
            DynamicSchema = AggregateErrorsAndWarnings(openApiParameter.GetDynamicSchema());
            DynamicProperty = AggregateErrorsAndWarnings(openApiParameter.GetDynamicProperty());
            DynamicValues = AggregateErrorsAndWarnings(openApiParameter.GetDynamicValue());
            DynamicList = AggregateErrorsAndWarnings(openApiParameter.GetDynamicList());
        }

        internal static readonly FormulaType DefaultType = FormulaType.UntypedObject;

        internal ConnectorType(string error, string name, FormulaType formulaType, ExpressionError warning = default)
            : base(error, warning)
        {
            Name = name;
            FormulaType = formulaType;
        }

        internal ConnectorType(ISwaggerSchema schema, ConnectorSettings settings)
            : this(schema, null, new SwaggerParameter(null, true, schema, null).GetConnectorType(settings))
        {
        }

        // Called by ConnectorFunction.GetCdpTableType
        internal ConnectorType(JsonElement schema, string tableName, SymbolTable optionSets, ConnectorSettings settings, string datasetName, string name, string displayName, string connectorName, ICdpTableResolver resolver, ServiceCapabilities serviceCapabilities, bool isTableReadOnly, CDPMetadataItem fieldMetadata)
            : this(SwaggerJsonSchema.New(schema), null, new SwaggerParameter(null, true, SwaggerJsonSchema.New(schema), null).GetConnectorType(tableName, optionSets, settings))
        {
            Name = name;
            DisplayName = displayName;
            FieldMetadata = fieldMetadata;

            foreach (ConnectorType field in Fields.Where(f => f.Capabilities != null))
            {
                serviceCapabilities.AddColumnCapability(field.Name, field.Capabilities);
            }

            FormulaType = new CdpRecordType(this, resolver, ServiceCapabilities.ToDelegationInfo(serviceCapabilities, name, isTableReadOnly, this, datasetName), FieldMetadata);
        }

        internal ConnectorType(ISwaggerSchema schema, ISwaggerParameter openApiParameter, ConnectorType connectorType)
            : this(schema, openApiParameter, connectorType.FormulaType)
        {
            Fields = connectorType.Fields;
            AggregateErrorsAndWarnings(connectorType);
        }

        internal ConnectorType(ISwaggerSchema schema, ISwaggerParameter openApiParameter, FormulaType formulaType, RecordType hiddenRecordType)
            : this(schema, openApiParameter, formulaType)
        {
            HiddenRecordType = hiddenRecordType;
        }

        internal ConnectorType(ISwaggerSchema schema, ISwaggerParameter openApiParameter, TableType tableType, ConnectorType tableConnectorType)
            : this(schema, openApiParameter, tableType)
        {
            Fields = new ConnectorType[] { tableConnectorType };
            HiddenRecordType = null;
            AggregateErrorsAndWarnings(tableConnectorType);
        }

        internal ConnectorType(ISwaggerSchema schema, ISwaggerParameter openApiParameter, RecordType recordType, RecordType hiddenRecordType, ConnectorType[] fields, ConnectorType[] hiddenFields)
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

        internal DisplayNameProvider DisplayNameProvider
        {
            get
            {
                _displayNameProvider ??= DisplayNameUtility.MakeUnique(Fields.Select(field => new KeyValuePair<string, string>(field.Name, field.DisplayName ?? field.Name)));
                return _displayNameProvider;
            }
        }

        private DisplayNameProvider _displayNameProvider;

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
