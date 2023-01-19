// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using Contracts = Microsoft.PowerFx.Core.Utils.Contracts;

namespace Microsoft.AppMagic.Authoring
{
    internal sealed class ServiceFunctionParameterTemplate
    {
        private readonly string _docRefId;
        private readonly TypedName _typedName;
        private readonly string _description;
        private readonly FormulaValue _defaultValue;
        private readonly FormulaType _formulaType;

        public string DocRefId => _docRefId;

        public TypedName TypedName => _typedName;

        public FormulaType FormulaType => _formulaType;

        public string Description => _description;

        public FormulaValue DefaultValue => _defaultValue;

        public ServiceFunctionParameterTemplate(FormulaType formulaType, TypedName typedName, string description, FormulaValue defaultValue, string docRefId)
        {
            Contracts.Assert(typedName.IsValid);
            Contracts.AssertValueOrNull(description);
            Contracts.AssertValueOrNull(defaultValue);
            
            // If docRefId is null, this means no documentation is provided for this element.
            Contracts.AssertNonEmptyOrNull(docRefId);

            _formulaType = formulaType;
            _typedName = typedName;
            _description = description;
            _defaultValue = defaultValue;
            _docRefId = docRefId;
        }

        public ServiceFunctionParameterTemplate(FormulaType formulaType, TypedName typedName, string description, FormulaValue defaultValue)
        {
            Contracts.Assert(typedName.IsValid);
            Contracts.AssertValueOrNull(description);
            Contracts.AssertValueOrNull(defaultValue);

            _formulaType = formulaType;
            _typedName = typedName;
            _description = description;
            _defaultValue = defaultValue;
        }
    }
}
