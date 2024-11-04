// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class JoinFunction : FilterFunctionBase
    {
        public override bool IsSelfContained => true;

        public override int ScopeArgs => 2;

        public JoinFunction()
            : base("Join", TexlStrings.AboutJoin, FunctionCategories.Table, DType.EmptyTable, 0x8, 3, 4, DType.EmptyTable, DType.EmptyTable)
        {
            ScopeInfo = new FunctionJoinScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            throw new NotImplementedException();
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var valid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            var leftTable = argTypes[0];
            var rightTable = argTypes[1];

            // JoinType argument is present.
            if (argTypes.Count() == 4)
            {
                valid &= BuiltInEnums.JoinTypeEnum.FormulaType._type.Accepts(argTypes[3], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules);
            }

            valid = leftTable.CanUnionWith(rightTable, useLegacyDateTimeAccepts: false, features: context.Features);

            if (valid)
            {
                // !!!TODO do I need to combine display providers?
                returnType = DType.Union(leftTable, rightTable, false, Features.PowerFxV1);
            }
            else
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrJoinCantUnion);
            }
            
            return valid;
        }

        public override bool IsLambdaParam(TexlNode node, int index)
        {
            return index == 2;
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { BuiltInEnums.JoinTypeEnum.EntityName.Value };
        }

        public override ParamIdentifierStatus GetIdentifierParamStatus(TexlNode node, Features features, int index)
        {
            return ParamIdentifierStatus.NeverIdentifier;
        }
    }
}
