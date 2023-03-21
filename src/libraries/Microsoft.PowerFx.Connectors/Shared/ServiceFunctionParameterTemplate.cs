// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AppMagic.Authoring.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using Contracts = Microsoft.PowerFx.Core.Utils.Contracts;

namespace Microsoft.AppMagic.Authoring
{
    internal sealed class ServiceFunctionParameterTemplate
    {
        private readonly TypedName _typedName;
        private readonly string _description;
        private readonly FormulaValue _defaultValue;
        private readonly FormulaType _formulaType;
        private readonly ConnectorDynamicValue _dynamicValue;
        private readonly ConnectorDynamicSchema _dynamicSchema;

        public TypedName TypedName => _typedName;

        public FormulaType FormulaType => _formulaType;

        public string Description => _description;

        public FormulaValue DefaultValue => _defaultValue;

        public ConnectorDynamicValue ConnectorDynamicValue => _dynamicValue;
        
        public ConnectorDynamicSchema ConnectorDynamicSchema => _dynamicSchema;

        public ServiceFunctionParameterTemplate(FormulaType formulaType, TypedName typedName, string description, FormulaValue defaultValue, ConnectorDynamicValue dynamicValue, ConnectorDynamicSchema dynamicSchema)
        {
            Contracts.Assert(typedName.IsValid);
            Contracts.AssertValueOrNull(description);
            Contracts.AssertValueOrNull(defaultValue);

            _formulaType = formulaType;
            _typedName = typedName;
            _description = description;
            _defaultValue = defaultValue;
            _dynamicValue = dynamicValue;
            _dynamicSchema = dynamicSchema;
        }
    }
}
