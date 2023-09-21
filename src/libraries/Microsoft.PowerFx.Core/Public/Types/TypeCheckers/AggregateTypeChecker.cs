// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Public.Types.TypeCheckers
{
    internal abstract class AggregateTypeChecker
    {
        protected ICollection<ExpressionError> _errorList;

        public AggregateTypeChecker(ICollection<ExpressionError> errorList)
        {
            _errorList = errorList ?? throw new ArgumentNullException(nameof(errorList));
        }

        public bool Run(AggregateType sourceType, AggregateType typeToCheck)
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
                    Severity = ErrorSeverity.Critical,
                    Message = $"Type mismatch between source and target types. Expected: {sourceType._type.GetKindString()}, found {typeToCheck._type.GetKindString()}."
                });
                return false;
            }

            return IsMatch(sourceType, typeToCheck);
        }

        public abstract bool IsMatch(AggregateType sourceType, AggregateType typeToCheck);

        public abstract bool IsFieldMatch(FormulaType sourceField, FormulaType fieldToCheck);
    }

    internal class StrictAggregateTypeChecker : AggregateTypeChecker
    {
        public StrictAggregateTypeChecker(ICollection<ExpressionError> errorList)
            : base(errorList)
        {
        }

        public override bool IsMatch(AggregateType sourceType, AggregateType typeToCheck)
        {
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
                        Severity = ErrorSeverity.Critical,
                        Message = $"Type mismatch between source and target record types. Given type has extra fields: {extraFields}."
                    });
                }
                else
                {
                    var missingFields = string.Join(", ", sourceType.FieldNames.Except(typeToCheck.FieldNames));
                    _errorList.Add(new ExpressionError()
                    {
                        Kind = ErrorKind.Validation,
                        Severity = ErrorSeverity.Critical,
                        Message = $"Type mismatch between source and target record types. Given type has missing fields: {missingFields}."
                    });
                }

                _errorList.Add(new ExpressionError()
                {
                    Kind = ErrorKind.Validation,
                    Severity = ErrorSeverity.Critical,
                    Message = $"Type mismatch between source and target record types. SourceType field count: {sourceType.FieldNames.Count()}; TypeToCheck field count {typeToCheck.FieldNames.Count()}."
                });
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
                        Severity = ErrorSeverity.Critical,
                        Message = $"Type mismatch between source and target record types. Field name: {sourceField} not found."
                    });
                    return false;
                }

                if (!IsFieldMatch(sourceFieldType, targetFieldType))
                {
                    _errorList.Add(new ExpressionError()
                    {
                        Kind = ErrorKind.Validation,
                        Severity = ErrorSeverity.Critical,
                        Message = $"Type mismatch between source and target record types. Field name: {sourceField} Expected {sourceFieldType._type.GetKindString()}; Found {targetFieldType._type.GetKindString()}."
                    });
                    return false;
                }
            }

            return true;
        }

        public override bool IsFieldMatch(FormulaType sourceField, FormulaType fieldToCheck)
        {
            if (sourceField == null || fieldToCheck == null)
            {
                return false;
            }

            if (sourceField._type == DType.ObjNull || fieldToCheck._type == DType.ObjNull)
            {
                return true;
            }

            if (sourceField._type.IsAggregate && fieldToCheck._type.IsAggregate)
            {
                return IsMatch((AggregateType)sourceField, (AggregateType)fieldToCheck);
            }

            // allow numeric coercion
            if ((sourceField._type == DType.Number || sourceField._type == DType.Decimal) &&
                (fieldToCheck._type == DType.Number || fieldToCheck._type == DType.Decimal))
            {
                return true;
            }

            return sourceField == fieldToCheck;
        }
    }
}
