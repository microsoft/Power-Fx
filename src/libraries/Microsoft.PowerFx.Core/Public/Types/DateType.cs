// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class DateType : FormulaType
    {
        internal DateType()
            : base(DType.Date)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Date";
        }

        internal override string DefaultExpressionValue()
        {
            var dateTimeMin = System.DateTime.MinValue.ToUniversalTime();

            return $"DateValue({CharacterUtils.ToPlainText(dateTimeMin.Date.ToString("o"))})";
        }
    }
}
