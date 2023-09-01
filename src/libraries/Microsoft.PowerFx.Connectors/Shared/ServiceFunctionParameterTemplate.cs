// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AppMagic.Authoring.Texl.Builtins;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using Contracts = Microsoft.PowerFx.Core.Utils.Contracts;

namespace Microsoft.AppMagic.Authoring
{
    internal sealed class ServiceFunctionParameterTemplate
    {
        private readonly TypedName _typedName;
        private readonly string _description;
        private readonly string _summary;
        private readonly FormulaValue _defaultValue;
        private readonly FormulaType _formulaType;
        private readonly ConnectorType _connectorType;
        private readonly ConnectorDynamicValue _dynamicValue;
        private readonly ConnectorDynamicList _dynamicList;
        private readonly ConnectorDynamicSchema _dynamicSchema;
        private readonly ConnectorDynamicProperty _dynamicProperty;

        public TypedName TypedName => _typedName;

        public FormulaType FormulaType => _formulaType;

        public ConnectorType ConnectorType => _connectorType;

        public string Description => _description;

        public string Summary => _summary;

        public FormulaValue DefaultValue => _defaultValue;

        public ConnectorDynamicValue ConnectorDynamicValue => _dynamicValue;

        public ConnectorDynamicList ConnectorDynamicList => _dynamicList;
        
        public ConnectorDynamicSchema ConnectorDynamicSchema => _dynamicSchema;
        
        public ConnectorDynamicProperty ConnectorDynamicProperty => _dynamicProperty;

        public bool SupportsDynamicIntellisense => (_dynamicValue != null && string.IsNullOrEmpty(_dynamicValue.Capability)) || _dynamicList != null || _dynamicSchema != null || _dynamicProperty != null;

        public ServiceFunctionParameterTemplate(FormulaType formulaType, ConnectorType connectorType, TypedName typedName, string description, string summary, FormulaValue defaultValue, ConnectorDynamicValue dynamicValue, ConnectorDynamicList dynamicList, ConnectorDynamicSchema dynamicSchema, ConnectorDynamicProperty dynamicProperty)
        {
            Contracts.Assert(typedName.IsValid);
            Contracts.AssertValueOrNull(description);
            Contracts.AssertValueOrNull(defaultValue);

            _formulaType = formulaType;
            _connectorType = connectorType;
            _typedName = typedName;
            _description = description;
            _summary = summary;
            _defaultValue = defaultValue;
            _dynamicValue = dynamicValue;
            _dynamicList = dynamicList;
            _dynamicSchema = dynamicSchema;
            _dynamicProperty = dynamicProperty;
        }
    }
}
