// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal sealed class TableConcatenateFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public TableConcatenateFunction()
            : base("TableConcatenate", TexlStrings.AboutTableConcatenate, FunctionCategories.Table, DType.EmptyTable, 0, 2, int.MaxValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TableConcatenateArg1, TexlStrings.TableConcatenateArg1 };
            yield return new[] { TexlStrings.TableConcatenateArg1, TexlStrings.TableConcatenateArg1, TexlStrings.TableConcatenateArg1 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.TableConcatenateArg1, TexlStrings.TableConcatenateArg1);
            }

            return base.GetSignatures(arity);
        }

        // Typecheck an invocation of TableConcatenate.
        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var isValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            // Ensure that all args (if any) are tables with compatible schemas.
            var tableType = DType.EmptyTable;
            for (var i = 0; i < argTypes.Length; i++)
            {
                var argType = argTypes[i];

                if (!argType.IsTable)
                {
                    errors.TypeMismatchError(args[i], tableType, argType);
                    isValid = false;
                }
                else
                {
                    if (DType.TryUnionWithCoerce(
                        tableType,
                        argType,
                        context.Features,
                        coerceToLeftTypeOnly: true,
                        out var newType,
                        out bool coercionNeeded))
                    {
                        tableType = newType;

                        if (coercionNeeded)
                        {
                            CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], tableType);
                        }
                    }
                    else
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrTableDoesNotAcceptThisType);
                        isValid = false;
                    }
                }

                Contracts.Assert(tableType.IsTable);
            }

            returnType = tableType;

            return isValid;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            base.CheckSemantics(binding, args, argTypes, errors);

            for (var i = 0; i < argTypes.Length; i++)
            {
                var ads = argTypes[i].AssociatedDataSources?.FirstOrDefault();

                if (ads is IExternalDataSource tDsInfo && tDsInfo is IExternalTabularDataSource)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrorDelegationTableNotSupported, Name, ads.EntityName);
                    continue;
                }
            }
        }
    }
}
