// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Public.Types.TypeCheckers
{
    internal abstract class AggregateTypeChecker
    {
        protected readonly ICollection<ExpressionError> _errorList;

        public AggregateTypeChecker(ICollection<ExpressionError> errorList)
        {
            _errorList = errorList ?? throw new ArgumentNullException(nameof(errorList));
        }

        public bool Run(AggregateType sourceType, AggregateType typeToCheck, FormulaTypeChecker formulaTypeChecker)
        {
            if (sourceType == null || typeToCheck == null)
            {
                return false;
            }
            else if (sourceType._type.Kind != typeToCheck._type.Kind)
            {
                _errorList.Add(new ExpressionError()
                {
                    Kind = ErrorKind.Validation,
                    ResourceKey = TexlStrings.ErrExpectedRVTypeMismatch,
                    MessageArgs = new object[] { sourceType._type.GetKindString(), typeToCheck._type.GetKindString() }
                });

                return false;
            }

            return IsMatch(sourceType, typeToCheck, formulaTypeChecker);
        }

        public abstract bool IsMatch(AggregateType sourceType, AggregateType typeToCheck, FormulaTypeChecker formulaTypeChecker);
    }

    internal class StrictAggregateTypeChecker : AggregateTypeChecker
    {
        public StrictAggregateTypeChecker(ICollection<ExpressionError> errorList)
            : base(errorList)
        {
        }

        public override bool IsMatch(AggregateType sourceType, AggregateType typeToCheck, FormulaTypeChecker formulaTypeChecker)
        {
            var maybeDVEntitySource = sourceType.TableSymbolName;
            var maybeDVEntityTarget = typeToCheck.TableSymbolName;

            if ((maybeDVEntitySource != null || maybeDVEntityTarget != null) &&
                maybeDVEntitySource != maybeDVEntityTarget)
            {
                _errorList.Add(new ExpressionError()
                {
                    Kind = ErrorKind.Validation,
                    ResourceKey = TexlStrings.ErrExpectedRVTypeMismatch,
                    MessageArgs = new object[] { maybeDVEntitySource ?? sourceType._type.GetKindString(),  maybeDVEntityTarget ?? typeToCheck._type.GetKindString() }
                });

                return false;
            }

            var sourceFieldCount = sourceType.FieldNames.Count();
            var typeToCheckFieldCount = typeToCheck.FieldNames.Count();
            if (sourceFieldCount != typeToCheckFieldCount)
            {
                if (sourceFieldCount < typeToCheckFieldCount)
                {
                    var extraFields = string.Join(", ", typeToCheck.FieldNames.Except(sourceType.FieldNames));
                    _errorList.Add(new ExpressionError()
                    {
                        Kind = ErrorKind.Validation,
                        ResourceKey = TexlStrings.ErrExpectedRVExtraFields,
                        MessageArgs = new object[] { extraFields }
                    });
                }
                else
                {
                    var missingFields = string.Join(", ", sourceType.FieldNames.Except(typeToCheck.FieldNames));
                    _errorList.Add(new ExpressionError()
                    {
                        Kind = ErrorKind.Validation,
                        ResourceKey = TexlStrings.ErrExpectedRVMissingFields,
                        MessageArgs = new object[] { missingFields }
                    });
                }

                return false;
            }

            foreach (var sourceField in sourceType.FieldNames)
            {
                if (!sourceType.TryGetFieldType(sourceField, out var sourceFieldType))
                {
                    return false;
                }

                if (!typeToCheck.TryGetFieldType(sourceField, out var targetFieldType))
                {
                    _errorList.Add(new ExpressionError()
                    {
                        Kind = ErrorKind.Validation,
                        ResourceKey = TexlStrings.ErrExpectedRVFieldNotFound,
                        MessageArgs = new object[] { sourceField }
                    });
                    return false;
                }

                if (!formulaTypeChecker.Run(sourceFieldType, targetFieldType))
                {
                    // remove error added by formulaTypeChecker
                    _errorList.Remove(_errorList.Last());

                    _errorList.Add(new ExpressionError()
                    {
                        Kind = ErrorKind.Validation,
                        ResourceKey = TexlStrings.ErrExpectedRVFieldTypeMismatch,
                        MessageArgs = new object[] { sourceField, sourceFieldType._type.GetKindString(), targetFieldType._type.GetKindString() }
                    });
                    return false;
                }
            }

            return true;
        }
    }
}
