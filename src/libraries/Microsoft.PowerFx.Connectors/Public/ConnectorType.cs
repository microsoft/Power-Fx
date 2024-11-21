// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.PowerFx.Core;
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
    public class ConnectorType : SupportsConnectorErrors, IEquatable<ConnectorType>
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

        // Supports x-ms-notification-url
        public bool? NotificationUrl { get; }

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
        internal IEnumerable<ConnectorRelationship> ExternalTables => _externalTables;

        private List<ConnectorRelationship> _externalTables;

        internal ConnectorType(ISwaggerSchema schema, ISwaggerParameter openApiParameter, FormulaType formulaType, ErrorResourceKey warning = default)
        {
            Name = openApiParameter?.Name;
            IsRequired = openApiParameter?.Required == true;
            Visibility = openApiParameter?.GetVisibility().ToVisibility() ?? Visibility.Unknown;
            FormulaType = formulaType;
            Schema = schema;
            Binary = schema.Format == "binary" || schema.Format == "no_format";
            MediaKind = openApiParameter?.GetMediaKind().ToMediaKind() ?? (Binary ? MediaKind.File : MediaKind.NotBinary);
            NotificationUrl = openApiParameter?.GetNotificationUrl();

            if (schema != null)
            {
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
                    _externalTables = schema.ReferenceTo.Select(r => new ConnectorRelationship()
                    {
                        ForeignTable = r,
                        ForeignKey = null // Seems to always be Id 
                    }).ToList();
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

        internal ConnectorType(JsonElement schema, ConnectorCompatibility compatibility, IList<SqlRelationship> sqlRelationships)
            : this(SwaggerJsonSchema.New(schema), null, new SwaggerParameter(null, true, SwaggerJsonSchema.New(schema), null).GetConnectorType(compatibility, sqlRelationships))
        {
        }

        // Called by ConnectorFunction.GetCdpTableType
        internal ConnectorType(JsonElement schema, string tableName, SymbolTable optionSets, ConnectorCompatibility compatibility, IList<SqlRelationship> sqlRelationships, IList<ReferencedEntity> referencedEntities, string datasetName, string name, string connectorName, ICdpTableResolver resolver, ServiceCapabilities serviceCapabilities, bool isTableReadOnly)
            : this(SwaggerJsonSchema.New(schema), null, new SwaggerParameter(null, true, SwaggerJsonSchema.New(schema), null).GetConnectorType(tableName, optionSets, compatibility, sqlRelationships))
        {
            Name = name;

            foreach (ConnectorType field in Fields.Where(f => f.Capabilities != null))
            {
                serviceCapabilities.AddColumnCapability(field.Name, field.Capabilities);
            }

            FormulaType = new CdpRecordType(this, resolver, ServiceCapabilities.ToDelegationInfo(serviceCapabilities, name, isTableReadOnly, this, datasetName));
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
                _displayNameProvider ??= new SingleSourceDisplayNameProvider(Fields.Select(field => new KeyValuePair<DName, DName>(new DName(field.Name), new DName(field.DisplayName ?? field.Name))));
                return _displayNameProvider;
            }
        }

        private DisplayNameProvider _displayNameProvider;

        internal void SetRelationships(IEnumerable<SqlRelationship> relationships)
        {
            _externalTables ??= new List<ConnectorRelationship>();

            foreach (SqlRelationship relationship in relationships)
            {
                _externalTables.Add(new ConnectorRelationship()
                {
                    ForeignTable = relationship.ReferencedTable,
                    ForeignKey = relationship.ReferencedColumnName
                });
            }
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

        public bool Equals(ConnectorType other)
        {
            if (other == null)
            {
                return false;
            }

            return this.Name == other.Name &&
                   this.DisplayName == other.DisplayName &&
                   this.Description == other.Description &&
                   this.IsRequired == other.IsRequired &&
                   Enumerable.SequenceEqual((IList<ConnectorType>)this.Fields ?? Array.Empty<ConnectorType>(), (IList<ConnectorType>)other.Fields ?? Array.Empty<ConnectorType>()) &&
                   Enumerable.SequenceEqual((IList<ConnectorType>)this.HiddenFields ?? Array.Empty<ConnectorType>(), (IList<ConnectorType>)other.HiddenFields ?? Array.Empty<ConnectorType>()) &&
                   this.ExplicitInput == other.ExplicitInput &&
                   this.IsEnum == other.IsEnum &&
                   Enumerable.SequenceEqual((IList<FormulaValue>)this.EnumValues ?? Array.Empty<FormulaValue>(), (IList<FormulaValue>)other.EnumValues ?? Array.Empty<FormulaValue>()) &&
                   Enumerable.SequenceEqual((IList<string>)this.EnumDisplayNames ?? Array.Empty<string>(), (IList<string>)other.EnumDisplayNames ?? Array.Empty<string>()) &&
                   this.Visibility == other.Visibility &&
                   ((this.Capabilities == null && other.Capabilities == null) || this.Capabilities.Equals(other.Capabilities)) &&
                   this.KeyType == other.KeyType &&
                   this.KeyOrder == other.KeyOrder &&
                   this.Permission == other.Permission &&
                   this.NotificationUrl == other.NotificationUrl &&
                   ((this.HiddenRecordType == null && other.HiddenRecordType == null) || this.HiddenRecordType.Equals(other.HiddenRecordType)) &&
                   this.Binary == other.Binary &&
                   this.MediaKind == other.MediaKind &&
                   Enumerable.SequenceEqual((IList<string>)this.ExternalTables ?? Array.Empty<string>(), (IList<string>)other.ExternalTables ?? Array.Empty<string>());
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConnectorType);
        }

        public override int GetHashCode()
        {
            int h = Hashing.CombineHash(Name.GetHashCode(), DisplayName?.GetHashCode() ?? 0, Description?.GetHashCode() ?? 0, IsRequired.GetHashCode());

            if (Fields != null)
            {
                foreach (ConnectorType field in Fields)
                {
                    h = Hashing.CombineHash(h, field.GetHashCode());
                }
            }

            if (HiddenFields != null)
            {
                foreach (ConnectorType hiddenField in HiddenFields)
                {
                    h = Hashing.CombineHash(h, hiddenField.GetHashCode());
                }
            }            

            h = Hashing.CombineHash(h, ExplicitInput.GetHashCode(), IsEnum.GetHashCode(), Visibility.GetHashCode());

            if (EnumValues != null)
            {
                foreach (FormulaValue enumValue in EnumValues)
                {
                    h = Hashing.CombineHash(h, enumValue.GetHashCode());
                }
            }

            if (EnumDisplayNames != null)
            {
                foreach (string enumDisplayName in EnumDisplayNames)
                {
                    h = Hashing.CombineHash(h, enumDisplayName.GetHashCode());
                }
            }            

            h = Hashing.CombineHash(h, Capabilities.GetHashCode(), KeyType.GetHashCode(), KeyOrder.GetHashCode(), Permission.GetHashCode(), NotificationUrl?.GetHashCode() ?? 0, HiddenRecordType.GetHashCode());
            h = Hashing.CombineHash(h, Binary.GetHashCode());
            h = Hashing.CombineHash(h, MediaKind.GetHashCode());

            if (ExternalTables != null)
            {
                foreach (ConnectorRelationship relationship in ExternalTables)
                {
                    h = Hashing.CombineHash(h, relationship.GetHashCode());                   
                }
            }

            return h;
        }
    }
}
