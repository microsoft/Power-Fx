// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
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
            yield return new[] { TexlStrings.JoinArg1, TexlStrings.JoinArg2, TexlStrings.JoinArg3 };
            yield return new[] { TexlStrings.JoinArg1, TexlStrings.JoinArg2, TexlStrings.JoinArg3, TexlStrings.JoinArg4 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var valid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            var leftTable = argTypes[0];
            var rightTable = argTypes[1];

            // JoinType argument is present?
            if (argTypes.Count() == 4)
            {
                var isArg3Valid = true;

                isArg3Valid &= BuiltInEnums.JoinTypeEnum.FormulaType._type.Accepts(argTypes[3], exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: context.Features.PowerFxV1CompatibilityRules);
                isArg3Valid &= args[3] is DottedNameNode dottedNamedNode && dottedNamedNode.Left is FirstNameNode firstNameNode && firstNameNode.Ident.Name.Value == LanguageConstants.JoinTypeName;

                if (!isArg3Valid)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[3], TexlStrings.ErrJoinNotPlainJoinTypeEnum);
                    valid = false;
                }
            }

            if (leftTable.CanUnionWithForcedUniqueColumns(rightTable, useLegacyDateTimeAccepts: false, features: context.Features, out var duplicatedDType))
            {
                returnType = DType.Union(leftTable, rightTable, false, Features.PowerFxV1);
            }
            else
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrJoinCantUnion, string.Join(", ", duplicatedDType.GetAllNames(DPath.Root).Select(tname => tname.Name.Value)));
                valid = false;
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
