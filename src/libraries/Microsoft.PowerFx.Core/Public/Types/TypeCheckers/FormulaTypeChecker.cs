// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Public.Types.TypeCheckers
{
    internal abstract class FormulaTypeChecker
    {
        protected ICollection<ExpressionError> _errorList;
        protected readonly AggregateTypeChecker _aggregateTypeChecker;

        public bool Run(FormulaType sourceType, FormulaType typeToCheck)
        {
            if (sourceType == null || typeToCheck == null)
            {
                return false;
            }

            if (sourceType._type == DType.ObjNull || typeToCheck._type == DType.ObjNull)
            {
                return true;
            }

            // check Aggregate types
            if (sourceType._type.IsAggregate && typeToCheck._type.IsAggregate)
            {
                return _aggregateTypeChecker.Run((AggregateType)sourceType, (AggregateType)typeToCheck);
            }

            return IsMatch(sourceType, typeToCheck);
        }

        public FormulaTypeChecker(AggregateTypeChecker aggregateTypeChecker, ICollection<ExpressionError> errorList)
        {
            _errorList = errorList ?? throw new ArgumentNullException(nameof(errorList));
            _aggregateTypeChecker = aggregateTypeChecker ?? throw new ArgumentNullException(nameof(aggregateTypeChecker));
        }

        public abstract bool IsMatch(FormulaType sourceType, FormulaType typeToCheck);
    }

    internal class FormulaTypeCheckerNumberCoercionOnly : FormulaTypeChecker
    {
        public FormulaTypeCheckerNumberCoercionOnly(AggregateTypeChecker aggregateTypeChecker, ICollection<ExpressionError> errorList)
            : base(aggregateTypeChecker, errorList)
        {
        }

        public override bool IsMatch(FormulaType sourceType, FormulaType typeToCheck)
        {
            // NumericCoercion is allowed.
            if ((sourceType._type == DType.Number || sourceType._type == DType.Decimal) &&
                (typeToCheck._type == DType.Number || typeToCheck._type == DType.Decimal))
            {
                return true;
            }

            var isTypeMatch = sourceType == typeToCheck;

            if (!isTypeMatch)
            {
                _errorList.Add(new ExpressionError()
                {
                    Kind = ErrorKind.Validation,
                    Severity = ErrorSeverity.Critical,
                    Message = $"Type mismatch between source and target types. Expected {sourceType._type.GetKindString()}; Found {typeToCheck._type.GetKindString()}."
                });
            }

            return isTypeMatch;
        }
    }

    internal class FormulaTypeCheckerWithCoercion : FormulaTypeChecker
    {
        public FormulaTypeCheckerWithCoercion(AggregateTypeChecker aggregateTypeChecker, ICollection<ExpressionError> errorList)
            : base(aggregateTypeChecker, errorList)
        {
        }

        public override bool IsMatch(FormulaType sourceType, FormulaType typeToCheck)
        {
            var isTypeMatch = typeToCheck._type.CoercesTo(sourceType._type, aggregateCoercion: false, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: true);
            if (!isTypeMatch)
            {
                _errorList.Add(new ExpressionError()
                {
                    Kind = ErrorKind.Validation,
                    Severity = ErrorSeverity.Critical,
                    Message = $"Given {typeToCheck._type.GetKindString()} type cannot be coerced to source type {sourceType._type.GetKindString()}."
                });
            }

            return isTypeMatch;
        }
    }
}
