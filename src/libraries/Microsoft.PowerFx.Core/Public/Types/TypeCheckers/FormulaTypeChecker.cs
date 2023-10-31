// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Public.Types.TypeCheckers
{
    internal abstract class FormulaTypeChecker
    {
        protected readonly ICollection<ExpressionError> _errorList;

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
            if (sourceType is AggregateType sourceAggregateType && typeToCheck is AggregateType aggregateTypeToCheck)
            {
                return _aggregateTypeChecker.Run(sourceAggregateType, aggregateTypeToCheck, this);
            }

            return IsMatchScalar(sourceType, typeToCheck);
        }

        public FormulaTypeChecker(AggregateTypeChecker aggregateTypeChecker, ICollection<ExpressionError> errorList)
        {
            _errorList = errorList ?? throw new ArgumentNullException(nameof(errorList));
            _aggregateTypeChecker = aggregateTypeChecker ?? throw new ArgumentNullException(nameof(aggregateTypeChecker));
        }

        public abstract bool IsMatchScalar(FormulaType sourceType, FormulaType typeToCheck);
    }

    internal class FormulaTypeCheckerNumberCoercionOnly : FormulaTypeChecker
    {
        public FormulaTypeCheckerNumberCoercionOnly(AggregateTypeChecker aggregateTypeChecker, ICollection<ExpressionError> errorList)
            : base(aggregateTypeChecker, errorList)
        {
        }

        public override bool IsMatchScalar(FormulaType sourceType, FormulaType typeToCheck)
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
                    ResourceKey = TexlStrings.ErrExpectedRVTypeMismatch,
                    MessageArgs = new object[] { sourceType._type.GetKindString(), typeToCheck._type.GetKindString() }
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

        public override bool IsMatchScalar(FormulaType sourceType, FormulaType typeToCheck)
        {
            var isTypeMatch = typeToCheck._type.CoercesTo(sourceType._type, aggregateCoercion: false, isTopLevelCoercion: false, Features.PowerFxV1);
            if (!isTypeMatch)
            {
                _errorList.Add(new ExpressionError()
                {
                    Kind = ErrorKind.Validation,
                    ResourceKey = TexlStrings.ErrExpectedRVCannotCoerceType,
                    MessageArgs = new object[] { typeToCheck._type.GetKindString(), sourceType._type.GetKindString() }
                });
            }

            return isTypeMatch;
        }
    }
}
