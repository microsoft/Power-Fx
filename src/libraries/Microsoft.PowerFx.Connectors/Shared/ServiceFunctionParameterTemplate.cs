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
        private TypedName _typedName;
        private readonly string _description;
        private readonly string _summary;
        private readonly FormulaValue _defaultValue;
        private readonly FormulaType _formulaType;
        private readonly ConnectorType _connectorType;
        private readonly ConnectorDynamicValue _dynamicValue;
        private readonly ConnectorDynamicSchema _dynamicSchema;

        public TypedName TypedName => _typedName;

        public FormulaType FormulaType => _formulaType;

        public ConnectorType ConnectorType => _connectorType;

        public string Description => _description;

        public string Summary => _summary;

        public FormulaValue DefaultValue => _defaultValue;

        public ConnectorDynamicValue ConnectorDynamicValue => _dynamicValue;
        
        public ConnectorDynamicSchema ConnectorDynamicSchema => _dynamicSchema;

        public ServiceFunctionParameterTemplate(FormulaType formulaType, ConnectorType connectorType, TypedName typedName, string description, string summary, FormulaValue defaultValue, ConnectorDynamicValue dynamicValue, ConnectorDynamicSchema dynamicSchema)
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
            _dynamicSchema = dynamicSchema;
        }

        public void SetTypedName(TypedName typedName)
        {
            _typedName = typedName;
        }
    }
}
