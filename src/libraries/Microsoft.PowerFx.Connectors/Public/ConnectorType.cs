// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
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

        // "x-ms-summary" or "title"
        public string DisplayName { get; }

        // "description"
        public string Description { get; }

        // "required"
        public bool IsRequired { get; internal set; }

        // Only used for RecordType and TableType
        public ConnectorType[] Fields { get; }

        internal ConnectorType[] HiddenFields { get; }

        // FormulaType
        public FormulaType FormulaType { get; private set; }

        // "x-ms-explicit-input"
        public bool ExplicitInput { get; }

        public bool IsEnum { get; }

        // Enumeration value, only defined if IsEnum is true
        public FormulaValue[] EnumValues { get; }

        // Enumeration display name ("x-ms-enum-display-name"), only defined if IsEnum is true
        // If not defined, this array will be empty
        public string[] EnumDisplayNames { get; }

        public Dictionary<string, FormulaValue> Enum => GetEnum();

        // Supports x-ms-visibility
        public Visibility Visibility { get; internal set; }

        // Supports x-ms-capabilities
        internal ColumnCapabilities Capabilities { get; }

        // Supports x-ms-relationships
        internal Dictionary<string, Relationship> Relationships { get; }

        // Supports x-ms-keyType
        public ConnectorKeyType KeyType { get; }

        // Supports x-ms-keyOrder (only valid if KeyType = Primary)
        public double KeyOrder { get; }

        public ConnectorPermission Permission { get; }

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

        // Supports x-ms-media-kind
        internal MediaKind MediaKind { get; private set; }

        internal ISwaggerSchema Schema { get; private set; } = null;

        // Relationships to external tables
        internal List<string> ExternalTables { get; }

        internal string RelationshipName { get; }

        internal ConnectorType(ISwaggerSchema schema, ISwaggerParameter openApiParameter, FormulaType formulaType, ErrorResourceKey warning = default)
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

                string summary = schema.GetSummary();
                string title = schema.Title;

                DisplayName = string.IsNullOrEmpty(title) ? summary : title;
                ExplicitInput = schema.GetExplicitInput();
                Capabilities = schema.GetColumnCapabilities();
                Relationships = schema.GetRelationships();
                KeyType = schema.GetKeyType();
                KeyOrder = schema.GetKeyOrder();
                Permission = schema.GetPermission();                

                // We only support one reference for now
                // SalesForce only
                if (schema.ReferenceTo != null && schema.ReferenceTo.Count == 1)
                {
                    ExternalTables = new List<string>(schema.ReferenceTo);
                    RelationshipName = schema.RelationshipName;
                }

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
                    EnumDisplayNames = schema.Extensions != null && schema.Extensions.TryGetValue(XMsEnumDisplayName, out IOpenApiExtension enumNames) && enumNames is IList<IOpenApiAny> oaa
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

        internal ConnectorType(ISwaggerSchema schema, ConnectorCompatibility compatibility)
            : this(schema, null, new SwaggerParameter(null, true, schema, null).GetConnectorType(compatibility))
        {
        }

        internal ConnectorType(JsonElement schema, ConnectorCompatibility compatibility)
            : this(SwaggerJsonSchema.New(schema), null, new SwaggerParameter(null, true, SwaggerJsonSchema.New(schema), null).GetConnectorType(compatibility))
        {
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

        internal void AddTabularDataSource(ITabularTableResolver tableResolver, IList<ReferencedEntity> referencedEntities, DName name, string datasetName, ConnectorType connectorType, ServiceCapabilities serviceCapabilities, bool isReadOnly, BidirectionalDictionary<string, string> displayNameMapping = null)
        {
            if (FormulaType is not RecordType)
            {
                throw new PowerFxConnectorException("Invalid FormulaType");
            }

            // $$$ Hack to enable IExternalTabularDataSource, will be removed later
            if (name.Value.StartsWith("__", StringComparison.OrdinalIgnoreCase))
            {
                HashSet<IExternalTabularDataSource> dataSource = new HashSet<IExternalTabularDataSource>() { new ExternalTabularDataSource(name, datasetName, serviceCapabilities, isReadOnly, displayNameMapping) };
                DType newDType = DType.CreateDTypeWithConnectedDataSourceInfoMetadata(FormulaType._type, dataSource, null);
                FormulaType = new KnownRecordType(newDType);
            }

            FormulaType = new TabularRecordType(connectorType, FormulaType._type, tableResolver, referencedEntities);
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
